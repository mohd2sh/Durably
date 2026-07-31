namespace Durably.Traceability;
/// <summary>Redacts sensitive fields from trace records before they are enqueued for persistence.</summary>
public interface ITraceRedactor
{
    void Redact(TraceRecord record);
}
