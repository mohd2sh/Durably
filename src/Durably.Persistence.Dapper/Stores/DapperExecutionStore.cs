using Dapper;
using Microsoft.Data.SqlClient;
#if NET8_0_OR_GREATER
using Npgsql;
#endif

namespace Durably;

/// <summary>
/// A driver-agnostic <see cref="IExecutionStore"/> built on Dapper. Each operation opens a fresh
/// connection from the factory. Optimistic concurrency and execution leases are enforced in SQL.
/// </summary>
internal sealed class DapperExecutionStore : IExecutionStore
{
    private readonly DapperConnectionRunner _runner;

    public DapperExecutionStore(IDbConnectionFactory connectionFactory, ISqlDialect dialect, DapperStoreOptions? options = null)
    {
        _runner = new DapperConnectionRunner(connectionFactory, dialect, options ?? new DapperStoreOptions());
    }

    public static DapperExecutionStore ForSqlServer(string connectionString, DapperStoreOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        return new DapperExecutionStore(
            new DelegateDbConnectionFactory(() => new SqlConnection(connectionString)),
            new SqlServerDialect(),
            options);
    }

#if NET8_0_OR_GREATER
    public static DapperExecutionStore ForPostgreSql(string connectionString, DapperStoreOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        return new DapperExecutionStore(
            new DelegateDbConnectionFactory(() => new NpgsqlConnection(connectionString)),
            new PostgreSqlDialect(),
            options);
    }
#endif

    public async Task<ExecutionRecord?> LoadAsync(string flowName, string instanceId, CancellationToken cancellationToken)
    {
        using var connection = await _runner.OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QueryFirstOrDefaultAsync<ExecutionRow>(
            new CommandDefinition(_runner.Dialect.LoadSql, new { FlowName = flowName, InstanceId = instanceId },
                cancellationToken: cancellationToken, commandTimeout: _runner.Options.CommandTimeoutSeconds)).ConfigureAwait(false);
        return row?.ToRecord();
    }

    public async Task CreateAsync(ExecutionRecord record, CancellationToken cancellationToken)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        using var connection = await _runner.OpenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(_runner.Dialect.InsertSql, ExecutionRow.From(record),
                    cancellationToken: cancellationToken, commandTimeout: _runner.Options.CommandTimeoutSeconds)).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsDuplicateKey(ex))
        {
            throw new ExecutionAlreadyExistsException(record.FlowName, record.InstanceId);
        }
    }

    public async Task SaveCheckpointAsync(
        ExecutionRecord record,
        string runnerId,
        DateTimeOffset leaseUntil,
        CancellationToken cancellationToken)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        if (string.IsNullOrWhiteSpace(runnerId))
        {
            throw new ArgumentException("Runner id is required.", nameof(runnerId));
        }

        using var connection = await _runner.OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = ExecutionRow.From(record);
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                _runner.Dialect.UpdateCheckpointSql,
                new
                {
                    row.FlowName,
                    row.InstanceId,
                    row.Status,
                    row.CurrentStep,
                    row.ContextJson,
                    row.StepPathHash,
                    row.Attempts,
                    row.FailedStep,
                    row.ErrorMessage,
                    row.Version,
                    row.UpdatedAt,
                    LockedUntil = leaseUntil.UtcDateTime,
                    RunnerId = runnerId
                },
                cancellationToken: cancellationToken,
                commandTimeout: _runner.Options.CommandTimeoutSeconds)).ConfigureAwait(false);

        if (affected == 0)
        {
            var current = await LoadAsync(record.FlowName, record.InstanceId, cancellationToken).ConfigureAwait(false);
            if (current is null || string.Equals(current.LockedBy, runnerId, StringComparison.Ordinal))
            {
                throw new ConcurrencyConflictException(record.FlowName, record.InstanceId);
            }

            throw new LeaseLostException(record.FlowName, record.InstanceId);
        }

        record.Version += 1;
        record.LockedBy = runnerId;
        record.LockedUntil = leaseUntil;
    }

    public async Task<bool> TryAcquireLeaseAsync(
        string flowName,
        string instanceId,
        string runnerId,
        DateTimeOffset leaseUntil,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runnerId))
        {
            throw new ArgumentException("Runner id is required.", nameof(runnerId));
        }

        var now = DateTimeOffset.UtcNow;
        using var connection = await _runner.OpenAsync(cancellationToken).ConfigureAwait(false);
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                _runner.Dialect.AcquireLeaseSql,
                new
                {
                    FlowName = flowName,
                    InstanceId = instanceId,
                    RunnerId = runnerId,
                    LockedUntil = leaseUntil.UtcDateTime,
                    UpdatedAt = now.UtcDateTime,
                    Now = now.UtcDateTime
                },
                cancellationToken: cancellationToken,
                commandTimeout: _runner.Options.CommandTimeoutSeconds)).ConfigureAwait(false);

        return affected > 0;
    }

    public async Task ReleaseLeaseAsync(string flowName, string instanceId, string runnerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runnerId))
        {
            throw new ArgumentException("Runner id is required.", nameof(runnerId));
        }

        using var connection = await _runner.OpenAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(
            new CommandDefinition(
                _runner.Dialect.ReleaseLeaseSql,
                new
                {
                    FlowName = flowName,
                    InstanceId = instanceId,
                    RunnerId = runnerId,
                    UpdatedAt = DateTimeOffset.UtcNow.UtcDateTime
                },
                cancellationToken: cancellationToken,
                commandTimeout: _runner.Options.CommandTimeoutSeconds)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ExecutionRecord>> ClaimDueAsync(
        string runnerId,
        DateTimeOffset leaseUntil,
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runnerId))
        {
            throw new ArgumentException("Runner id is required.", nameof(runnerId));
        }

        if (batchSize <= 0)
        {
            return Array.Empty<ExecutionRecord>();
        }

        var now = DateTimeOffset.UtcNow.UtcDateTime;
        using var connection = await _runner.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<ExecutionRecord>(
            new CommandDefinition(
                _runner.Dialect.ClaimDueSql,
                new
                {
                    RunnerId = runnerId,
                    LockedUntil = leaseUntil.UtcDateTime,
                    BatchSize = batchSize,
                    Pending = (int)ExecutionStatus.Pending,
                    Running = (int)ExecutionStatus.Running,
                    Now = now,
                    UpdatedAt = now
                },
                cancellationToken: cancellationToken,
                commandTimeout: _runner.Options.CommandTimeoutSeconds)).ConfigureAwait(false);

        return rows.AsList();
    }

    private static bool IsDuplicateKey(Exception ex)
    {
        if (ex is SqlException sqlException && sqlException.Number is 2627 or 2601)
        {
            return true;
        }

#if NET8_0_OR_GREATER
        if (ex is PostgresException postgresException
            && string.Equals(postgresException.SqlState, DurablyLimits.PostgresUniqueViolationSqlState, StringComparison.Ordinal))
        {
            return true;
        }
#endif

        var message = ex.Message;
        return message.IndexOf("PRIMARY KEY", StringComparison.OrdinalIgnoreCase) >= 0
            || message.IndexOf("duplicate key", StringComparison.OrdinalIgnoreCase) >= 0
            || message.IndexOf("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) >= 0
            || message.IndexOf(DurablyLimits.PostgresUniqueViolationSqlState, StringComparison.Ordinal) >= 0;
    }
}
