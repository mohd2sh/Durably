namespace Durably;

/// <summary>PostgreSQL dialect. Stores instances in the <c>durable.executions</c> table (timestamps as UTC).</summary>
public sealed class PostgreSqlDialect : ISqlDialect
{
    private static readonly string IdentifierLength =
        DurablyLimits.IdentifierMaxLength.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private const string SelectColumns =
        "flowname AS FlowName, runid AS RunId, instanceid AS InstanceId, status AS Status, currentstep AS CurrentStep, " +
        "contextjson AS ContextJson, steppathhash AS StepPathHash, attempts AS Attempts, failedstep AS FailedStep, errormessage AS ErrorMessage, " +
        "version AS Version, createdat AS CreatedAt, updatedat AS UpdatedAt, lockedby AS LockedBy, " +
        "lockeduntil AS LockedUntil, metadatajson AS MetadataJson";

    public string EnsureSchemaSql =>
$@"CREATE SCHEMA IF NOT EXISTS durable;
CREATE TABLE IF NOT EXISTS durable.executions (
    flowname     varchar({IdentifierLength}) NOT NULL,
    runid        varchar({IdentifierLength}) NOT NULL,
    instanceid   varchar({IdentifierLength}) NOT NULL,
    status       int          NOT NULL,
    currentstep  int          NOT NULL,
    contextjson  text         NOT NULL,
    attempts     int          NOT NULL,
    failedstep   varchar({IdentifierLength}) NULL,
    errormessage text         NULL,
    version      bigint       NOT NULL,
    createdat    timestamp    NOT NULL,
    updatedat    timestamp    NOT NULL,
    lockedby     varchar({IdentifierLength}) NULL,
    lockeduntil  timestamp    NULL,
    metadatajson text         NULL,
    steppathhash varchar(64)  NULL,
    CONSTRAINT pk_durable_executions PRIMARY KEY (flowname, runid)
);
ALTER TABLE durable.executions ADD COLUMN IF NOT EXISTS runid varchar({IdentifierLength}) NULL;
ALTER TABLE durable.executions ADD COLUMN IF NOT EXISTS metadatajson text NULL;
ALTER TABLE durable.executions ADD COLUMN IF NOT EXISTS steppathhash varchar(64) NULL;
CREATE TABLE IF NOT EXISTS durable.traces (
    id               bigserial    PRIMARY KEY,
    flowname         varchar({IdentifierLength}) NOT NULL,
    runid            varchar({IdentifierLength}) NOT NULL,
    instanceid       varchar({IdentifierLength}) NOT NULL,
    stepkey          varchar({IdentifierLength}) NOT NULL,
    attempt          int          NOT NULL,
    outcome          int          NOT NULL,
    inputjson        text         NULL,
    outputjson       text         NULL,
    durationms       int          NOT NULL,
    exceptionmessage text         NULL,
    timestamp        timestamp    NOT NULL
);
ALTER TABLE durable.traces ADD COLUMN IF NOT EXISTS runid varchar({IdentifierLength}) NULL;
DROP INDEX IF EXISTS durable.ix_durable_traces_flow_instance;
CREATE INDEX IF NOT EXISTS ix_durable_traces_flow_run ON durable.traces (flowname, runid, timestamp);
DROP INDEX IF EXISTS durable.ix_durable_executions_status_lockeduntil;
CREATE INDEX IF NOT EXISTS ix_durable_executions_status_lockeduntil_createdat
    ON durable.executions (status, lockeduntil, createdat);
CREATE INDEX IF NOT EXISTS ix_durable_executions_flow_instance
    ON durable.executions (flowname, instanceid);
CREATE UNIQUE INDEX IF NOT EXISTS ix_durable_executions_open_flow_instance
    ON durable.executions (flowname, instanceid)
    WHERE status IN (0, 3);";

    public string LoadSql =>
$@"SELECT {SelectColumns}
FROM durable.executions
WHERE flowname = @FlowName AND runid = @RunId;";

    public string FindOpenSql =>
$@"SELECT {SelectColumns}
FROM durable.executions
WHERE flowname = @FlowName AND instanceid = @InstanceId
  AND (status = @Pending OR status = @Running);";

    public string LoadLatestSql =>
$@"SELECT {SelectColumns}
FROM durable.executions
WHERE flowname = @FlowName AND instanceid = @InstanceId
ORDER BY updatedat DESC, createdat DESC
LIMIT 1;";

    public string ListRunsSql =>
$@"SELECT {SelectColumns}
FROM durable.executions
WHERE flowname = @FlowName AND instanceid = @InstanceId
ORDER BY updatedat DESC, createdat DESC;";

