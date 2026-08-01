namespace Durably.Queries;

/// <summary>Read-only queries over persisted flow executions for observability UI.</summary>
public interface IExecutionQuery
{
    Task<PagedResult<ExecutionSummary>> SearchAsync(ExecutionSearchCriteria criteria, CancellationToken cancellationToken);

    /// <summary>Load one run. When <paramref name="runId"/> is null, returns the latest run for the instance.</summary>
    Task<ExecutionDetail?> GetAsync(string flowName, string instanceId, string? runId, CancellationToken cancellationToken);

    /// <summary>All runs for a business instance id, newest first.</summary>
    Task<IReadOnlyList<ExecutionSummary>> ListRunsAsync(string flowName, string instanceId, CancellationToken cancellationToken);
}
