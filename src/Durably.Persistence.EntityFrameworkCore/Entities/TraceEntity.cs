namespace Durably;

internal sealed class TraceEntity
{
    public long Id { get; set; }

    public string FlowName { get; set; } = string.Empty;

    public string InstanceId { get; set; } = string.Empty;

    public string StepKey { get; set; } = string.Empty;

    public int Attempt { get; set; }

    public int Outcome { get; set; }

    public string? InputJson { get; set; }

    public string? OutputJson { get; set; }

    public int DurationMs { get; set; }

    public string? ExceptionMessage { get; set; }

    public DateTime Timestamp { get; set; }
}
