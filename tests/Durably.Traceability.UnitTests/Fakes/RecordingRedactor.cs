namespace Durably.Traceability.UnitTests.Fakes;

internal sealed class RecordingRedactor : ITraceRedactor
{
    public int CallCount { get; private set; }

    public void Redact(TraceRecord record)
    {
        CallCount++;
        record.InputJson = "[redacted]";
        record.OutputJson = "[redacted]";
    }
}
