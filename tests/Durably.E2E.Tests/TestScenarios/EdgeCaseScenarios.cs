using Durably.E2E.Tests.Flows;
using Durably.E2E.Tests.Models;
using Xunit;

namespace Durably.E2E.Tests.TestScenarios;

public abstract class EdgeCaseScenarios<TFixture> : ScenarioTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected EdgeCaseScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Completed_instance_second_start_is_idempotent()
    {
        await ResetAsync();
        var (flow, counters) = ScenarioFlows.CreateIdempotent();

        await using var host = await StartHostAsync(d => d.AddFlow(flow));

        await host.Engine.StartAsync(flow, "id-1", new OrderState());
        await host.WaitForStatusAsync(flow.Name, "id-1", ExecutionStatus.Completed);
        Assert.Equal(1, counters.Generate);

        var second = await host.Engine.StartAsync(flow, "id-1", new OrderState());
        Assert.Equal(FlowStartOutcome.AlreadyExists, second.Outcome);
        Assert.Equal(ExecutionStatus.Completed, second.Status);

        await Task.Delay(TestLimits.MediumDelay);
        Assert.Equal(1, counters.Generate);
    }

    [Fact]
    public async Task Step_timeout_fails_cleanly_without_corrupting_checkpoint_pointer()
    {
        await ResetAsync();
        var flow = ScenarioFlows.CreateTimeoutStep(TestLimits.StepTimeout);

        await using var host = await StartHostAsync(d => d.AddFlow(flow));

        await host.Engine.StartAsync(flow, "timeout-1", new OrderState());
        await host.WaitForStatusAsync(flow.Name, "timeout-1", ExecutionStatus.Failed, TestLimits.ShortWaitTimeout);

        var status = await host.Engine.GetStatusAsync(flow.Name, "timeout-1");
        Assert.Equal("slow", status!.FailedStep);
        Assert.Equal(0, status.CurrentStep);
    }

    [Fact]
    public async Task Null_initial_state_uses_new_and_completes()
    {
        await ResetAsync();
        var flow = ScenarioFlows.CreateNullStateFriendly();

        await using var host = await StartHostAsync(d => d.AddFlow(flow));

        await host.Engine.StartAsync(flow, "null-1", initialState: null);
        await host.WaitForStatusAsync(flow.Name, "null-1", ExecutionStatus.Completed);

        var state = await host.LoadStateAsync<BranchState>(flow.Name, "null-1");
        Assert.Equal("ok", state.Path);
    }

    [Fact]
    public async Task Unregistered_flow_quarantines_as_Failed_when_processed()
    {
        await ResetAsync();
        var store = NewExecutionStore();
        var now = DateTimeOffset.UtcNow;
        await store.CreateAsync(new ExecutionRecord
        {
            FlowName = "Missing.Flow",
            InstanceId = "u1",
            Status = ExecutionStatus.Pending,
            CurrentStep = 0,
            ContextJson = "{}",
            Attempts = 0,
            Version = 0,
            CreatedAt = now,
            UpdatedAt = now
        }, default);

        await using var host = await StartHostAsync(_ => { }, o => o.WorkerEnabled = false);

        var leaseUntil = DateTimeOffset.UtcNow.Add(TestLimits.DefaultLeaseDuration);
        Assert.True(await host.Store.TryAcquireLeaseAsync("Missing.Flow", "u1", "runner", leaseUntil, CancellationToken.None));
        var record = await host.Store.LoadAsync("Missing.Flow", "u1", CancellationToken.None);

        var result = await host.Processor.ProcessAsync(record!, "runner", TestLimits.DefaultLeaseDuration);

        Assert.Equal(FlowStatus.Failed, result.Status);
        Assert.Contains("not registered", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
        var status = await host.Store.LoadAsync("Missing.Flow", "u1", CancellationToken.None);
        Assert.Equal(ExecutionStatus.Failed, status!.Status);
    }
}

[Collection(SqlServerE2ECollection.Name)]
public sealed class SqlServerEdgeCaseScenarios : EdgeCaseScenarios<SqlServerDatabaseFixture>
{
    public SqlServerEdgeCaseScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresE2ECollection.Name)]
public sealed class PostgresEdgeCaseScenarios : EdgeCaseScenarios<PostgresDatabaseFixture>
{
    public PostgresEdgeCaseScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
