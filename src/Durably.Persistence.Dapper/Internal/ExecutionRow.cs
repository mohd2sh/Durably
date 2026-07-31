namespace Durably;

/// <summary>Row shape used for Dapper mapping; isolates provider-friendly types from <see cref="ExecutionRecord"/>.</summary>
internal sealed class ExecutionRow
{
    public string FlowName { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public int Status { get; set; }
    public int CurrentStep { get; set; }
    public string ContextJson { get; set; } = string.Empty;
    public string? StepPathHash { get; set; }
    public int Attempts { get; set; }
    public string? FailedStep { get; set; }
    public string? ErrorMessage { get; set; }
    public long Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? LockedBy { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string? MetadataJson { get; set; }

    public static ExecutionRow From(ExecutionRecord record) => new()
    {
        FlowName = record.FlowName,
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

    public ExecutionRecord ToRecord() => new()
    {
        FlowName = FlowName,
        InstanceId = InstanceId,
        Status = (ExecutionStatus)Status,
        CurrentStep = CurrentStep,
        ContextJson = ContextJson,
        StepPathHash = StepPathHash,
        Attempts = Attempts,
        FailedStep = FailedStep,
        ErrorMessage = ErrorMessage,
        Version = Version,
        CreatedAt = new DateTimeOffset(DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc)),
        UpdatedAt = new DateTimeOffset(DateTime.SpecifyKind(UpdatedAt, DateTimeKind.Utc)),
        LockedBy = LockedBy,
        LockedUntil = LockedUntil is null
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(LockedUntil.Value, DateTimeKind.Utc)),
        MetadataJson = MetadataJson
    };
}
