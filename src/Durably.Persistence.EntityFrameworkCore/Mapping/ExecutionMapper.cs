namespace Durably;

internal static class ExecutionMapper
{
    public static ExecutionEntity ToEntity(ExecutionRecord record) => new()
    {
        FlowName = record.FlowName,
        RunId = record.RunId,
        InstanceId = record.InstanceId,
        Status = (int)record.Status,
        CurrentStep = record.CurrentStep,
        ContextJson = record.ContextJson,
        StepPathHash = record.StepPathHash,
        Attempts = record.Attempts,
        FailedStep = record.FailedStep,
        ErrorMessage = record.ErrorMessage,
        Version = record.Version,
        CreatedAt = record.CreatedAt.UtcDateTime,
        UpdatedAt = record.UpdatedAt.UtcDateTime,
        LockedBy = record.LockedBy,
        LockedUntil = record.LockedUntil?.UtcDateTime,
        MetadataJson = record.MetadataJson
    };

    public static ExecutionRecord ToRecord(ExecutionEntity entity) => new()
    {
        FlowName = entity.FlowName,
        RunId = entity.RunId,
        InstanceId = entity.InstanceId,
        Status = (ExecutionStatus)entity.Status,
        CurrentStep = entity.CurrentStep,
        ContextJson = entity.ContextJson,
        StepPathHash = entity.StepPathHash,
        Attempts = entity.Attempts,
        FailedStep = entity.FailedStep,
        ErrorMessage = entity.ErrorMessage,
        Version = entity.Version,
        CreatedAt = ToUtcOffset(entity.CreatedAt),
        UpdatedAt = ToUtcOffset(entity.UpdatedAt),
        LockedBy = entity.LockedBy,
        LockedUntil = entity.LockedUntil is null ? null : ToUtcOffset(entity.LockedUntil.Value),
        MetadataJson = entity.MetadataJson
    };

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
