namespace Durably;

internal sealed class ExecutionEntity
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
}
