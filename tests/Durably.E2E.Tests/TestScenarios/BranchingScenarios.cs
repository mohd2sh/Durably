using Durably.E2E.Tests.Flows;
using Durably.E2E.Tests.Models;
using Xunit;

namespace Durably.E2E.Tests.TestScenarios;

public abstract class BranchingScenarios<TFixture> : ScenarioTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected BranchingScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task StepIf_skips_when_false_and_runs_when_true()
    {
        await ResetAsync();
        var (flow, counters) = ScenarioFlows.CreateStepIf();

        await using var host = await StartHostAsync(d => d.AddFlow(flow));

        await host.Engine.StartAsync(flow, "opt-off", new BranchState { Flag = false });
        await host.WaitForStatusAsync(flow.Name, "opt-off", ExecutionStatus.Completed);
        Assert.Equal(0, counters.Optional);

        await host.Engine.StartAsync(flow, "opt-on", new BranchState { Flag = true });
        await host.WaitForStatusAsync(flow.Name, "opt-on", ExecutionStatus.Completed);
        Assert.Equal(1, counters.Optional);
    }

    [Fact]
    public async Task Choose_takes_matching_branch()
    {
        await ResetAsync();
        var flow = ScenarioFlows.CreateChoose();

        await using var host = await StartHostAsync(d => d.AddFlow(flow));

        await host.Engine.StartAsync(flow, "choose-a", new BranchState { Kind = "a" });
        await host.WaitForStatusAsync(flow.Name, "choose-a", ExecutionStatus.Completed);
        Assert.Equal("a", (await host.LoadStateAsync<BranchState>(flow.Name, "choose-a")).Path);

        await host.Engine.StartAsync(flow, "choose-b", new BranchState { Kind = "b" });
        await host.WaitForStatusAsync(flow.Name, "choose-b", ExecutionStatus.Completed);
        Assert.Equal("b", (await host.LoadStateAsync<BranchState>(flow.Name, "choose-b")).Path);

        await host.Engine.StartAsync(flow, "choose-z", new BranchState { Kind = "z" });
        await host.WaitForStatusAsync(flow.Name, "choose-z", ExecutionStatus.Completed);
        Assert.Equal("otherwise", (await host.LoadStateAsync<BranchState>(flow.Name, "choose-z")).Path);
    }

    [Fact]
    public async Task Resume_after_branch_failure_uses_checkpointed_kind_not_new_initial_state()
    {
        await ResetAsync();
        var (flow, counters) = ScenarioFlows.CreateChooseFailOnce();

        await using var host = await StartHostAsync(d => d.AddFlow(flow));

        await host.Engine.StartAsync(flow, "branch-resume", new BranchState { Kind = "a" });
        await host.WaitForStatusAsync(flow.Name, "branch-resume", ExecutionStatus.Failed);
        Assert.Equal(1, counters.BranchA);
        Assert.Equal(0, counters.BranchB);

        // A Failed run is not "open"; resuming it happens via the explicit resume path
        // (re-processing the same RunId), not by calling StartAsync again — that would
        // instead create a brand new, independent run.
        var resumed = await host.ResumeFailedAsync(flow.Name, "branch-resume");
        Assert.Equal(FlowStatus.Completed, resumed.Status);
        Assert.Equal(2, counters.BranchA);
        Assert.Equal(0, counters.BranchB);

        var state = await host.LoadStateAsync<BranchState>(flow.Name, "branch-resume");
        Assert.Equal("a", state.Path);
        Assert.Equal("a", state.Kind);

        // Now that the run is Completed, starting again creates a fresh, independent run.
        var start = await host.Engine.StartAsync(flow, "branch-resume", new BranchState { Kind = "b" });
        Assert.Equal(FlowStartOutcome.Created, start.Outcome);
    }
}

[Collection(SqlServerE2ECollection.Name)]
public sealed class SqlServerBranchingScenarios : BranchingScenarios<SqlServerDatabaseFixture>
{
    public SqlServerBranchingScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresE2ECollection.Name)]
public sealed class PostgresBranchingScenarios : BranchingScenarios<PostgresDatabaseFixture>
{
    public PostgresBranchingScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
