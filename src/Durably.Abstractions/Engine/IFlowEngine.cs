namespace Durably.Engine;
public interface IFlowEngine
{
    Task<FlowStartResult> StartAsync<TFlow, TState>(
        string instanceId,
        TState? initialState = null,
        FlowStartOptions? options = null,
        CancellationToken cancellationToken = default)
        where TFlow : class, IFlow<TState>
        where TState : class, new();

    Task<FlowStartResult> StartAsync<TState>(
        IFlowBuilder<TState> flow,
        string instanceId,
        TState? initialState = null,
        FlowStartOptions? options = null,
        CancellationToken cancellationToken = default)
        where TState : class, new();

    Task<FlowStartResult> StartAsync<TState>(
        IFlow<TState> flow,
        string instanceId,
        TState? initialState = null,
        FlowStartOptions? options = null,
        CancellationToken cancellationToken = default)
        where TState : class, new();

    /// <summary>Status of the latest run for this instance, or null if none.</summary>
    Task<ExecutionStatusInfo?> GetStatusAsync(
        string flowName,
        string instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>Status of a specific run.</summary>
    Task<ExecutionStatusInfo?> GetStatusAsync(
        string flowName,
        string instanceId,
        string runId,
        CancellationToken cancellationToken = default);

    Task<ExecutionStatusInfo?> GetStatusAsync<TFlow>(
        string instanceId,
        CancellationToken cancellationToken = default);
}
