namespace Durably;

/// <summary>SQL Server dialect. Stores instances in the <c>durable.Executions</c> table.</summary>
public sealed class SqlServerDialect : ISqlDialect
{
    private static readonly string IdentifierLength =
        DurablyLimits.IdentifierMaxLength.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private const string SelectColumns =
        "FlowName, RunId, InstanceId, Status, CurrentStep, ContextJson, StepPathHash, Attempts, FailedStep, ErrorMessage, " +
        "Version, CreatedAt, UpdatedAt, LockedBy, LockedUntil, MetadataJson";

    public string EnsureSchemaSql =>
$@"IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'durable')
    EXEC('CREATE SCHEMA durable');
IF OBJECT_ID(N'durable.Executions', N'U') IS NULL
    CREATE TABLE durable.Executions (
        FlowName     NVARCHAR({IdentifierLength}) NOT NULL,
        RunId        NVARCHAR({IdentifierLength}) NOT NULL,
        InstanceId   NVARCHAR({IdentifierLength}) NOT NULL,
        Status       INT           NOT NULL,
        CurrentStep  INT           NOT NULL,
        ContextJson  NVARCHAR(MAX) NOT NULL,
        Attempts     INT           NOT NULL,
        FailedStep   NVARCHAR({IdentifierLength}) NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        Version      BIGINT        NOT NULL,
        CreatedAt    DATETIME2(7)  NOT NULL,
        UpdatedAt    DATETIME2(7)  NOT NULL,
        LockedBy     NVARCHAR({IdentifierLength}) NULL,
        LockedUntil  DATETIME2(7)  NULL,
        MetadataJson NVARCHAR(MAX) NULL,
        StepPathHash NVARCHAR(64) NULL,
        CONSTRAINT PK_durable_Executions PRIMARY KEY (FlowName, RunId)
    );
IF COL_LENGTH('durable.Executions', 'RunId') IS NULL
    ALTER TABLE durable.Executions ADD RunId NVARCHAR({IdentifierLength}) NULL;
IF COL_LENGTH('durable.Executions', 'MetadataJson') IS NULL
    ALTER TABLE durable.Executions ADD MetadataJson NVARCHAR(MAX) NULL;
IF COL_LENGTH('durable.Executions', 'StepPathHash') IS NULL
    ALTER TABLE durable.Executions ADD StepPathHash NVARCHAR(64) NULL;
IF OBJECT_ID(N'durable.Traces', N'U') IS NULL
    CREATE TABLE durable.Traces (
        Id               BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FlowName         NVARCHAR({IdentifierLength}) NOT NULL,
        RunId            NVARCHAR({IdentifierLength}) NOT NULL,
        InstanceId       NVARCHAR({IdentifierLength}) NOT NULL,
        StepKey          NVARCHAR({IdentifierLength}) NOT NULL,
        Attempt          INT           NOT NULL,
        Outcome          INT           NOT NULL,
        InputJson        NVARCHAR(MAX) NULL,
        OutputJson       NVARCHAR(MAX) NULL,
        DurationMs       INT           NOT NULL,
        ExceptionMessage NVARCHAR(MAX) NULL,
        Timestamp        DATETIME2(7)  NOT NULL
    );
IF COL_LENGTH('durable.Traces', 'RunId') IS NULL
    ALTER TABLE durable.Traces ADD RunId NVARCHAR({IdentifierLength}) NULL;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_durable_Traces_Flow_Instance' AND object_id = OBJECT_ID(N'durable.Traces'))
    DROP INDEX IX_durable_Traces_Flow_Instance ON durable.Traces;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_durable_Traces_Flow_Run' AND object_id = OBJECT_ID(N'durable.Traces'))
    CREATE INDEX IX_durable_Traces_Flow_Run ON durable.Traces (FlowName, RunId, Timestamp);
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_durable_Executions_Status_LockedUntil' AND object_id = OBJECT_ID(N'durable.Executions'))
    DROP INDEX IX_durable_Executions_Status_LockedUntil ON durable.Executions;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_durable_Executions_Status_LockedUntil_CreatedAt' AND object_id = OBJECT_ID(N'durable.Executions'))
    CREATE INDEX IX_durable_Executions_Status_LockedUntil_CreatedAt ON durable.Executions (Status, LockedUntil, CreatedAt);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_durable_Executions_Flow_Instance' AND object_id = OBJECT_ID(N'durable.Executions'))
    CREATE INDEX IX_durable_Executions_Flow_Instance ON durable.Executions (FlowName, InstanceId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_durable_Executions_Open_Flow_Instance' AND object_id = OBJECT_ID(N'durable.Executions'))
    CREATE UNIQUE INDEX IX_durable_Executions_Open_Flow_Instance ON durable.Executions (FlowName, InstanceId) WHERE Status IN (0, 3);";

    public string LoadSql =>
$@"SELECT {SelectColumns}
FROM durable.Executions
WHERE FlowName = @FlowName AND RunId = @RunId;";

    public string FindOpenSql =>
$@"SELECT {SelectColumns}
FROM durable.Executions
WHERE FlowName = @FlowName AND InstanceId = @InstanceId
  AND (Status = @Pending OR Status = @Running);";

    public string LoadLatestSql =>
$@"SELECT TOP (1) {SelectColumns}
FROM durable.Executions
WHERE FlowName = @FlowName AND InstanceId = @InstanceId
ORDER BY UpdatedAt DESC, CreatedAt DESC;";

    public string ListRunsSql =>
$@"SELECT {SelectColumns}
FROM durable.Executions
WHERE FlowName = @FlowName AND InstanceId = @InstanceId
ORDER BY UpdatedAt DESC, CreatedAt DESC;";

    public string InsertSql =>
@"INSERT INTO durable.Executions
    (FlowName, RunId, InstanceId, Status, CurrentStep, ContextJson, StepPathHash, Attempts, FailedStep, ErrorMessage, Version, CreatedAt, UpdatedAt, LockedBy, LockedUntil, MetadataJson)
VALUES
    (@FlowName, @RunId, @InstanceId, @Status, @CurrentStep, @ContextJson, @StepPathHash, @Attempts, @FailedStep, @ErrorMessage, @Version, @CreatedAt, @UpdatedAt, @LockedBy, @LockedUntil, @MetadataJson);";

    public string UpdateCheckpointSql =>
@"UPDATE durable.Executions SET
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
@"UPDATE durable.Executions SET
    LockedBy = @RunnerId,
    LockedUntil = @LockedUntil,
    UpdatedAt = @UpdatedAt
WHERE FlowName = @FlowName AND RunId = @RunId
  AND (LockedUntil IS NULL OR LockedUntil <= @Now OR LockedBy = @RunnerId);";

    public string ReleaseLeaseSql =>
@"UPDATE durable.Executions SET
    LockedBy = NULL,
    LockedUntil = NULL,
    UpdatedAt = @UpdatedAt
WHERE FlowName = @FlowName AND RunId = @RunId AND LockedBy = @RunnerId;";

    public string ClaimDueSql =>
@"WITH candidates AS (
    SELECT TOP (@BatchSize) *
    FROM durable.Executions WITH (READPAST, UPDLOCK, ROWLOCK)
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

    public string ListDueSql =>
@"SELECT TOP (@BatchSize) FlowName, RunId, InstanceId
FROM durable.Executions
WHERE (Status = @Pending OR Status = @Running)
  AND (LockedUntil IS NULL OR LockedUntil <= @Now)
ORDER BY CreatedAt;";

    public string AppendTraceSql =>
@"INSERT INTO durable.Traces
    (FlowName, RunId, InstanceId, StepKey, Attempt, Outcome, InputJson, OutputJson, DurationMs, ExceptionMessage, Timestamp)
VALUES
    (@FlowName, @RunId, @InstanceId, @StepKey, @Attempt, @Outcome, @InputJson, @OutputJson, @DurationMs, @ExceptionMessage, @Timestamp);";

    public string LoadTracesSql =>
@"SELECT FlowName, RunId, InstanceId, StepKey, Attempt, Outcome, InputJson, OutputJson, DurationMs, ExceptionMessage, Timestamp
FROM durable.Traces
WHERE FlowName = @FlowName AND RunId = @RunId
ORDER BY Timestamp;";

    public string ExecutionsTableName => "durable.Executions";

    public string SearchSelectColumns => SelectColumns;

    public string QuoteColumn(string columnName) => columnName;

    public string PagingClause => " OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

    public string? BuildMetadataEqualsPredicate(
        string metadataKey,
        string metadataValue,
        IDictionary<string, object> parameters)
    {
        parameters["MetadataKeyPath"] = "$." + metadataKey;
        parameters["MetadataValue"] = metadataValue;
        return $"JSON_VALUE({QuoteColumn("MetadataJson")}, @MetadataKeyPath) = @MetadataValue";
    }
}
