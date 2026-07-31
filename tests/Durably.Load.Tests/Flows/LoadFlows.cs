namespace Durably.Load.Tests;

internal static class LoadFlows
{
    private sealed class CountingFlow;

    public static IFlowBuilder<LoadState> CreateCountingStep(Action onExecute)
        => Flow.For<CountingFlow, LoadState>()
            .Step("work", (_, _) =>
            {
                onExecute();
                return Task.CompletedTask;
            });
}
