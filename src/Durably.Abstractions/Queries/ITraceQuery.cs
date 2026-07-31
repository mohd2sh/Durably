namespace Durably.Queries;
/// <summary>Read-only queries over persisted step traces.</summary>
public interface ITraceQuery
{
    Task<IReadOnlyList<TraceRecord>> GetTracesAsync(string flowName, string instanceId, CancellationToken cancellationToken);
}
