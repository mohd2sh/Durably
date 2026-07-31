namespace Durably;

/// <summary>PostgreSQL dialect. Stores instances in the <c>durable.executions</c> table (timestamps as UTC).</summary>
public sealed class PostgreSqlDialect : ISqlDialect
{
    private static readonly string IdentifierLength =
        DurablyLimits.IdentifierMaxLength.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public string EnsureSchemaSql =>
$@"CREATE SCHEMA IF NOT EXISTS durable;
CREATE TABLE IF NOT EXISTS durable.executions (
    flowname     varchar({IdentifierLength}) NOT NULL,
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
    CONSTRAINT pk_durable_executions PRIMARY KEY (flowname, instanceid)
);
ALTER TABLE durable.executions ADD COLUMN IF NOT EXISTS metadatajson text NULL;
ALTER TABLE durable.executions ADD COLUMN IF NOT EXISTS steppathhash varchar(64) NULL;
CREATE TABLE IF NOT EXISTS durable.traces (
    id               bigserial    PRIMARY KEY,
    flowname         varchar({IdentifierLength}) NOT NULL,
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
CREATE INDEX IF NOT EXISTS ix_durable_traces_flow_instance ON durable.traces (flowname, instanceid, timestamp);
DROP INDEX IF EXISTS durable.ix_durable_executions_status_lockeduntil;
CREATE INDEX IF NOT EXISTS ix_durable_executions_status_lockeduntil_createdat
    ON durable.executions (status, lockeduntil, createdat);";

    public string LoadSql =>
@"SELECT flowname, instanceid, status, currentstep, contextjson, steppathhash, attempts, failedstep, errormessage, version, createdat, updatedat, lockedby, lockeduntil, metadatajson
FROM durable.executions
WHERE flowname = @FlowName AND instanceid = @InstanceId;";

    public string InsertSql =>
@"INSERT INTO durable.executions
    (flowname, instanceid, status, currentstep, contextjson, steppathhash, attempts, failedstep, errormessage, version, createdat, updatedat, lockedby, lockeduntil, metadatajson)
VALUES
    (@FlowName, @InstanceId, @Status, @CurrentStep, @ContextJson, @StepPathHash, @Attempts, @FailedStep, @ErrorMessage, @Version, @CreatedAt, @UpdatedAt, @LockedBy, @LockedUntil, @MetadataJson);";

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
WHERE flowname = @FlowName AND instanceid = @InstanceId AND version = @Version AND lockedby = @RunnerId;";

    public string AcquireLeaseSql =>
@"UPDATE durable.executions SET
    lockedby = @RunnerId,
    lockeduntil = @LockedUntil,
    updatedat = @UpdatedAt
WHERE flowname = @FlowName AND instanceid = @InstanceId
  AND (lockeduntil IS NULL OR lockeduntil <= @Now OR lockedby = @RunnerId);";

    public string ReleaseLeaseSql =>
@"UPDATE durable.executions SET
    lockedby = NULL,
    lockeduntil = NULL,
    updatedat = @UpdatedAt
WHERE flowname = @FlowName AND instanceid = @InstanceId AND lockedby = @RunnerId;";

    public string ClaimDueSql =>
@"UPDATE durable.executions AS e
SET lockedby = @RunnerId,
    lockeduntil = @LockedUntil,
    updatedat = (now() AT TIME ZONE 'utc')
WHERE (flowname, instanceid) IN (
    SELECT flowname, instanceid
    FROM durable.executions
    WHERE (status = @Pending OR status = @Running)
      AND (lockeduntil IS NULL OR lockeduntil <= (now() AT TIME ZONE 'utc'))
    ORDER BY createdat
    LIMIT @BatchSize
    FOR UPDATE SKIP LOCKED)
RETURNING flowname AS FlowName, instanceid AS InstanceId, status AS Status, currentstep AS CurrentStep,
    contextjson AS ContextJson, steppathhash AS StepPathHash, attempts AS Attempts, failedstep AS FailedStep, errormessage AS ErrorMessage,
    version AS Version, createdat AS CreatedAt, updatedat AS UpdatedAt, lockedby AS LockedBy,
    lockeduntil AS LockedUntil, metadatajson AS MetadataJson;";

    public string ListDueSql =>
@"SELECT flowname, instanceid
FROM durable.executions
WHERE (status = @Pending OR status = @Running)
  AND (lockeduntil IS NULL OR lockeduntil <= @Now)
ORDER BY createdat
LIMIT @BatchSize;";

    public string AppendTraceSql =>
@"INSERT INTO durable.traces
    (flowname, instanceid, stepkey, attempt, outcome, inputjson, outputjson, durationms, exceptionmessage, timestamp)
VALUES
    (@FlowName, @InstanceId, @StepKey, @Attempt, @Outcome, @InputJson, @OutputJson, @DurationMs, @ExceptionMessage, @Timestamp);";

    public string LoadTracesSql =>
@"SELECT flowname, instanceid, stepkey, attempt, outcome, inputjson, outputjson, durationms, exceptionmessage, timestamp
FROM durable.traces
WHERE flowname = @FlowName AND instanceid = @InstanceId
ORDER BY timestamp;";

    public string ExecutionsTableName => "durable.executions";

    public string SearchSelectColumns =>
        "flowname AS FlowName, instanceid AS InstanceId, status AS Status, currentstep AS CurrentStep, " +
        "contextjson AS ContextJson, steppathhash AS StepPathHash, attempts AS Attempts, failedstep AS FailedStep, errormessage AS ErrorMessage, " +
        "version AS Version, createdat AS CreatedAt, updatedat AS UpdatedAt, lockedby AS LockedBy, " +
        "lockeduntil AS LockedUntil, metadatajson AS MetadataJson";

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
