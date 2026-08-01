namespace Durably;

/// <summary>SQLite dialect. Single-file/embedded durable execution; also used for fast tests.</summary>
public sealed class SqliteDialect : ISqlDialect
{
    private const string SelectColumns =
        "FlowName, RunId, InstanceId, Status, CurrentStep, ContextJson, StepPathHash, Attempts, FailedStep, ErrorMessage, " +
        "Version, CreatedAt, UpdatedAt, LockedBy, LockedUntil, MetadataJson";

    public string EnsureSchemaSql =>
@"CREATE TABLE IF NOT EXISTS Executions (
    FlowName     TEXT    NOT NULL,
    RunId        TEXT    NOT NULL,
    InstanceId   TEXT    NOT NULL,
    Status       INTEGER NOT NULL,
    CurrentStep  INTEGER NOT NULL,
    ContextJson  TEXT    NOT NULL,
    Attempts     INTEGER NOT NULL,
    FailedStep   TEXT    NULL,
    ErrorMessage TEXT    NULL,
    Version      INTEGER NOT NULL,
    CreatedAt    TEXT    NOT NULL,
    UpdatedAt    TEXT    NOT NULL,
    LockedBy     TEXT    NULL,
    LockedUntil  TEXT    NULL,
    MetadataJson TEXT    NULL,
    StepPathHash TEXT    NULL,
    PRIMARY KEY (FlowName, RunId)
);
CREATE TABLE IF NOT EXISTS Traces (
    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
    FlowName         TEXT    NOT NULL,
    RunId            TEXT    NOT NULL,
    InstanceId       TEXT    NOT NULL,
    StepKey          TEXT    NOT NULL,
    Attempt          INTEGER NOT NULL,
    Outcome          INTEGER NOT NULL,
    InputJson        TEXT    NULL,
    OutputJson       TEXT    NULL,
    DurationMs       INTEGER NOT NULL,
    ExceptionMessage TEXT    NULL,
    Timestamp        TEXT    NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_Traces_Flow_Run ON Traces (FlowName, RunId, Timestamp);
CREATE INDEX IF NOT EXISTS IX_Executions_Status_LockedUntil_CreatedAt ON Executions (Status, LockedUntil, CreatedAt);
CREATE INDEX IF NOT EXISTS IX_Executions_Flow_Instance ON Executions (FlowName, InstanceId);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Executions_Open_Flow_Instance ON Executions (FlowName, InstanceId) WHERE Status IN (0, 3);";

    public string LoadSql =>
$@"SELECT {SelectColumns}
FROM Executions
WHERE FlowName = @FlowName AND RunId = @RunId;";

    public string FindOpenSql =>
$@"SELECT {SelectColumns}
FROM Executions
WHERE FlowName = @FlowName AND InstanceId = @InstanceId
  AND (Status = @Pending OR Status = @Running);";

    public string LoadLatestSql =>
$@"SELECT {SelectColumns}
FROM Executions
WHERE FlowName = @FlowName AND InstanceId = @InstanceId
ORDER BY UpdatedAt DESC, CreatedAt DESC
LIMIT 1;";

    public string ListRunsSql =>
$@"SELECT {SelectColumns}
FROM Executions
WHERE FlowName = @FlowName AND InstanceId = @InstanceId
ORDER BY UpdatedAt DESC, CreatedAt DESC;";

    public string InsertSql =>
@"INSERT INTO Executions
    (FlowName, RunId, InstanceId, Status, CurrentStep, ContextJson, StepPathHash, Attempts, FailedStep, ErrorMessage, Version, CreatedAt, UpdatedAt, LockedBy, LockedUntil, MetadataJson)
VALUES
    (@FlowName, @RunId, @InstanceId, @Status, @CurrentStep, @ContextJson, @StepPathHash, @Attempts, @FailedStep, @ErrorMessage, @Version, @CreatedAt, @UpdatedAt, @LockedBy, @LockedUntil, @MetadataJson);";

    public string UpdateCheckpointSql =>
@"UPDATE Executions SET
    Status = @Status,
    CurrentStep = @CurrentStep,
    ContextJson = @ContextJson,
    StepPathHash = @StepPathHash,
    Attempts = @Attempts,
    FailedStep = @FailedStep,
    ErrorMessage = @ErrorMessage,
    LockedUntil = @LockedUntil,
    Version = Version + 1,
    UpdatedAt = @UpdatedAt
WHERE FlowName = @FlowName AND RunId = @RunId AND Version = @Version AND LockedBy = @RunnerId;";

    public string AcquireLeaseSql =>
@"UPDATE Executions SET
    LockedBy = @RunnerId,
    LockedUntil = @LockedUntil,
    UpdatedAt = @UpdatedAt
WHERE FlowName = @FlowName AND RunId = @RunId
  AND (LockedUntil IS NULL OR LockedUntil <= @Now OR LockedBy = @RunnerId);";

    public string ReleaseLeaseSql =>
@"UPDATE Executions SET
    LockedBy = NULL,
    LockedUntil = NULL,
    UpdatedAt = @UpdatedAt
WHERE FlowName = @FlowName AND RunId = @RunId AND LockedBy = @RunnerId;";

    public string ClaimDueSql =>
@"UPDATE Executions
SET LockedBy = @RunnerId,
    LockedUntil = @LockedUntil,
    UpdatedAt = @UpdatedAt
WHERE rowid IN (
    SELECT rowid
    FROM Executions
    WHERE (Status = @Pending OR Status = @Running)
      AND (LockedUntil IS NULL OR LockedUntil <= @Now)
    ORDER BY CreatedAt
    LIMIT @BatchSize
)
RETURNING FlowName, RunId, InstanceId, Status, CurrentStep, ContextJson, StepPathHash, Attempts, FailedStep, ErrorMessage,
          Version, CreatedAt, UpdatedAt, LockedBy, LockedUntil, MetadataJson;";

    public string ListDueSql =>
@"SELECT FlowName, RunId, InstanceId
FROM Executions
WHERE (Status = @Pending OR Status = @Running)
  AND (LockedUntil IS NULL OR LockedUntil <= @Now)
ORDER BY CreatedAt
LIMIT @BatchSize;";

    public string AppendTraceSql =>
@"INSERT INTO Traces
    (FlowName, RunId, InstanceId, StepKey, Attempt, Outcome, InputJson, OutputJson, DurationMs, ExceptionMessage, Timestamp)
VALUES
    (@FlowName, @RunId, @InstanceId, @StepKey, @Attempt, @Outcome, @InputJson, @OutputJson, @DurationMs, @ExceptionMessage, @Timestamp);";

    public string LoadTracesSql =>
@"SELECT FlowName, RunId, InstanceId, StepKey, Attempt, Outcome, InputJson, OutputJson, DurationMs, ExceptionMessage, Timestamp
FROM Traces
WHERE FlowName = @FlowName AND RunId = @RunId
ORDER BY Timestamp;";

    public string ExecutionsTableName => "Executions";

    public string SearchSelectColumns => SelectColumns;

    public string QuoteColumn(string columnName) => columnName;

    public string PagingClause => " LIMIT @Take OFFSET @Skip";

    public string? BuildMetadataEqualsPredicate(
        string metadataKey,
        string metadataValue,
        IDictionary<string, object> parameters)
    {
        parameters["MetadataKeyPath"] = "$." + metadataKey;
        parameters["MetadataValue"] = metadataValue;
        return $"json_extract({QuoteColumn("MetadataJson")}, @MetadataKeyPath) = @MetadataValue";
    }
}
