using Xunit;

namespace Durably.Core.UnitTests.Engine;
public class EngineTests
{
    private sealed class RetryFlow;
    private sealed class RetryExhaustFlow;
    private sealed class OptionalStepFlow;
    private sealed class ChooseFlow;
    private sealed class IdempotentFlow;

    [Fact]
    public async Task Resume_after_exception_continues_from_failed_step_without_rerunning_prior_steps()
    {
        var harness = EngineTestHarness.Create();
        var (flow, counters) = ResumeFlowTestHelper.CreateFailOnceEmailFlow();

        var first = await harness.StartAndProcessAsync(flow, "order-1", new OrderState());

        Assert.Equal(FlowStatus.Failed, first.Status);
        Assert.Equal("email", first.FailedStep);
        Assert.Equal(1, counters.Generate);
        Assert.Equal(1, counters.Enrich);
        Assert.Equal(1, counters.Email);
        Assert.Equal(0, counters.Finalize);

        var second = await harness.StartAndProcessAsync(flow, "order-1", new OrderState());

        Assert.Equal(FlowStatus.Completed, second.Status);
        Assert.Equal(1, counters.Generate);
        Assert.Equal(1, counters.Enrich);
        Assert.Equal(2, counters.Email);
        Assert.Equal(1, counters.Finalize);
    }

    [Fact]
    public async Task Step_retries_until_it_succeeds()
    {
        var harness = EngineTestHarness.Create();
        var attempts = 0;

        var flow = Flow.For<RetryFlow, OrderState>()
            .Step("flaky", (s, ct) =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new InvalidOperationException("transient");
                }

                return Task.CompletedTask;
            }, o => o.Retry(RetryPolicy.Fixed(3, TimeSpan.Zero)));

        var result = await harness.StartAndProcessAsync(flow, "r-1", new OrderState());

        Assert.Equal(FlowStatus.Completed, result.Status);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Step_fails_after_exhausting_retries()
    {
        var harness = EngineTestHarness.Create();
        var attempts = 0;

        var flow = Flow.For<RetryExhaustFlow, OrderState>()
            .Step("always-fails", (s, ct) =>
            {
                attempts++;
                throw new InvalidOperationException("nope");
            }, o => o.Retry(RetryPolicy.Fixed(2, TimeSpan.Zero)));

        var result = await harness.StartAndProcessAsync(flow, "r-2", new OrderState());

        Assert.Equal(FlowStatus.Failed, result.Status);
        Assert.Equal("always-fails", result.FailedStep);
        Assert.Equal(2, result.Attempts);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task StepIf_skips_optional_step_when_condition_is_false()
    {
        var harness = EngineTestHarness.Create();
        var optionalRan = false;

        var flow = Flow.For<OptionalStepFlow, BranchState>()
            .Step("a", (s, ct) => Task.CompletedTask)
            .StepIf(s => s.Flag, "optional", (s, ct) => { optionalRan = true; return Task.CompletedTask; })
            .Step("b", (s, ct) => Task.CompletedTask);

        var skipped = await harness.StartAndProcessAsync(flow, "opt-off", new BranchState { Flag = false });
        Assert.Equal(FlowStatus.Completed, skipped.Status);
        Assert.False(optionalRan);

        var ran = await harness.StartAndProcessAsync(flow, "opt-on", new BranchState { Flag = true });
        Assert.Equal(FlowStatus.Completed, ran.Status);
        Assert.True(optionalRan);
    }

    [Theory]
    [InlineData("a", "a")]
    [InlineData("b", "b")]
    [InlineData("z", "otherwise")]
    public async Task Choose_takes_the_matching_branch(string kind, string expectedPath)
    {
        var harness = EngineTestHarness.Create();

        var flow = Flow.For<ChooseFlow, BranchState>()
            .Choose(s => s.Kind)
                .When("a", b => b.Step("a1", (s, ct) => { s.Path = "a"; return Task.CompletedTask; }))
                .When("b", b => b.Step("b1", (s, ct) => { s.Path = "b"; return Task.CompletedTask; }))
                .Otherwise(b => b.Step("o", (s, ct) => { s.Path = "otherwise"; return Task.CompletedTask; }))
            .EndChoose()
            .Step("end", (s, ct) => Task.CompletedTask);

        var instanceId = $"choose-{kind}";
        var result = await harness.StartAndProcessAsync(flow, instanceId, new BranchState { Kind = kind });

        Assert.Equal(FlowStatus.Completed, result.Status);
        var finalState = await harness.LoadStateAsync<BranchState>(flow.Name, instanceId);
        Assert.Equal(expectedPath, finalState.Path);
    }

    [Fact]
    public async Task Oop_flow_runs_identically_to_functional_flow()
    {
        var harness = EngineTestHarness.Create();
        var orderFlow = new OrderFlow();

        var result = await harness.StartAndProcessAsync(orderFlow, "oop-1", new OrderState());

        Assert.Equal(FlowStatus.Completed, result.Status);
        var finalState = await harness.LoadStateAsync<OrderState>(typeof(OrderFlow).FullName!, "oop-1");
        Assert.Equal("report", finalState.Report);
        Assert.True(finalState.EmailSent);
        Assert.True(finalState.Finalized);
    }

    [Fact]
    public async Task Global_default_retry_applies_when_step_does_not_override()
    {
        var harness = EngineTestHarness.Create(stepDefaults: new StepDefaults(RetryPolicy.Fixed(3, TimeSpan.Zero), null));
        var attempts = 0;

        var flow = new GlobalDefaultRetryFlow((_, _) =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new InvalidOperationException("transient");
            }

            return Task.CompletedTask;
        });

        var result = await harness.StartAndProcessAsync(flow, "global-retry-1", new OrderState());

        Assert.Equal(FlowStatus.Completed, result.Status);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Per_step_retry_none_overrides_global_default()
    {
        var harness = EngineTestHarness.Create(stepDefaults: new StepDefaults(RetryPolicy.Fixed(3, TimeSpan.Zero), null));
        var attempts = 0;

        var flow = new GlobalDefaultRetryOverrideFlow(
            (_, _) =>
            {
                attempts++;
                throw new InvalidOperationException("no retry");
            },
            (_, _) => Task.CompletedTask);

        var result = await harness.StartAndProcessAsync(flow, "global-retry-override", new OrderState());

        Assert.Equal(FlowStatus.Failed, result.Status);
        Assert.Equal("no-retry", result.FailedStep);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task Completed_flow_is_idempotent_and_does_not_rerun_steps()
    {
        var harness = EngineTestHarness.Create();
        var runs = 0;

        var flow = Flow.For<IdempotentFlow, OrderState>()
            .Step("only", (s, ct) => { runs++; return Task.CompletedTask; });

        var first = await harness.StartAndProcessAsync(flow, "id-1", new OrderState());
        var second = await harness.StartAndProcessAsync(flow, "id-1", new OrderState());

        Assert.Equal(FlowStatus.Completed, first.Status);
        Assert.Equal(FlowStatus.Completed, second.Status);
        Assert.Equal(1, runs);
    }
}
