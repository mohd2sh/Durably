namespace Durably.Execution;

/// <summary>Thrown when a checkpoint write loses an optimistic-concurrency race.</summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string flowName, string runId, string? instanceId = null)
        : base($"Concurrency conflict saving flow '{flowName}' run '{runId}'. Another runner advanced it.")
    {
        FlowName = flowName;
        RunId = runId;
        InstanceId = instanceId;
    }

    public string FlowName { get; }

    public string RunId { get; }

    public string? InstanceId { get; }
}
