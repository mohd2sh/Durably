namespace Durably.Queries;
/// <summary>Read-only queries over persisted flow instances for observability UI.</summary>
public interface IExecutionQuery
{
    Task<PagedResult<ExecutionSummary>> SearchAsync(ExecutionSearchCriteria criteria, CancellationToken cancellationToken);

    Task<ExecutionDetail?> GetAsync(string flowName, string instanceId, CancellationToken cancellationToken);
}
