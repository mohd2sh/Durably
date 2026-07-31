namespace Durably.Execution;
/// <summary>Thrown when a checkpoint write fails because this runner no longer owns the execution lease.</summary>
public sealed class LeaseLostException : Exception
{
    public LeaseLostException(string flowName, string instanceId)
        : base($"Runner lost the lease for flow '{flowName}' instance '{instanceId}'.")
    {
        FlowName = flowName;
        InstanceId = instanceId;
    }

    public string FlowName { get; }

    public string InstanceId { get; }
}
