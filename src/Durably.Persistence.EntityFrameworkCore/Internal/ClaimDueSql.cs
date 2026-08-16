using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Durably;

/// <summary>Provider-specific atomic claim-N SQL (SKIP LOCKED / READPAST + lease).</summary>
internal static class ClaimDueSql
{
    public static Task<IReadOnlyList<ExecutionRecord>> ClaimAsync(
        DurablyDbContext context,
        string schema,
        string runnerId,
        DateTimeOffset leaseUntil,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var provider = context.Database.ProviderName ?? string.Empty;
        if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return ClaimSqlServerAsync(context, schema, runnerId, leaseUntil, batchSize, cancellationToken);
        }

        if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            return ClaimPostgresAsync(context, schema, runnerId, leaseUntil, batchSize, cancellationToken);
        }

        return ClaimSqliteAsync(context, runnerId, leaseUntil, batchSize, cancellationToken);
    }

    private static Task<IReadOnlyList<ExecutionRecord>> ClaimSqlServerAsync(
        DurablyDbContext context,
        string schema,
        string runnerId,
        DateTimeOffset leaseUntil,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var table = QuoteSqlServer(schema) + "." + QuoteSqlServer("Executions");
        var sql =
$@"WITH candidates AS (
    SELECT TOP (@BatchSize) *
    FROM {table} WITH (READPAST, UPDLOCK, ROWLOCK)
    WHERE (Status = @Pending OR Status = @Running)
      AND (LockedUntil IS NULL OR LockedUntil <= SYSUTCDATETIME())
    ORDER BY CreatedAt
)
UPDATE candidates
SET LockedBy = @RunnerId,
    LockedUntil = @LockedUntil,
    UpdatedAt = SYSUTCDATETIME()
