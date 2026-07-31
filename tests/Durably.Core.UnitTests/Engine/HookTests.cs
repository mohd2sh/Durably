using Xunit;

namespace Durably.Core.UnitTests.Engine;
public class HookTests
{
    private sealed class HookSuccessFlow;
    private sealed class HookFailureFlow;
    private sealed class HookThrowFlow;

    [Fact]
    public async Task OnSuccess_lambda_runs_after_completion()
    {
        var harness = EngineTestHarness.Create();
        var seen = false;

        var flow = Flow.For<HookSuccessFlow, OrderState>()
            .Step("work", (s, ct) =>
            {
                s.Report = "done";
                return Task.CompletedTask;
            })
            .OnSuccess(s => seen = s.Report == "done");

        var result = await harness.StartAndProcessAsync(flow, "h1", new OrderState());

        Assert.Equal(FlowStatus.Completed, result.Status);
        Assert.True(seen);
    }

    [Fact]
    public async Task OnFailure_lambda_runs_after_terminal_failure()
    {
        var harness = EngineTestHarness.Create();
        Exception? seen = null;

        var flow = Flow.For<HookFailureFlow, OrderState>()
            .Step("boom", (s, ct) => throw new InvalidOperationException("nope"))
            .OnFailure((_, ex) => seen = ex);

        var result = await harness.StartAndProcessAsync(flow, "h2", new OrderState());

        Assert.Equal(FlowStatus.Failed, result.Status);
        Assert.NotNull(seen);
        Assert.Equal("nope", seen!.Message);
    }

    [Fact]
    public async Task OnSuccess_hook_exception_does_not_fail_execution()
    {
        var harness = EngineTestHarness.Create();

        var flow = Flow.For<HookThrowFlow, OrderState>()
            .Step("work", (s, ct) => Task.CompletedTask)
            .OnSuccess(_ => throw new InvalidOperationException("hook blew up"));

        var result = await harness.StartAndProcessAsync(flow, "h3", new OrderState());

        Assert.Equal(FlowStatus.Completed, result.Status);
        var status = await harness.Engine.GetStatusAsync(flow.Name, "h3");
        Assert.Equal(ExecutionStatus.Completed, status!.Status);
    }
}
