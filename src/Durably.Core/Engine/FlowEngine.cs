namespace Durably.Engine;
public sealed class FlowEngine : IFlowEngine
{
    private readonly IExecutionStore _store;
    private readonly IStateSerializer _serializer;
    private readonly IExecutionWorkSignal _workSignal;

    public FlowEngine(
        IExecutionStore store,
        IStateSerializer? serializer = null,
        IExecutionWorkSignal? workSignal = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _serializer = serializer ?? new JsonStateSerializer();
        _workSignal = workSignal ?? new ExecutionWorkSignal();
    }

    public Task<FlowStartResult> StartAsync<TFlow, TState>(
        string instanceId,
        TState? initialState = null,
        FlowStartOptions? options = null,
        CancellationToken cancellationToken = default)
        where TFlow : class, IFlow<TState>
        where TState : class, new()
    {
        var flowName = FlowIdentity.ForFlow<TFlow>();
        return StartCoreAsync(flowName, instanceId, initialState, options, cancellationToken);
    }

    public Task<FlowStartResult> StartAsync<TState>(
        IFlowBuilder<TState> flow,
        string instanceId,
        TState? initialState = null,
        FlowStartOptions? options = null,
        CancellationToken cancellationToken = default)
        where TState : class, new()
    {
        if (flow is null)
        {
            throw new ArgumentNullException(nameof(flow));
        }

        return StartCoreAsync(flow.Name, instanceId, initialState, options, cancellationToken);
    }

    public Task<FlowStartResult> StartAsync<TState>(
        IFlow<TState> flow,
        string instanceId,
        TState? initialState = null,
        FlowStartOptions? options = null,
        CancellationToken cancellationToken = default)
        where TState : class, new()
    {
        if (flow is null)
        {
            throw new ArgumentNullException(nameof(flow));
        }

        var flowName = FlowIdentity.FromType(flow.GetType());
        return StartCoreAsync(flowName, instanceId, initialState, options, cancellationToken);
    }

    public async Task<ExecutionStatusInfo?> GetStatusAsync(
        string flowName,
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(flowName))
        {
            throw new ArgumentException("Flow name is required.", nameof(flowName));
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance id is required.", nameof(instanceId));
        }

        var record = await _store.LoadAsync(flowName, instanceId, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        return new ExecutionStatusInfo
        {
            FlowName = record.FlowName,
            InstanceId = record.InstanceId,
            Status = record.Status,
            CurrentStep = record.CurrentStep,
            FailedStep = record.FailedStep,
            ErrorMessage = record.ErrorMessage,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }

    public Task<ExecutionStatusInfo?> GetStatusAsync<TFlow>(
        string instanceId,
        CancellationToken cancellationToken = default)
        => GetStatusAsync(FlowIdentity.ForFlow<TFlow>(), instanceId, cancellationToken);

    private async Task<FlowStartResult> StartCoreAsync<TState>(
        string flowName,
        string instanceId,
        TState? initialState,
        FlowStartOptions? options,
        CancellationToken cancellationToken)
        where TState : class, new()
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance id must be provided.", nameof(instanceId));
        }

        var existing = await _store.LoadAsync(flowName, instanceId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            _workSignal.Notify();
            return FlowStartResult.AlreadyExists(flowName, instanceId, existing.Status);
        }

        var state = initialState ?? new TState();
        var startOptions = options ?? new FlowStartOptions();
        var record = new ExecutionRecord
        {
            FlowName = flowName,
            InstanceId = instanceId,
            Status = ExecutionStatus.Pending,
            CurrentStep = 0,
            ContextJson = _serializer.Serialize(state),
            MetadataJson = ExecutionMetadataSerializer.Serialize(startOptions.Metadata),
            Version = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await _store.CreateAsync(record, cancellationToken).ConfigureAwait(false);
        }
        catch (ExecutionAlreadyExistsException)
        {
            existing = await _store.LoadAsync(flowName, instanceId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Execution record disappeared after duplicate create.");
            _workSignal.Notify();
            return FlowStartResult.AlreadyExists(flowName, instanceId, existing.Status);
        }

        _workSignal.Notify();
        return FlowStartResult.Created(flowName, instanceId);
    }
}
