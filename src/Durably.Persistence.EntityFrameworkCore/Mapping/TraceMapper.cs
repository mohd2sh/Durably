namespace Durably;

internal static class TraceMapper
{
    public static TraceEntity ToEntity(TraceRecord record) => new()
    {
        FlowName = record.FlowName,
        InstanceId = record.InstanceId,
        StepKey = record.StepKey,
        Attempt = record.Attempt,
        Outcome = (int)record.Outcome,
        InputJson = record.InputJson,
        OutputJson = record.OutputJson,
        DurationMs = record.DurationMs,
        ExceptionMessage = record.ExceptionMessage,
        Timestamp = record.Timestamp.UtcDateTime
    };

    public static TraceRecord ToRecord(TraceEntity entity) => new()
    {
        FlowName = entity.FlowName,
        InstanceId = entity.InstanceId,
        StepKey = entity.StepKey,
        Attempt = entity.Attempt,
        Outcome = (TraceOutcome)entity.Outcome,
        InputJson = entity.InputJson,
        OutputJson = entity.OutputJson,
        DurationMs = entity.DurationMs,
        ExceptionMessage = entity.ExceptionMessage,
        Timestamp = new DateTimeOffset(DateTime.SpecifyKind(entity.Timestamp, DateTimeKind.Utc))
    };
}
