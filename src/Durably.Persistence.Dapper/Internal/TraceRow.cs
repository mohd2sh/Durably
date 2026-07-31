namespace Durably;

internal sealed class TraceRow
{
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

    public static TraceRow From(TraceRecord record) => new()
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

    public TraceRecord ToRecord() => new()
    {
        FlowName = FlowName,
        InstanceId = InstanceId,
        StepKey = StepKey,
        Attempt = Attempt,
        Outcome = (TraceOutcome)Outcome,
        InputJson = InputJson,
        OutputJson = OutputJson,
        DurationMs = DurationMs,
        ExceptionMessage = ExceptionMessage,
        Timestamp = new DateTimeOffset(DateTime.SpecifyKind(Timestamp, DateTimeKind.Utc))
    };
}
