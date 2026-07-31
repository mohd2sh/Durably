namespace Durably.Execution;
public sealed class ExecutionStatusInfo
{
    public string FlowName { get; set; } = string.Empty;

    public string InstanceId { get; set; } = string.Empty;

    public ExecutionStatus Status { get; set; }

    public int CurrentStep { get; set; }

    public string? FailedStep { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
