namespace Durably.Execution;
/// <summary>Thrown when a checkpoint write loses an optimistic-concurrency race.</summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string flowName, string instanceId)
        : base($"Concurrency conflict saving flow '{flowName}' instance '{instanceId}'. Another runner advanced it.")
    {
        FlowName = flowName;
        InstanceId = instanceId;
    }

    public string FlowName { get; }

    public string InstanceId { get; }
}
