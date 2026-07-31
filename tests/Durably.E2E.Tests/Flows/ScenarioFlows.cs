using Durably.E2E.Tests.Models;

namespace Durably.E2E.Tests.Flows;

internal static class ScenarioFlows
{
    private sealed class HappyPathFlow;
    private sealed class FailOnceEmailFlow;
    private sealed class TransientRetryFlow;
    private sealed class RetryExhaustFlow;
    private sealed class StepIfFlow;
    private sealed class ChooseFlow;
    private sealed class ChooseFailFlow;
    private sealed class BlockingFlow;
    private sealed class TimeoutFlow;
    private sealed class IdempotentFlow;
    private sealed class NullStateFlow;
    private sealed class SlowLeaseFlow;

    public static IFlowBuilder<OrderState> CreateHappyPath()
        => Flow.For<HappyPathFlow, OrderState>()
            .Step("generate", (s, _) =>
            {
                s.Report = "report";
                return Task.CompletedTask;
            })
            .Step("email", (s, _) =>
            {
                s.EmailSent = true;
                return Task.CompletedTask;
            })
            .Step("finalize", (s, _) =>
            {
                s.Finalized = true;
                return Task.CompletedTask;
            });

    public static (IFlowBuilder<OrderState> Flow, StepCounters Counters) CreateFailOnceEmail()
    {
        var counters = new StepCounters();
        var flow = Flow.For<FailOnceEmailFlow, OrderState>()
            .Step("generate", (s, _) =>
            {
                Interlocked.Increment(ref counters.Generate);
                s.Report = "report";
                return Task.CompletedTask;
            })
            .Step("email", (s, _) =>
            {
                Interlocked.Increment(ref counters.Email);
                if (counters.FailOnce)
                {
                    counters.FailOnce = false;
                    throw new InvalidOperationException("smtp down");
                }

                s.EmailSent = true;
                return Task.CompletedTask;
            })
            .Step("finalize", (s, _) =>
            {
                Interlocked.Increment(ref counters.Finalize);
                s.Finalized = true;
                return Task.CompletedTask;
            });

        return (flow, counters);
    }

    public static (IFlowBuilder<OrderState> Flow, StepCounters Counters) CreateTransientRetry()
    {
        var counters = new StepCounters();
        var flow = Flow.For<TransientRetryFlow, OrderState>()
            .Step("flaky", (_, _) =>
            {
                var attempt = Interlocked.Increment(ref counters.Flaky);
                if (attempt < counters.FailUntilAttempt)
                {
                    throw new InvalidOperationException("transient");
                }

                return Task.CompletedTask;
            }, o => o.Retry(RetryPolicy.Fixed(5, TimeSpan.Zero)));

        return (flow, counters);
    }

    public static (IFlowBuilder<OrderState> Flow, StepCounters Counters) CreateRetryExhaust()
    {
        var counters = new StepCounters();
        var flow = Flow.For<RetryExhaustFlow, OrderState>()
            .Step("always-fails", (_, _) =>
            {
                Interlocked.Increment(ref counters.Flaky);
                throw new InvalidOperationException("nope");
            }, o => o.Retry(RetryPolicy.Fixed(2, TimeSpan.Zero)));

        return (flow, counters);
    }

    public static (IFlowBuilder<BranchState> Flow, StepCounters Counters) CreateStepIf()
    {
        var counters = new StepCounters();
        var flow = Flow.For<StepIfFlow, BranchState>()
            .Step("a", (_, _) => Task.CompletedTask)
            .StepIf(s => s.Flag, "optional", (_, _) =>
            {
                Interlocked.Increment(ref counters.Optional);
                return Task.CompletedTask;
            })
            .Step("b", (_, _) => Task.CompletedTask);

        return (flow, counters);
    }

    public static IFlowBuilder<BranchState> CreateChoose()
        => Flow.For<ChooseFlow, BranchState>()
            .Choose(s => s.Kind)
                .When("a", b => b.Step("a1", (s, _) =>
                {
                    s.Path = "a";
                    return Task.CompletedTask;
                }))
                .When("b", b => b.Step("b1", (s, _) =>
                {
                    s.Path = "b";
                    return Task.CompletedTask;
                }))
                .Otherwise(b => b.Step("o", (s, _) =>
                {
                    s.Path = "otherwise";
                    return Task.CompletedTask;
                }))
            .EndChoose()
            .Step("end", (_, _) => Task.CompletedTask);

    public static (IFlowBuilder<BranchState> Flow, StepCounters Counters) CreateChooseFailOnce()
    {
        var counters = new StepCounters();
        var flow = Flow.For<ChooseFailFlow, BranchState>()
            .Choose(s => s.Kind)
                .When("a", b => b.Step("a1", (s, _) =>
                {
                    Interlocked.Increment(ref counters.BranchA);
                    if (counters.FailOnce)
                    {
                        counters.FailOnce = false;
                        throw new InvalidOperationException("branch boom");
                    }

                    s.Path = "a";
                    return Task.CompletedTask;
                }))
                .When("b", b => b.Step("b1", (s, _) =>
                {
                    Interlocked.Increment(ref counters.BranchB);
                    s.Path = "b";
                    return Task.CompletedTask;
                }))
                .Otherwise(b => b.Step("o", (s, _) =>
                {
                    Interlocked.Increment(ref counters.Otherwise);
                    s.Path = "otherwise";
                    return Task.CompletedTask;
                }))
            .EndChoose()
            .Step("end", (_, _) => Task.CompletedTask);

        return (flow, counters);
    }

    public static (IFlowBuilder<OrderState> Flow, StepCounters Counters, TaskCompletionSource Gate) CreateBlockingAfterGenerate()
    {
        var counters = new StepCounters();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var flow = Flow.For<BlockingFlow, OrderState>()
            .Step("generate", (s, _) =>
            {
                Interlocked.Increment(ref counters.Generate);
                s.Report = "report";
                return Task.CompletedTask;
            })
            .Step("block", async (_, ct) =>
            {
                Interlocked.Increment(ref counters.Blocking);
                await gate.Task.WaitAsync(ct);
            })
            .Step("finalize", (s, _) =>
            {
                Interlocked.Increment(ref counters.Finalize);
                s.Finalized = true;
                return Task.CompletedTask;
            });

        return (flow, counters, gate);
    }

    public static IFlowBuilder<OrderState> CreateTimeoutStep(TimeSpan timeout)
        => Flow.For<TimeoutFlow, OrderState>()
            .Step("slow", async (_, ct) =>
            {
                await Task.Delay(TestLimits.SlowPollInterval, ct);
            }, o => o.Timeout(timeout));

    public static (IFlowBuilder<OrderState> Flow, StepCounters Counters) CreateIdempotent()
    {
        var counters = new StepCounters();
        var flow = Flow.For<IdempotentFlow, OrderState>()
            .Step("only", (_, _) =>
            {
                Interlocked.Increment(ref counters.Generate);
                return Task.CompletedTask;
            });

        return (flow, counters);
    }

    public static IFlowBuilder<BranchState> CreateNullStateFriendly()
        => Flow.For<NullStateFlow, BranchState>()
            .Step("set", (s, _) =>
            {
                s.Path = "ok";
                return Task.CompletedTask;
            });

    public static IFlowBuilder<OrderState> CreateSlowLeaseStep(TimeSpan delay)
        => Flow.For<SlowLeaseFlow, OrderState>()
            .Step("slow", async (_, ct) =>
            {
                await Task.Delay(delay, ct);
            });
}
