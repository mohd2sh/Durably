namespace Durably;

/// <summary>
/// Provider-specific SQL for the execution store. All statements bind the same parameter names
/// and project columns whose names match <see cref="ExecutionRecord"/> (case-insensitive).
/// </summary>
public interface ISqlDialect
{
    string EnsureSchemaSql { get; }

    string LoadSql { get; }

    string InsertSql { get; }

    string UpdateCheckpointSql { get; }

    string AcquireLeaseSql { get; }

    string ReleaseLeaseSql { get; }

    /// <summary>Atomic claim-N: lease up to @BatchSize due rows and return full records.</summary>
    string ClaimDueSql { get; }

    string ListDueSql { get; }

    string AppendTraceSql { get; }

    string LoadTracesSql { get; }

    /// <summary>Qualified executions table name used by search queries.</summary>
    string ExecutionsTableName { get; }

    /// <summary>SELECT column list for search result rows.</summary>
    string SearchSelectColumns { get; }

    /// <summary>Maps a logical column name to the dialect-specific identifier.</summary>
    string QuoteColumn(string columnName);

    /// <summary>Appends paging (OFFSET/FETCH or LIMIT/OFFSET) using @Skip and @Take parameters.</summary>
    string PagingClause { get; }

    /// <summary>
    /// Returns a SQL predicate for metadata key/value filtering, or <c>null</c> when unsupported.
    /// Adds any required parameters to <paramref name="parameters"/>.
    /// </summary>
    string? BuildMetadataEqualsPredicate(
        string metadataKey,
        string metadataValue,
        IDictionary<string, object> parameters);
}
