using Xunit;

namespace Durably.Core.UnitTests.Execution;
public sealed class ExecutionProcessorUnitTests
{
    private sealed class TimeoutFlow;
    private sealed class CompletedFlow;

    private const string InstanceId = "proc-1";
    private const string SlowStepKey = "slow";

    [Fact]
    public async Task ProcessAsync_unregistered_flow_quarantines_as_Failed()
    {
        // Arrange
        var harness = EngineTestHarness.Create();
        const string missingFlowName = "Missing.Flow";
        var runId = Guid.NewGuid().ToString("N");
        await harness.Store.CreateAsync(new ExecutionRecord
        {
            FlowName = missingFlowName,
            RunId = runId,
            InstanceId = InstanceId,
            Status = ExecutionStatus.Pending,
            CurrentStep = 0,
            ContextJson = "{}",
            Attempts = 0,
            Version = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, CancellationToken.None);

        var leaseUntil = DateTimeOffset.UtcNow.Add(TestLimits.DefaultLeaseDuration);
        Assert.True(await harness.Store.TryAcquireLeaseAsync(
            missingFlowName, runId, harness.RunnerId, leaseUntil, CancellationToken.None));
        var record = await harness.Store.LoadAsync(missingFlowName, runId, CancellationToken.None);

        // Act
        var result = await harness.Processor.ProcessAsync(record!, harness.RunnerId, harness.LeaseDuration);

        // Assert
        Assert.Equal(FlowStatus.Failed, result.Status);
        Assert.Contains("not registered", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
        var status = await harness.Store.LoadAsync(missingFlowName, runId, CancellationToken.None);
        Assert.Equal(ExecutionStatus.Failed, status!.Status);
        Assert.Null(status.LockedBy);
    }

    [Fact]
    public async Task ProcessAsync_corrupt_ContextJson_quarantines_as_Failed()
    {
        // Arrange
        var harness = EngineTestHarness.Create();
        var flow = Flow.For<CompletedFlow, OrderState>()
            .Step("only", (_, _) => Task.CompletedTask);
        harness.Register(flow);

        var runId = Guid.NewGuid().ToString("N");
        await harness.Store.CreateAsync(new ExecutionRecord
        {
            FlowName = flow.Name,
            RunId = runId,
            InstanceId = InstanceId,
            Status = ExecutionStatus.Pending,
            CurrentStep = 0,
            ContextJson = "{not-json",
            Attempts = 0,
            Version = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, CancellationToken.None);

        var leaseUntil = DateTimeOffset.UtcNow.Add(TestLimits.DefaultLeaseDuration);
        Assert.True(await harness.Store.TryAcquireLeaseAsync(
            flow.Name, runId, harness.RunnerId, leaseUntil, CancellationToken.None));
        var record = await harness.Store.LoadAsync(flow.Name, runId, CancellationToken.None);

        // Act
        var result = await harness.Processor.ProcessAsync(record!, harness.RunnerId, harness.LeaseDuration);

        // Assert
        Assert.Equal(FlowStatus.Failed, result.Status);
        var status = await harness.Store.LoadAsync(flow.Name, runId, CancellationToken.None);
        Assert.Equal(ExecutionStatus.Failed, status!.Status);
        Assert.Contains("deserialize", status.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        Assert.Null(status.LockedBy);
    }

    [Fact]
    public async Task ProcessAsync_step_timeout_marks_Failed_with_FailedStep()
    {
        // Arrange
        var harness = EngineTestHarness.Create();
        var flow = Flow.For<TimeoutFlow, OrderState>()
            .Step(SlowStepKey, async (_, cancellationToken) => await Task.Delay(TestLimits.LongDelay, cancellationToken), options => options.Timeout(TestLimits.ShortTimeout));

        // Act
        var result = await harness.StartAndProcessAsync(flow, InstanceId, new OrderState());

        // Assert
        Assert.Equal(FlowStatus.Failed, result.Status);
        Assert.Equal(SlowStepKey, result.FailedStep);
        var status = await harness.Engine.GetStatusAsync(flow.Name, InstanceId);
        Assert.Equal(ExecutionStatus.Failed, status!.Status);
        Assert.Equal(0, status.CurrentStep);
    }

    [Fact]
    public async Task ProcessAsync_completed_instance_returns_AlreadyCompleted()
    {
        // Arrange
        var harness = EngineTestHarness.Create();
        var flow = Flow.For<CompletedFlow, OrderState>()
            .Step("only", (_, _) => Task.CompletedTask);
        await harness.StartAndProcessAsync(flow, InstanceId, new OrderState());

        var record = await harness.Store.LoadLatestAsync(flow.Name, InstanceId, CancellationToken.None);
        var leaseUntil = DateTimeOffset.UtcNow.Add(harness.LeaseDuration);
        Assert.True(await harness.Store.TryAcquireLeaseAsync(
            flow.Name, record!.RunId, harness.RunnerId, leaseUntil, CancellationToken.None));

        // Act
        var second = await harness.Processor.ProcessAsync(record!, harness.RunnerId, harness.LeaseDuration);

        // Assert
        Assert.Equal(FlowRunOutcome.AlreadyCompleted, second.Outcome);
    }
}
