using Dapper;

namespace Durably;

/// <summary>
/// Dapper-backed <see cref="ITraceStore"/>. Opens a fresh connection per batch on a dedicated factory,
/// separate from checkpoint I/O.
/// </summary>
internal sealed class DapperTraceStore : ITraceStore
{
    private readonly DapperConnectionRunner _runner;

    public DapperTraceStore(IDbConnectionFactory connectionFactory, ISqlDialect dialect, DapperStoreOptions? options = null)
    {
        _runner = new DapperConnectionRunner(connectionFactory, dialect, options ?? new DapperStoreOptions());
    }

    public async Task AppendAsync(IReadOnlyList<TraceRecord> records, CancellationToken cancellationToken)
    {
        if (records is null || records.Count == 0)
        {
            return;
        }

        using var connection = await _runner.OpenAsync(cancellationToken).ConfigureAwait(false);
        foreach (var record in records)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(_runner.Dialect.AppendTraceSql, TraceRow.From(record),
                    cancellationToken: cancellationToken, commandTimeout: _runner.Options.CommandTimeoutSeconds)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<TraceRecord>> LoadAsync(string flowName, string instanceId, CancellationToken cancellationToken)
    {
        using var connection = await _runner.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<TraceRow>(
            new CommandDefinition(_runner.Dialect.LoadTracesSql, new { FlowName = flowName, InstanceId = instanceId },
                cancellationToken: cancellationToken, commandTimeout: _runner.Options.CommandTimeoutSeconds)).ConfigureAwait(false);
        return rows.Select(r => r.ToRecord()).ToList();
    }
}
