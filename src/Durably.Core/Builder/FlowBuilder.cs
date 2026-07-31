namespace Durably.Builder;
public sealed class FlowBuilder<TState> : IFlowBuilder<TState>
{
    private readonly List<StepNode<TState>> _nodes = new();
    private readonly Func<TState, bool>? _branchGuard;
    private readonly StepDefaults _stepDefaults;

    public FlowBuilder(string name) : this(name, StepDefaults.None, null)
    {
    }

    internal FlowBuilder(string name, StepDefaults stepDefaults, Func<TState, bool>? branchGuard)
    {
        Name = name;
        _stepDefaults = stepDefaults ?? StepDefaults.None;
        _branchGuard = branchGuard;
    }

    public string Name { get; }

    internal IReadOnlyList<StepNode<TState>> Nodes => _nodes;

    internal Func<TState, CancellationToken, Task>? OnSuccessHandler { get; private set; }

    internal Func<TState, Exception?, CancellationToken, Task>? OnFailureHandler { get; private set; }

    public IFlowBuilder<TState> Step(string key, Func<TState, CancellationToken, Task> body, Action<IStepOptions>? configure = null)
    {
        if (body is null)
        {
            throw new ArgumentNullException(nameof(body));
        }

        StepExecutor<TState> exec = (_, state, _, ct) => body(state, ct);
        AddNode(key, localGuard: null, exec, StepOptions.Resolve(configure, _stepDefaults));
        return this;
    }

    public IFlowBuilder<TState> Step<TStep>(string? key = null, Action<IStepOptions>? configure = null) where TStep : IStep<TState>
    {
        AddNode(key ?? typeof(TStep).Name, localGuard: null, ClassStepExecutor<TStep>(), StepOptions.Resolve(configure, _stepDefaults));
        return this;
    }

    public IFlowBuilder<TState> StepIf(Func<TState, bool> condition, string key, Func<TState, CancellationToken, Task> body, Action<IStepOptions>? configure = null)
    {
        if (condition is null)
        {
            throw new ArgumentNullException(nameof(condition));
        }

        if (body is null)
        {
            throw new ArgumentNullException(nameof(body));
        }

        StepExecutor<TState> exec = (_, state, _, ct) => body(state, ct);
        AddNode(key, condition, exec, StepOptions.Resolve(configure, _stepDefaults));
        return this;
    }

    public IFlowBuilder<TState> StepIf<TStep>(Func<TState, bool> condition, string? key = null, Action<IStepOptions>? configure = null) where TStep : IStep<TState>
    {
        if (condition is null)
        {
            throw new ArgumentNullException(nameof(condition));
        }

        AddNode(key ?? typeof(TStep).Name, condition, ClassStepExecutor<TStep>(), StepOptions.Resolve(configure, _stepDefaults));
        return this;
    }

    public IChoiceBuilder<TState, TKey> Choose<TKey>(Func<TState, TKey> selector)
    {
        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        return new ChoiceBuilder<TState, TKey>(this, selector);
    }

    public IFlowBuilder<TState> OnSuccess(Func<TState, CancellationToken, Task> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        OnSuccessHandler = handler;
        return this;
    }

    public IFlowBuilder<TState> OnSuccess(Action<TState> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        OnSuccessHandler = (state, _) =>
        {
            handler(state);
            return Task.CompletedTask;
        };
        return this;
    }

    public IFlowBuilder<TState> OnFailure(Func<TState, Exception?, CancellationToken, Task> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        OnFailureHandler = handler;
        return this;
    }

    public IFlowBuilder<TState> OnFailure(Action<TState, Exception?> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        OnFailureHandler = (state, error, _) =>
        {
            handler(state, error);
            return Task.CompletedTask;
        };
        return this;
    }

    internal FlowBuilder<TState> CreateBranch(Func<TState, bool> branchGuard)
        => new(Name, _stepDefaults, Combine(_branchGuard, branchGuard));

    internal void AppendCompiledNodes(IEnumerable<StepNode<TState>> nodes)
    {
        foreach (var node in nodes)
        {
            EnsureUniqueKey(node.Key);
            _nodes.Add(node);
        }
    }

    internal void EnsureHasSteps()
    {
        if (_nodes.Count == 0)
        {
            throw new InvalidOperationException($"Flow '{Name}' has no steps.");
        }
    }

    private void AddNode(string key, Func<TState, bool>? localGuard, StepExecutor<TState> exec, StepOptions options)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Step key must be provided.", nameof(key));
        }

        EnsureUniqueKey(key);
        _nodes.Add(new StepNode<TState>(key, Combine(_branchGuard, localGuard), exec, options.RetryPolicy, options.TimeoutValue));
    }

    private void EnsureUniqueKey(string key)
    {
        for (var i = 0; i < _nodes.Count; i++)
        {
            if (string.Equals(_nodes[i].Key, key, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Flow '{Name}' already contains a step with key '{key}'.");
            }
        }
    }

    private static StepExecutor<TState> ClassStepExecutor<TStep>() where TStep : IStep<TState>
        => (services, state, context, ct) => ResolveStep<TStep>(services).ExecuteAsync(state, context, ct);

    private static IStep<TState> ResolveStep<TStep>(IServiceProvider? services) where TStep : IStep<TState>
    {
        if (services is not null && services.GetService(typeof(TStep)) is IStep<TState> resolved)
        {
            return resolved;
        }

        return (IStep<TState>)Activator.CreateInstance(typeof(TStep))!;
    }

    private static Func<TState, bool>? Combine(Func<TState, bool>? left, Func<TState, bool>? right)
    {
        if (left is null)
        {
            return right;
        }

        if (right is null)
        {
            return left;
        }

        return state => left(state) && right(state);
    }
}
