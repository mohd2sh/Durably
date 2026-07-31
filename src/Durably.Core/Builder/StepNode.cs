namespace Durably.Builder;
/// <summary>
/// A single compiled node in a flow pipeline. Conditional steps and <c>Choose</c> branches are
/// flattened into a linear node list where each node may carry a <see cref="Guard"/>; the engine
/// skips a node whose guard evaluates false. Resume relies on the node index, so guards on already
/// passed nodes are never re-evaluated.
/// </summary>
internal sealed class StepNode<TState>
{
    public StepNode(string key, Func<TState, bool>? guard, StepExecutor<TState> execute, RetryPolicy retry, TimeSpan? timeout)
    {
        Key = key;
        Guard = guard;
        Execute = execute;
        Retry = retry;
        Timeout = timeout;
    }

    public string Key { get; }

    /// <summary>Optional predicate; when present and false at runtime the node is skipped.</summary>
    public Func<TState, bool>? Guard { get; }

    public StepExecutor<TState> Execute { get; }

    public RetryPolicy Retry { get; }

    public TimeSpan? Timeout { get; }
}
