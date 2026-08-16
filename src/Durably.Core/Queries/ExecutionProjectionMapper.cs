namespace Durably.Queries;

internal static class ExecutionProjectionMapper
{
    public static ExecutionSummary ToSummary(ExecutionRecord record) => new()
    {
        FlowName = record.FlowName,
        RunId = record.RunId,
        InstanceId = record.InstanceId,
        Status = record.Status,
        CurrentStep = record.CurrentStep,
        Attempts = record.Attempts,
        FailedStep = record.FailedStep,
        ErrorMessage = record.ErrorMessage,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt,
        MetadataJson = record.MetadataJson
    };

    public static ExecutionDetail ToDetail(ExecutionRecord record) => new()
    {
        FlowName = record.FlowName,
        RunId = record.RunId,
        InstanceId = record.InstanceId,
        Status = record.Status,
        CurrentStep = record.CurrentStep,
        Attempts = record.Attempts,
        FailedStep = record.FailedStep,
        ErrorMessage = record.ErrorMessage,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt,
        MetadataJson = record.MetadataJson,
        ContextJson = record.ContextJson,
        Version = record.Version,
        LockedBy = record.LockedBy,
        LockedUntil = record.LockedUntil
    };
}
