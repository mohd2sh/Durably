using Dapper;

namespace Durably;

/// <summary>Dapper-backed read-only execution queries for the observability UI.</summary>
internal sealed class DapperExecutionQuery : IExecutionQuery
{
    private readonly DapperConnectionRunner _runner;
    private readonly ExecutionSearchSqlBuilder _searchSqlBuilder;

    public DapperExecutionQuery(IDbConnectionFactory connectionFactory, ISqlDialect dialect, DapperStoreOptions? options = null)
    {
        _runner = new DapperConnectionRunner(connectionFactory, dialect, options ?? new DapperStoreOptions());
        _searchSqlBuilder = new ExecutionSearchSqlBuilder(dialect);
    }

    public async Task<PagedResult<ExecutionSummary>> SearchAsync(ExecutionSearchCriteria criteria, CancellationToken cancellationToken)
    {
        if (criteria is null)
        {
            throw new ArgumentNullException(nameof(criteria));
        }

        var take = NormalizeTake(criteria.Take);
        var skip = Math.Max(0, criteria.Skip);
        var (searchSql, parameters) = _searchSqlBuilder.BuildSearch(criteria);
        var (countSql, countParameters) = _searchSqlBuilder.BuildCount(criteria);

        using var connection = await _runner.OpenAsync(cancellationToken).ConfigureAwait(false);
        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, countParameters, cancellationToken: cancellationToken,
                commandTimeout: _runner.Options.CommandTimeoutSeconds)).ConfigureAwait(false);

        var rows = await connection.QueryAsync<ExecutionRow>(
            new CommandDefinition(searchSql, parameters, cancellationToken: cancellationToken,
                commandTimeout: _runner.Options.CommandTimeoutSeconds)).ConfigureAwait(false);

        return new PagedResult<ExecutionSummary>
        {
            Items = rows.Select(row => ExecutionProjectionMapper.ToSummary(row.ToRecord())).ToList(),
            TotalCount = totalCount,
            Skip = skip,
            Take = take
        };
    }

    public async Task<ExecutionDetail?> GetAsync(string flowName, string instanceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(flowName))
        {
            throw new ArgumentException("Flow name is required.", nameof(flowName));
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance id is required.", nameof(instanceId));
        }

        using var connection = await _runner.OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QueryFirstOrDefaultAsync<ExecutionRow>(
            new CommandDefinition(_runner.Dialect.LoadSql, new { FlowName = flowName, InstanceId = instanceId },
                cancellationToken: cancellationToken, commandTimeout: _runner.Options.CommandTimeoutSeconds)).ConfigureAwait(false);

        return row is null ? null : ExecutionProjectionMapper.ToDetail(row.ToRecord());
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
