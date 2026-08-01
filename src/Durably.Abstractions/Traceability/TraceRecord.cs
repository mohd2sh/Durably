namespace Durably.Traceability;
/// <summary>
/// A single per-step trace event emitted by the engine and persisted asynchronously by
/// <see cref="ITraceStore"/>. Best-effort: loss on crash is acceptable; must never block checkpoints.
/// </summary>
public sealed class TraceRecord
{
    public string FlowName { get; set; } = string.Empty;

    public string RunId { get; set; } = string.Empty;

    public string InstanceId { get; set; } = string.Empty;

    public string StepKey { get; set; } = string.Empty;

    /// <summary>1-based attempt number for this step.</summary>
    public int Attempt { get; set; }

    public TraceOutcome Outcome { get; set; }

    /// <summary>Serialized context snapshot before the step ran.</summary>
    public string? InputJson { get; set; }

    /// <summary>Serialized context snapshot after a successful step.</summary>
    public string? OutputJson { get; set; }

    public int DurationMs { get; set; }

    public string? ExceptionMessage { get; set; }

    public DateTimeOffset Timestamp { get; set; }
}
