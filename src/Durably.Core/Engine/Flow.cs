namespace Durably.Engine;
/// <summary>Entry point for composing an inline (functional) flow.</summary>
public static class Flow
{
    /// <summary>
    /// Begin building a flow whose identity is <c>typeof(TState).FullName</c>.
    /// Use only when a single fluent flow exists per state type.
    /// </summary>
    public static FlowBuilder<TState> For<TState>()
        where TState : class
        => new(FlowIdentity.ForState<TState>());

    /// <summary>
    /// Begin building a flow whose identity is <c>typeof(TFlow).FullName</c>.
    /// Use when multiple fluent flows share the same state type.
    /// </summary>
    public static FlowBuilder<TState> For<TFlow, TState>()
        where TState : class
        => new(FlowIdentity.ForFlow<TFlow>());
}
