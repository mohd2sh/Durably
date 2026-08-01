using Microsoft.EntityFrameworkCore;

namespace Durably;

/// <summary>EF Core-backed read-only execution queries for the observability UI.</summary>
internal sealed class EfExecutionQuery : IExecutionQuery
{
    private readonly IDbContextFactory<DurablyDbContext> _contextFactory;
    private readonly EfStoreOptions _options;

    public EfExecutionQuery(IDbContextFactory<DurablyDbContext> contextFactory, EfStoreOptions options)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<PagedResult<ExecutionSummary>> SearchAsync(
        ExecutionSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        if (criteria is null)
        {
            throw new ArgumentNullException(nameof(criteria));
        }

        var take = NormalizeTake(criteria.Take);
        var skip = Math.Max(0, criteria.Skip);

        await EfDatabaseInitializer.EnsureReadyAsync(_contextFactory, _options, cancellationToken).ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var query = ApplyFilters(context.Executions.AsNoTracking(), criteria);
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(e => e.UpdatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<ExecutionSummary>
        {
            Items = items.Select(e => ExecutionProjectionMapper.ToSummary(ExecutionMapper.ToRecord(e))).ToList(),
            TotalCount = totalCount,
            Skip = skip,
            Take = take
        };
    }

    public async Task<ExecutionDetail?> GetAsync(
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

        await EfDatabaseInitializer.EnsureReadyAsync(_contextFactory, _options, cancellationToken).ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        ExecutionEntity? entity;
        if (!string.IsNullOrWhiteSpace(runId))
        {
            entity = await context.Executions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    e => e.FlowName == flowName && e.InstanceId == instanceId && e.RunId == runId,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            entity = await context.Executions
                .AsNoTracking()
                .Where(e => e.FlowName == flowName && e.InstanceId == instanceId)
                .OrderByDescending(e => e.UpdatedAt)
                .ThenByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return entity is null ? null : ExecutionProjectionMapper.ToDetail(ExecutionMapper.ToRecord(entity));
    }

    public async Task<IReadOnlyList<ExecutionSummary>> ListRunsAsync(
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

        await EfDatabaseInitializer.EnsureReadyAsync(_contextFactory, _options, cancellationToken).ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var items = await context.Executions
            .AsNoTracking()
            .Where(e => e.FlowName == flowName && e.InstanceId == instanceId)
            .OrderByDescending(e => e.UpdatedAt)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return items.Select(e => ExecutionProjectionMapper.ToSummary(ExecutionMapper.ToRecord(e))).ToList();
    }

    private static IQueryable<ExecutionEntity> ApplyFilters(
        IQueryable<ExecutionEntity> query,
        ExecutionSearchCriteria criteria)
    {
        if (!string.IsNullOrWhiteSpace(criteria.FlowName))
        {
            var pattern = criteria.FlowName.Trim();
            query = query.Where(e => EF.Functions.Like(e.FlowName, $"%{pattern}%"));
        }

        if (criteria.Status is not null)
        {
            var status = (int)criteria.Status;
            query = query.Where(e => e.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(criteria.InstanceId))
        {
            var pattern = criteria.InstanceId.Trim();
            query = query.Where(e => EF.Functions.Like(e.InstanceId, $"%{pattern}%"));
        }

        if (!string.IsNullOrWhiteSpace(criteria.RunId))
        {
            var pattern = criteria.RunId.Trim();
            query = query.Where(e => EF.Functions.Like(e.RunId, $"%{pattern}%"));
        }

        if (criteria.From is not null)
        {
            var from = criteria.From.Value.UtcDateTime;
            query = query.Where(e => e.UpdatedAt >= from);
        }

        if (criteria.To is not null)
        {
            var to = criteria.To.Value.UtcDateTime;
            query = query.Where(e => e.UpdatedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(criteria.MetadataKey)
            && !string.IsNullOrWhiteSpace(criteria.MetadataValue))
        {
            var key = criteria.MetadataKey.Trim();
            var value = criteria.MetadataValue.Trim();
            var token = $"\"{key}\":\"{value}\"";
            query = query.Where(e => e.MetadataJson != null && e.MetadataJson.Contains(token));
        }

        return query;
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
