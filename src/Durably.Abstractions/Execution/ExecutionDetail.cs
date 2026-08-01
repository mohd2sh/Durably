namespace Durably.Execution;
/// <summary>Full execution state for detail views.</summary>
public sealed class ExecutionDetail
{
    public string FlowName { get; set; } = string.Empty;

    public string RunId { get; set; } = string.Empty;

    public string InstanceId { get; set; } = string.Empty;

    public ExecutionStatus Status { get; set; }

    public int CurrentStep { get; set; }

    public int Attempts { get; set; }

    public string? FailedStep { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? MetadataJson { get; set; }

    public string ContextJson { get; set; } = string.Empty;

    public long Version { get; set; }

    public string? LockedBy { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }
}
