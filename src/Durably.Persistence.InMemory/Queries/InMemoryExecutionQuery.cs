namespace Durably.Queries;
/// <summary>In-memory read-only execution queries for tests and local development.</summary>
internal sealed class InMemoryExecutionQuery : IExecutionQuery
{
    private readonly InMemoryExecutionStore _store;

    public InMemoryExecutionQuery(InMemoryExecutionStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public Task<PagedResult<ExecutionSummary>> SearchAsync(ExecutionSearchCriteria criteria, CancellationToken cancellationToken)
    {
        if (criteria is null)
        {
            throw new ArgumentNullException(nameof(criteria));
        }

        var take = NormalizeTake(criteria.Take);
        var skip = Math.Max(0, criteria.Skip);

        var filtered = ExecutionSearchFilter.Apply(_store.SnapshotAll(), criteria).ToList();
        var page = filtered
            .Skip(skip)
            .Take(take)
            .Select(ExecutionProjectionMapper.ToSummary)
            .ToList();

        return Task.FromResult(new PagedResult<ExecutionSummary>
        {
            Items = page,
            TotalCount = filtered.Count,
            Skip = skip,
            Take = take
        });
    }

    public Task<ExecutionDetail?> GetAsync(string flowName, string instanceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(flowName))
        {
            throw new ArgumentException("Flow name is required.", nameof(flowName));
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance id is required.", nameof(instanceId));
        }

        var record = _store.SnapshotAll()
            .FirstOrDefault(item =>
                string.Equals(item.FlowName, flowName, StringComparison.Ordinal)
                && string.Equals(item.InstanceId, instanceId, StringComparison.Ordinal));

        return Task.FromResult(record is null ? null : ExecutionProjectionMapper.ToDetail(record));
    }

    private static int NormalizeTake(int take)
    {
        if (take <= 0)
        {
            return QueryDefaults.DefaultPageSize;
        }

        return Math.Min(take, QueryDefaults.MaxPageSize);
    }
}