    public string InsertSql =>
@"INSERT INTO durable.executions
    (flowname, runid, instanceid, status, currentstep, contextjson, steppathhash, attempts, failedstep, errormessage, version, createdat, updatedat, lockedby, lockeduntil, metadatajson)
VALUES
    (@FlowName, @RunId, @InstanceId, @Status, @CurrentStep, @ContextJson, @StepPathHash, @Attempts, @FailedStep, @ErrorMessage, @Version, @CreatedAt, @UpdatedAt, @LockedBy, @LockedUntil, @MetadataJson);";

    public string UpdateCheckpointSql =>
@"UPDATE durable.executions SET
    status = @Status,
    currentstep = @CurrentStep,
    contextjson = @ContextJson,
    steppathhash = @StepPathHash,
    attempts = @Attempts,
    failedstep = @FailedStep,
    errormessage = @ErrorMessage,
    lockeduntil = @LockedUntil,
    version = version + 1,
    updatedat = @UpdatedAt
WHERE flowname = @FlowName AND runid = @RunId AND version = @Version AND lockedby = @RunnerId;";

    public string AcquireLeaseSql =>
@"UPDATE durable.executions SET
    lockedby = @RunnerId,
    lockeduntil = @LockedUntil,
    updatedat = @UpdatedAt
WHERE flowname = @FlowName AND runid = @RunId
  AND (lockeduntil IS NULL OR lockeduntil <= @Now OR lockedby = @RunnerId);";

    public string ReleaseLeaseSql =>
@"UPDATE durable.executions SET
    lockedby = NULL,
    lockeduntil = NULL,
    updatedat = @UpdatedAt
WHERE flowname = @FlowName AND runid = @RunId AND lockedby = @RunnerId;";

    public string ClaimDueSql =>
@"UPDATE durable.executions AS e
SET lockedby = @RunnerId,
    lockeduntil = @LockedUntil,
    updatedat = (now() AT TIME ZONE 'utc')
WHERE (flowname, runid) IN (
    SELECT flowname, runid
    FROM durable.executions
    WHERE (status = @Pending OR status = @Running)
      AND (lockeduntil IS NULL OR lockeduntil <= (now() AT TIME ZONE 'utc'))
    ORDER BY createdat
    LIMIT @BatchSize
    FOR UPDATE SKIP LOCKED)
RETURNING flowname AS FlowName, runid AS RunId, instanceid AS InstanceId, status AS Status, currentstep AS CurrentStep,
    contextjson AS ContextJson, steppathhash AS StepPathHash, attempts AS Attempts, failedstep AS FailedStep, errormessage AS ErrorMessage,
    version AS Version, createdat AS CreatedAt, updatedat AS UpdatedAt, lockedby AS LockedBy,
    lockeduntil AS LockedUntil, metadatajson AS MetadataJson;";

    public string ListDueSql =>
@"SELECT flowname, runid, instanceid
FROM durable.executions
WHERE (status = @Pending OR status = @Running)
  AND (lockeduntil IS NULL OR lockeduntil <= @Now)
ORDER BY createdat
LIMIT @BatchSize;";

    public string AppendTraceSql =>
@"INSERT INTO durable.traces
    (flowname, runid, instanceid, stepkey, attempt, outcome, inputjson, outputjson, durationms, exceptionmessage, timestamp)
VALUES
    (@FlowName, @RunId, @InstanceId, @StepKey, @Attempt, @Outcome, @InputJson, @OutputJson, @DurationMs, @ExceptionMessage, @Timestamp);";

    public string LoadTracesSql =>
@"SELECT flowname AS FlowName, runid AS RunId, instanceid AS InstanceId, stepkey AS StepKey, attempt AS Attempt,
    outcome AS Outcome, inputjson AS InputJson, outputjson AS OutputJson, durationms AS DurationMs,
    exceptionmessage AS ExceptionMessage, timestamp AS Timestamp
FROM durable.traces
WHERE flowname = @FlowName AND runid = @RunId
ORDER BY timestamp;";

    public string ExecutionsTableName => "durable.executions";

    public string SearchSelectColumns => SelectColumns;

    public string QuoteColumn(string columnName) => columnName.ToLowerInvariant();

    public string PagingClause => " OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

    public string? BuildMetadataEqualsPredicate(
        string metadataKey,
        string metadataValue,
        IDictionary<string, object> parameters)
    {
        parameters["MetadataKey"] = metadataKey;
        parameters["MetadataValue"] = metadataValue;
        return $"{QuoteColumn("MetadataJson")}::jsonb ->> @MetadataKey = @MetadataValue";
    }
}
