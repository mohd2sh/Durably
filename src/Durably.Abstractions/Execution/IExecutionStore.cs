namespace Durably.Execution;
public interface IExecutionStore
{
    Task<ExecutionRecord?> LoadAsync(string flowName, string instanceId, CancellationToken cancellationToken);

    Task CreateAsync(ExecutionRecord record, CancellationToken cancellationToken);

    Task SaveCheckpointAsync(ExecutionRecord record, string runnerId, DateTimeOffset leaseUntil, CancellationToken cancellationToken);

    Task<bool> TryAcquireLeaseAsync(
        string flowName,
        string instanceId,
        string runnerId,
        DateTimeOffset leaseUntil,
        CancellationToken cancellationToken);

    Task ReleaseLeaseAsync(string flowName, string instanceId, string runnerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ExecutionRecord>> ClaimDueAsync(
        string runnerId,
        DateTimeOffset leaseUntil,
        int batchSize,
        CancellationToken cancellationToken);
}
