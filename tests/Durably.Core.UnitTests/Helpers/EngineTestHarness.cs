namespace Durably.Core.UnitTests;

/// <summary>
/// Wraps the async enqueue model (<see cref="FlowEngine.StartAsync"/> + <see cref="ExecutionProcessor"/>)
/// behind a single call so tests can start and drive a flow to its next checkpoint synchronously.
/// </summary>
public sealed class EngineTestHarness
{
    public InMemoryExecutionStore Store { get; }

    public FlowEngine Engine { get; }

    internal ExecutionProcessor Processor { get; }

    internal FlowRegistry Registry { get; }

    public string RunnerId { get; }

    public TimeSpan LeaseDuration { get; } = TestLimits.DefaultLeaseDuration;

    private EngineTestHarness(
        InMemoryExecutionStore store,
        FlowRegistry registry,
        string runnerId,
        ITraceSink? trace,
        StepDefaults? stepDefaults)
    {
        Store = store;
        Registry = registry;
        RunnerId = runnerId;
        Engine = new FlowEngine(Store);
        Processor = new ExecutionProcessor(Store, Registry, trace: trace, stepDefaults: stepDefaults);
    }

    internal static EngineTestHarness Create(ITraceSink? trace = null, StepDefaults? stepDefaults = null)
        => new(new InMemoryExecutionStore(), new FlowRegistry(), "test-runner", trace, stepDefaults);

    /// <summary>Creates a second harness contending for the same store/registry under a different runner id.</summary>
    public EngineTestHarness CreateContender(string runnerId)
        => new(Store, Registry, runnerId, null, null);

    public void Register<TState>(IFlowBuilder<TState> flow) where TState : class, new()
    {
        if (flow is not FlowBuilder<TState> typed)
        {
            throw new ArgumentException("Flow must be created via Flow.For<TState>().", nameof(flow));
        }

        Registry.Register(FlowRegistration<TState>.FromBuilder(typed));
    }

    public void Register<TState>(IFlow<TState> flow) where TState : class, new()
        => Registry.Register(FlowRegistration<TState>.FromConfigure(FlowName(flow.GetType()), builder => flow.Build(builder)));

    public async Task<FlowRunResult> StartAndProcessAsync<TState>(
        IFlowBuilder<TState> flow,
        string instanceId,
        TState? state = null,
        FlowStartOptions? options = null)
        where TState : class, new()
    {
        Register(flow);
        await Engine.StartAsync(flow, instanceId, state, options);
        return await ProcessAsync(flow.Name, instanceId);
    }

    public async Task<FlowRunResult> StartAndProcessAsync<TState>(
        IFlow<TState> flow,
        string instanceId,
        TState? state = null,
        FlowStartOptions? options = null)
        where TState : class, new()
    {
        Register(flow);
        await Engine.StartAsync(flow, instanceId, state, options);
        return await ProcessAsync(FlowName(flow.GetType()), instanceId);
    }

    /// <summary>
    /// Drives an existing instance to its next checkpoint. Tries <see cref="IExecutionStore.ClaimDueAsync"/>
    /// first (covers Pending/Running with an expired lease); if the instance is not in that batch, falls back
    /// to load + <see cref="IExecutionStore.TryAcquireLeaseAsync"/> (covers Failed resume and lease conflicts).
    /// </summary>
    public async Task<FlowRunResult> ProcessAsync(string flowName, string instanceId)
    {
        var leaseUntil = DateTimeOffset.UtcNow.Add(LeaseDuration);
        var claimed = await Store.ClaimDueAsync(RunnerId, leaseUntil, TestLimits.ClaimBatchSize, CancellationToken.None);
        var match = claimed.FirstOrDefault(r => r.FlowName == flowName && r.InstanceId == instanceId);
        if (match is not null)
        {
            return await Processor.ProcessAsync(match, RunnerId, LeaseDuration);
        }

        var record = await Store.LoadAsync(flowName, instanceId, CancellationToken.None);
        if (record is null)
        {
            throw new InvalidOperationException($"No execution found for flow '{flowName}' instance '{instanceId}'.");
        }

        if (!await Store.TryAcquireLeaseAsync(flowName, instanceId, RunnerId, leaseUntil, CancellationToken.None))
        {
            return FlowRunResult.AlreadyRunning();
        }

        var leased = await Store.LoadAsync(flowName, instanceId, CancellationToken.None)
            ?? throw new InvalidOperationException("Execution disappeared after lease acquisition.");
        return await Processor.ProcessAsync(leased, RunnerId, LeaseDuration);
    }

    /// <summary>Reloads persisted state for an instance, since processing always runs against a fresh deserialized copy.</summary>
    public async Task<TState> LoadStateAsync<TState>(string flowName, string instanceId) where TState : class, new()
    {
        var record = await Store.LoadAsync(flowName, instanceId, CancellationToken.None)
            ?? throw new InvalidOperationException($"No execution found for flow '{flowName}' instance '{instanceId}'.");
        return (TState)new JsonStateSerializer().Deserialize(record.ContextJson, typeof(TState))!;
    }

    private static string FlowName(Type type) => type.FullName ?? type.Name;
}
