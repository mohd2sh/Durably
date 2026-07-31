namespace Durably.Traceability;
/// <summary>
/// Receives trace events from the engine. Implementations must be fast and non-blocking; the default
/// no-op is used when traceability is not enabled.
/// </summary>
public interface ITraceSink
{
    /// <summary>Enqueue a trace event. Must not throw and must not block on I/O.</summary>
    void Emit(TraceRecord record);
}
