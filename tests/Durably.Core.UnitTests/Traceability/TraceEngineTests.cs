using Xunit;

namespace Durably.Core.UnitTests.Traceability;
public class TraceEngineTests
{
    private sealed class TraceOrderFlow;
    private sealed class SkipFlow;

    [Fact]
    public async Task Engine_emits_succeeded_and_failed_trace_events()
    {
        var sink = new CollectingTraceSink();
        var harness = EngineTestHarness.Create(trace: sink);

        var failOnce = true;
        var flow = Flow.For<TraceOrderFlow, OrderState>()
            .Step("generate", (s, ct) => { s.Report = "r"; return Task.CompletedTask; })
            .Step("email", (s, ct) =>
            {
                if (failOnce)
                {
                    failOnce = false;
                    throw new InvalidOperationException("smtp");
                }

                s.EmailSent = true;
                return Task.CompletedTask;
            })
            .Step("finalize", (s, ct) => { s.Finalized = true; return Task.CompletedTask; });

        var first = await harness.StartAndProcessAsync(flow, "t1", new OrderState());
        Assert.Equal(FlowStatus.Failed, first.Status);

        var failed = sink.Records.Single(r => r.StepKey == "email" && r.Outcome == TraceOutcome.Failed);
        Assert.Equal("smtp", failed.ExceptionMessage);
        Assert.NotNull(failed.InputJson);

        var succeededGenerate = sink.Records.Single(r => r.StepKey == "generate" && r.Outcome == TraceOutcome.Succeeded);
        Assert.NotNull(succeededGenerate.OutputJson);

        await harness.StartAndProcessAsync(flow, "t1", new OrderState());

        var succeededEmail = sink.Records.Single(r => r.StepKey == "email" && r.Outcome == TraceOutcome.Succeeded);
        Assert.True(succeededEmail.DurationMs >= 0);
    }

    [Fact]
    public async Task Engine_emits_skipped_for_guarded_steps()
    {
        var sink = new CollectingTraceSink();
        var harness = EngineTestHarness.Create(trace: sink);

        var flow = Flow.For<SkipFlow, BranchState>()
            .StepIf(s => s.Flag, "optional", (s, ct) => Task.CompletedTask)
            .Step("done", (s, ct) => Task.CompletedTask);

        await harness.StartAndProcessAsync(flow, "s1", new BranchState { Flag = false });

        Assert.Contains(sink.Records, r => r.StepKey == "optional" && r.Outcome == TraceOutcome.Skipped);
    }
}
