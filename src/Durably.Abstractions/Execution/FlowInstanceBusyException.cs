namespace Durably.Execution;
/// <summary>
/// Thrown when another runner already holds the lease for a flow instance and the caller
/// cannot proceed (for example when a conflict must surface to the application).
/// </summary>
public sealed class FlowInstanceBusyException : Exception
{
    public FlowInstanceBusyException(string flowName, string instanceId)
        : base($"Flow '{flowName}' instance '{instanceId}' is already running on another runner.")
    {
        FlowName = flowName;
        InstanceId = instanceId;
    }

    public string FlowName { get; }

    public string InstanceId { get; }
}
