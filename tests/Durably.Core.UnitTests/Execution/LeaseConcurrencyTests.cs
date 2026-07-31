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
    public async Task Resume_ignores_initial_state_on_second_run()
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
        var second = await harness.StartAndProcessAsync(flow, "s1", new OrderState { Report = "ignored" });

        Assert.True(second.IsCompleted);
        Assert.Equal(FlowRunOutcome.Resumed, second.Outcome);
        var record = await harness.Store.LoadAsync(flow.Name, "s1", default);
        Assert.Contains("checkpoint", record!.ContextJson, StringComparison.Ordinal);
        Assert.DoesNotContain("ignored", record.ContextJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Null_initial_state_uses_new_on_first_run()
    {
        var harness = EngineTestHarness.Create();

        var flow = Flow.For<BranchState>()
            .Step("set", (s, _) => { s.Path = "ok"; return Task.CompletedTask; });

        var result = await harness.StartAndProcessAsync(flow, "d1", state: null);

        Assert.True(result.IsCompleted);
        var record = await harness.Store.LoadAsync(flow.Name, "d1", default);
        Assert.Contains("ok", record!.ContextJson, StringComparison.Ordinal);
    }
}
