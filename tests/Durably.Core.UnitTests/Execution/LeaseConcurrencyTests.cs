using Xunit;

namespace Durably.Core.UnitTests.Execution;
public class LeaseConcurrencyTests
{
    private sealed class LeaseTestFlow;
    private sealed class StateTestFlow;

    [Fact]
    public async Task Second_runner_gets_AlreadyRunning_while_first_holds_lease()
    {
        var harness = EngineTestHarness.Create();
        var flow = Flow.For<LeaseTestFlow, OrderState>()
            .Step("slow", async (_, cancellationToken) =>
            {
                await Task.Delay(TestLimits.LongStepDelay, cancellationToken);
            });

        var first = harness.StartAndProcessAsync(flow, "inst-1", new OrderState());
        await Task.Delay(TestLimits.ContentionDelay);

        var contender = harness.CreateContender("runner-b");
        var second = await contender.ProcessAsync(flow.Name, "inst-1");

        await first;

        Assert.Equal(FlowRunOutcome.AlreadyRunning, second.Outcome);
    }

    [Fact]
    public async Task Resume_via_ProcessAsync_ignores_initial_state_since_no_new_start_occurs()
    {
        var harness = EngineTestHarness.Create();
        var failOnce = true;

        var flow = Flow.For<StateTestFlow, OrderState>()
            .Step("seed", (s, _) => { s.Report = "checkpoint"; return Task.CompletedTask; })
            .Step("fail", (s, _) =>
            {
                if (failOnce)
                {
                    failOnce = false;
                    throw new InvalidOperationException("boom");
                }

                return Task.CompletedTask;
            });

        await harness.StartAndProcessAsync(flow, "s1", new OrderState { Report = "initial" });
        // Resuming the same (Failed) run happens via re-processing, not via StartAsync again:
        // StartAsync on a non-open run always creates a brand new run with a new RunId.
        var second = await harness.ProcessAsync(flow.Name, "s1");

        Assert.True(second.IsCompleted);
        Assert.Equal(FlowRunOutcome.Resumed, second.Outcome);
        var record = await harness.Store.LoadLatestAsync(flow.Name, "s1", default);
        Assert.Contains("checkpoint", record!.ContextJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Second_StartAsync_after_failure_creates_new_run_using_new_initial_state()
    {
        var harness = EngineTestHarness.Create();
        var failOnce = true;

        var flow = Flow.For<StateTestFlow, OrderState>()
            .Step("seed", (s, _) => { s.Report ??= "checkpoint"; return Task.CompletedTask; })
            .Step("fail", (s, _) =>
            {
                if (failOnce)
                {
                    failOnce = false;
                    throw new InvalidOperationException("boom");
                }

                return Task.CompletedTask;
            });

        var first = await harness.StartAndProcessAsync(flow, "s1", new OrderState { Report = "initial" });
        Assert.Equal(FlowStatus.Failed, first.Status);

        var second = await harness.StartAndProcessAsync(flow, "s1", new OrderState { Report = "new-run-state" });

        Assert.True(second.IsCompleted);
        Assert.Equal(FlowRunOutcome.Started, second.Outcome);
        var record = await harness.Store.LoadLatestAsync(flow.Name, "s1", default);
        Assert.Contains("new-run-state", record!.ContextJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Null_initial_state_uses_new_on_first_run()
    {
        var harness = EngineTestHarness.Create();

        var flow = Flow.For<BranchState>()
            .Step("set", (s, _) => { s.Path = "ok"; return Task.CompletedTask; });

        var result = await harness.StartAndProcessAsync(flow, "d1", state: null);

        Assert.True(result.IsCompleted);
        var record = await harness.Store.LoadLatestAsync(flow.Name, "d1", default);
        Assert.Contains("ok", record!.ContextJson, StringComparison.Ordinal);
    }
}
