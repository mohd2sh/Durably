namespace Durably.Execution;

/// <summary>
/// Thrown when another runner already holds the lease for a flow run and the caller
/// cannot proceed (for example when a conflict must surface to the application).
/// </summary>
public sealed class FlowInstanceBusyException : Exception
{
    public FlowInstanceBusyException(string flowName, string runId, string? instanceId = null)
        : base($"Flow '{flowName}' run '{runId}' is already running on another runner.")
    {
        FlowName = flowName;
        RunId = runId;
        InstanceId = instanceId;
    }

    public string FlowName { get; }

    public string RunId { get; }

    public string? InstanceId { get; }
}
