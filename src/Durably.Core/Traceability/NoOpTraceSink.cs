namespace Durably.Traceability;
/// <summary>Default trace sink: discards all events with zero overhead.</summary>
internal sealed class NoOpTraceSink : ITraceSink
{
    public static readonly NoOpTraceSink Instance = new();

    public void Emit(TraceRecord record)
    {
    }
}
