using Xunit;

namespace Durably.Core.UnitTests.Engine;
public sealed class FlowEngineUnitTests
{
    private sealed class StatusFlow;
    private sealed class ConcurrentStartFlow;

    private const string InstanceId = "inst-1";

    [Fact]
    public async Task GetStatusAsync_returns_persisted_fields()
    {
        // Arrange
        var harness = EngineTestHarness.Create();
        var flow = Flow.For<StatusFlow, OrderState>()
            .Step("only", (s, _) =>
            {
                s.Report = "done";
                return Task.CompletedTask;
            });

        // Act
        await harness.StartAndProcessAsync(flow, InstanceId, new OrderState());
        var status = await harness.Engine.GetStatusAsync(flow.Name, InstanceId);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(flow.Name, status!.FlowName);
        Assert.Equal(InstanceId, status.InstanceId);
        Assert.Equal(ExecutionStatus.Completed, status.Status);
        Assert.Null(status.FailedStep);
    }

    [Fact]
    public async Task Concurrent_StartAsync_same_instance_yields_one_Created()
    {
        // Arrange
        var harness = EngineTestHarness.Create();
        var flow = Flow.For<ConcurrentStartFlow, OrderState>()
            .Step("noop", (_, _) => Task.CompletedTask);
        const int concurrentStarts = 8;

        // Act
        var tasks = Enumerable.Range(0, concurrentStarts)
            .Select(_ => harness.Engine.StartAsync(flow, InstanceId, new OrderState()))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, results.Count(r => r.Outcome == FlowStartOutcome.Created));
        Assert.Equal(concurrentStarts - 1, results.Count(r => r.Outcome == FlowStartOutcome.Conflict));
    }
}