OUTPUT inserted.FlowName, inserted.RunId, inserted.InstanceId, inserted.Status, inserted.CurrentStep, inserted.ContextJson,
       inserted.StepPathHash, inserted.Attempts, inserted.FailedStep, inserted.ErrorMessage, inserted.Version, inserted.CreatedAt,
       inserted.UpdatedAt, inserted.LockedBy, inserted.LockedUntil, inserted.MetadataJson;";

        return ExecuteAsync(context, sql, runnerId, leaseUntil, batchSize, cancellationToken);
    }

    private static Task<IReadOnlyList<ExecutionRecord>> ClaimPostgresAsync(
        DurablyDbContext context,
        string schema,
        string runnerId,
        DateTimeOffset leaseUntil,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var table = QuotePostgres(schema) + "." + QuotePostgres("Executions");
        var sql =
$@"UPDATE {table} AS e
SET ""LockedBy"" = @RunnerId,
    ""LockedUntil"" = @LockedUntil,
    ""UpdatedAt"" = (now() AT TIME ZONE 'utc')
WHERE (""FlowName"", ""RunId"") IN (
    SELECT ""FlowName"", ""RunId""
    FROM {table}
    WHERE (""Status"" = @Pending OR ""Status"" = @Running)
      AND (""LockedUntil"" IS NULL OR ""LockedUntil"" <= (now() AT TIME ZONE 'utc'))
    ORDER BY ""CreatedAt""
    LIMIT @BatchSize
    FOR UPDATE SKIP LOCKED)
RETURNING
    ""FlowName"", ""RunId"", ""InstanceId"", ""Status"", ""CurrentStep"", ""ContextJson"", ""StepPathHash"", ""Attempts"",
    ""FailedStep"", ""ErrorMessage"", ""Version"", ""CreatedAt"", ""UpdatedAt"",
    ""LockedBy"", ""LockedUntil"", ""MetadataJson"";";

        return ExecuteAsync(context, sql, runnerId, leaseUntil, batchSize, cancellationToken);
    }

    private static async Task<IReadOnlyList<ExecutionRecord>> ClaimSqliteAsync(
        DurablyDbContext context,
        string runnerId,
        DateTimeOffset leaseUntil,
        int batchSize,
        CancellationToken cancellationToken)
    {
        // SQLite has no SKIP LOCKED; claim under a write transaction for exclusivity.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        const string table = "\"Executions\"";
        var sql =
$@"UPDATE {table}
SET LockedBy = @RunnerId,
    LockedUntil = @LockedUntil,
    UpdatedAt = @UpdatedAt
WHERE rowid IN (
    SELECT rowid
    FROM {table}
    WHERE (Status = @Pending OR Status = @Running)
      AND (LockedUntil IS NULL OR LockedUntil <= @Now)
    ORDER BY CreatedAt
    LIMIT @BatchSize
)
RETURNING FlowName, RunId, InstanceId, Status, CurrentStep, ContextJson, StepPathHash, Attempts, FailedStep, ErrorMessage,
          Version, CreatedAt, UpdatedAt, LockedBy, LockedUntil, MetadataJson;";

        var claimed = await ExecuteAsync(
                context,
                sql,
                runnerId,
                leaseUntil,
                batchSize,
                cancellationToken,
                includeClientNow: true)
            .ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return claimed;
    }

    private static async Task<IReadOnlyList<ExecutionRecord>> ExecuteAsync(
        DurablyDbContext context,
        string sql,
        string runnerId,
        DateTimeOffset leaseUntil,
        int batchSize,
        CancellationToken cancellationToken,
        bool includeClientNow = false)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = sql;
        if (context.Database.GetCommandTimeout() is int timeout)
        {
            command.CommandTimeout = timeout;
        }

        AddParameter(command, "@RunnerId", runnerId);
        AddParameter(command, "@LockedUntil", leaseUntil.UtcDateTime);
        AddParameter(command, "@BatchSize", batchSize);
        AddParameter(command, "@Pending", (int)ExecutionStatus.Pending);
        AddParameter(command, "@Running", (int)ExecutionStatus.Running);
        if (includeClientNow)
        {
            var now = DateTime.UtcNow;
            AddParameter(command, "@Now", now);
            AddParameter(command, "@UpdatedAt", now);
        }

        var claimed = new List<ExecutionRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            claimed.Add(ReadRecord(reader));
        }

        return claimed;
    }

    private static ExecutionRecord ReadRecord(DbDataReader reader)
    {
        return new ExecutionRecord
        {
            FlowName = reader.GetString(reader.GetOrdinal("FlowName")),
            RunId = reader.GetString(reader.GetOrdinal("RunId")),
            InstanceId = reader.GetString(reader.GetOrdinal("InstanceId")),
            Status = (ExecutionStatus)reader.GetInt32(reader.GetOrdinal("Status")),
            CurrentStep = reader.GetInt32(reader.GetOrdinal("CurrentStep")),
            ContextJson = reader.GetString(reader.GetOrdinal("ContextJson")),
            StepPathHash = reader.IsDBNull(reader.GetOrdinal("StepPathHash"))
                ? null
                : reader.GetString(reader.GetOrdinal("StepPathHash")),
            Attempts = reader.GetInt32(reader.GetOrdinal("Attempts")),
            FailedStep = reader.IsDBNull(reader.GetOrdinal("FailedStep"))
                ? null
                : reader.GetString(reader.GetOrdinal("FailedStep")),
            ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage"))
                ? null
                : reader.GetString(reader.GetOrdinal("ErrorMessage")),
            Version = reader.GetInt64(reader.GetOrdinal("Version")),
            CreatedAt = ToUtc(reader.GetDateTime(reader.GetOrdinal("CreatedAt"))),
            UpdatedAt = ToUtc(reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))),
            LockedBy = reader.IsDBNull(reader.GetOrdinal("LockedBy"))
                ? null
                : reader.GetString(reader.GetOrdinal("LockedBy")),
            LockedUntil = reader.IsDBNull(reader.GetOrdinal("LockedUntil"))
                ? null
                : ToUtc(reader.GetDateTime(reader.GetOrdinal("LockedUntil"))),
            MetadataJson = reader.IsDBNull(reader.GetOrdinal("MetadataJson"))
                ? null
                : reader.GetString(reader.GetOrdinal("MetadataJson"))
        };
    }

    private static DateTimeOffset ToUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string QuoteSqlServer(string identifier) =>
        "[" + identifier.Replace("]", "]]", StringComparison.Ordinal) + "]";

    private static string QuotePostgres(string identifier) =>
        "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
