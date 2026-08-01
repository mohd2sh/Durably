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
        ValidateFlowAndInstance(flowName, instanceId);
        var record = await _store.LoadLatestAsync(flowName, instanceId, cancellationToken).ConfigureAwait(false);
        return record is null ? null : ToStatus(record);
    }

    public async Task<ExecutionStatusInfo?> GetStatusAsync(
        string flowName,
        string instanceId,
        string runId,
        CancellationToken cancellationToken = default)
    {
        ValidateFlowAndInstance(flowName, instanceId);
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("Run id is required.", nameof(runId));
        }

        var record = await _store.LoadAsync(flowName, runId, cancellationToken).ConfigureAwait(false);
        if (record is null
            || !string.Equals(record.InstanceId, instanceId, StringComparison.Ordinal))
        {
            return null;
        }

        return ToStatus(record);
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

        var startOptions = options ?? new FlowStartOptions();

        var open = await _store.FindOpenAsync(flowName, instanceId, cancellationToken).ConfigureAwait(false);
        if (open is not null)
        {
            return OpenConflictResult(flowName, instanceId, open, startOptions.OpenConflict);
        }

        var state = initialState ?? new TState();
        var runId = Guid.NewGuid().ToString("N");
        var record = new ExecutionRecord
        {
            FlowName = flowName,
            RunId = runId,
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
            // Lost the open-run unique index to a concurrent start — map to policy, never create again.
            var winner = await _store.LoadLatestAsync(flowName, instanceId, cancellationToken).ConfigureAwait(false);
            if (winner is not null)
            {
                return OpenConflictResult(flowName, instanceId, winner, startOptions.OpenConflict);
            }

            throw;
        }

        _workSignal.Notify();
        return FlowStartResult.Created(flowName, instanceId, runId);
    }

    private FlowStartResult OpenConflictResult(
        string flowName,
        string instanceId,
        ExecutionRecord open,
        OpenConflictPolicy policy)
    {
        _workSignal.Notify();
        return policy == OpenConflictPolicy.Skip
            ? FlowStartResult.Skipped(flowName, instanceId, open.RunId, open.Status)
            : FlowStartResult.Conflict(flowName, instanceId, open.RunId, open.Status);
    }

    private static ExecutionStatusInfo ToStatus(ExecutionRecord record) => new()
    {
        FlowName = record.FlowName,
        RunId = record.RunId,
        InstanceId = record.InstanceId,
        Status = record.Status,
        CurrentStep = record.CurrentStep,
        FailedStep = record.FailedStep,
        ErrorMessage = record.ErrorMessage,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt
    };

    private static void ValidateFlowAndInstance(string flowName, string instanceId)
    {
        if (string.IsNullOrWhiteSpace(flowName))
        {
            throw new ArgumentException("Flow name is required.", nameof(flowName));
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance id is required.", nameof(instanceId));
        }
    }
}
