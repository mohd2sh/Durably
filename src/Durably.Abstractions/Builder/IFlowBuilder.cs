namespace Durably.Builder;
public interface IFlowBuilder<TState>
{
    string Name { get; }

    IFlowBuilder<TState> Step(string key, Func<TState, CancellationToken, Task> body, Action<IStepOptions>? configure = null);

    IFlowBuilder<TState> Step<TStep>(string? key = null, Action<IStepOptions>? configure = null) where TStep : IStep<TState>;

    IFlowBuilder<TState> StepIf(Func<TState, bool> condition, string key, Func<TState, CancellationToken, Task> body, Action<IStepOptions>? configure = null);

    IFlowBuilder<TState> StepIf<TStep>(Func<TState, bool> condition, string? key = null, Action<IStepOptions>? configure = null) where TStep : IStep<TState>;

    IChoiceBuilder<TState, TKey> Choose<TKey>(Func<TState, TKey> selector);

    IFlowBuilder<TState> OnSuccess(Func<TState, CancellationToken, Task> handler);

    IFlowBuilder<TState> OnSuccess(Action<TState> handler);

    IFlowBuilder<TState> OnFailure(Func<TState, Exception?, CancellationToken, Task> handler);

    IFlowBuilder<TState> OnFailure(Action<TState, Exception?> handler);
}
