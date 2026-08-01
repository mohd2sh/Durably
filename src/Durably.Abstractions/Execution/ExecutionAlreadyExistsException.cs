namespace Durably.Execution;

/// <summary>
/// Thrown when <see cref="IExecutionStore.CreateAsync"/> cannot insert — duplicate run id,
/// or a unique open-run constraint for the same instance id.
/// </summary>
public sealed class ExecutionAlreadyExistsException : Exception
{
    public ExecutionAlreadyExistsException(string flowName, string instanceId, string? runId = null)
        : base(runId is null
            ? $"Flow '{flowName}' instance '{instanceId}' already has a conflicting execution."
            : $"Flow '{flowName}' run '{runId}' (instance '{instanceId}') already exists.")
    {
        FlowName = flowName;
        InstanceId = instanceId;
        RunId = runId;
    }

    public string FlowName { get; }

    public string InstanceId { get; }

    public string? RunId { get; }
}
