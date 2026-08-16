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

    public Task<ExecutionDetail?> GetAsync(
        string flowName,
        string instanceId,
        string? runId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(flowName))
        {
            throw new ArgumentException("Flow name is required.", nameof(flowName));
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance id is required.", nameof(instanceId));
        }

        var matches = _store.SnapshotAll()
            .Where(item =>
                string.Equals(item.FlowName, flowName, StringComparison.Ordinal)
                && string.Equals(item.InstanceId, instanceId, StringComparison.Ordinal));

        ExecutionRecord? record;
        if (!string.IsNullOrWhiteSpace(runId))
        {
            record = matches.FirstOrDefault(item =>
                string.Equals(item.RunId, runId, StringComparison.Ordinal));
        }
        else
        {
            record = matches
                .OrderByDescending(item => item.UpdatedAt)
                .ThenByDescending(item => item.CreatedAt)
                .FirstOrDefault();
        }

        return Task.FromResult(record is null ? null : ExecutionProjectionMapper.ToDetail(record));
    }

    public Task<IReadOnlyList<ExecutionSummary>> ListRunsAsync(
        string flowName,
        string instanceId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(flowName))
        {
            throw new ArgumentException("Flow name is required.", nameof(flowName));
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance id is required.", nameof(instanceId));
        }

        var runs = _store.SnapshotAll()
            .Where(item =>
                string.Equals(item.FlowName, flowName, StringComparison.Ordinal)
                && string.Equals(item.InstanceId, instanceId, StringComparison.Ordinal))
            .OrderByDescending(item => item.UpdatedAt)
            .ThenByDescending(item => item.CreatedAt)
            .Select(ExecutionProjectionMapper.ToSummary)
            .ToList();

        return Task.FromResult<IReadOnlyList<ExecutionSummary>>(runs);
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
