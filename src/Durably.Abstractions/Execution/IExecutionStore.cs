namespace Durably.Execution;

public interface IExecutionStore
{
    /// <summary>Load one execution by flow + run id.</summary>
    Task<ExecutionRecord?> LoadAsync(string flowName, string runId, CancellationToken cancellationToken);

    /// <summary>The open (Pending/Running) run for this instance, if any.</summary>
    Task<ExecutionRecord?> FindOpenAsync(string flowName, string instanceId, CancellationToken cancellationToken);

    /// <summary>Most recently updated run for this instance, or null.</summary>
    Task<ExecutionRecord?> LoadLatestAsync(string flowName, string instanceId, CancellationToken cancellationToken);

    Task CreateAsync(ExecutionRecord record, CancellationToken cancellationToken);

    Task SaveCheckpointAsync(ExecutionRecord record, string runnerId, DateTimeOffset leaseUntil, CancellationToken cancellationToken);

    Task<bool> TryAcquireLeaseAsync(
        string flowName,
        string runId,
        string runnerId,
        DateTimeOffset leaseUntil,
        CancellationToken cancellationToken);

    Task ReleaseLeaseAsync(string flowName, string runId, string runnerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ExecutionRecord>> ClaimDueAsync(
        string runnerId,
        DateTimeOffset leaseUntil,
        int batchSize,
        CancellationToken cancellationToken);
}
