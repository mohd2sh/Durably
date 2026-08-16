namespace Durably.Execution;

/// <summary>Thrown when a checkpoint write fails because this runner no longer owns the execution lease.</summary>
public sealed class LeaseLostException : Exception
{
    public LeaseLostException(string flowName, string runId, string? instanceId = null)
        : base($"Runner lost the lease for flow '{flowName}' run '{runId}'.")
    {
        FlowName = flowName;
        RunId = runId;
        InstanceId = instanceId;
    }

    public string FlowName { get; }

    public string RunId { get; }

    public string? InstanceId { get; }
}
