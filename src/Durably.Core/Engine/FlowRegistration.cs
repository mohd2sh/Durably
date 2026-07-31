namespace Durably.Engine;
internal sealed class FlowRegistration<TState> : IProcessableFlowRegistration
    where TState : class, new()
{
    private readonly FlowBuilder<TState>? _compiled;
    private readonly Action<FlowBuilder<TState>>? _configure;
    private readonly Type? _flowType;

    private FlowRegistration(string name, FlowBuilder<TState>? compiled, Action<FlowBuilder<TState>>? configure, Type? flowType)
    {
        Name = name;
        _compiled = compiled;
        _configure = configure;
        _flowType = flowType;
    }

    public string Name { get; }

    public Type StateType => typeof(TState);

    public static FlowRegistration<TState> FromBuilder(FlowBuilder<TState> builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.EnsureHasSteps();
        return new FlowRegistration<TState>(builder.Name, builder, null, null);
    }

    public static FlowRegistration<TState> FromConfigure(string name, Action<FlowBuilder<TState>> configure)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Flow name is required.", nameof(name));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return new FlowRegistration<TState>(name, null, configure, null);
    }

    public static FlowRegistration<TState> FromFlowType(Type flowType)
    {
        if (flowType is null)
        {
            throw new ArgumentNullException(nameof(flowType));
        }

        var name = FlowIdentity.FromType(flowType);
        return new FlowRegistration<TState>(name, null, null, flowType);
    }

    public Task<FlowRunResult> ProcessAsync(
        ExecutionProcessor processor,
        ExecutionRecord record,
        string runnerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
        => processor.ProcessTypedAsync(this, record, runnerId, leaseDuration, cancellationToken);

    public FlowBuilder<TState> Materialize(IServiceProvider? services, StepDefaults defaults)
    {
        if (_compiled is not null)
        {
            return _compiled;
        }

        var builder = new FlowBuilder<TState>(Name, defaults, null);

        if (_flowType is not null)
        {
            var flow = ResolveFlow(services, _flowType);
            flow.Build(builder);
            builder.EnsureHasSteps();
            return builder;
        }

        _configure!(builder);
        builder.EnsureHasSteps();
        return builder;
    }

    private static IFlow<TState> ResolveFlow(IServiceProvider? services, Type flowType)
    {
        if (services?.GetService(flowType) is IFlow<TState> resolved)
        {
            return resolved;
        }

        try
        {
            return (IFlow<TState>)Activator.CreateInstance(flowType)!;
        }
        catch (MissingMethodException)
        {
            throw new InvalidOperationException(
                $"Flow '{flowType.FullName}' is not registered in DI and could not be activated.");
        }
    }
}
