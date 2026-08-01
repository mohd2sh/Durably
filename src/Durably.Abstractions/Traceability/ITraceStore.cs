namespace Durably.Traceability;

/// <summary>
/// Persistence seam for trace records. Writes are performed by the background trace writer on a
/// dedicated connection, separate from checkpoint I/O.
/// </summary>
public interface ITraceStore
{
    /// <summary>Append a batch of trace records.</summary>
    Task AppendAsync(IReadOnlyList<TraceRecord> records, CancellationToken cancellationToken);

    /// <summary>Load all traces for one run (newest last).</summary>
    Task<IReadOnlyList<TraceRecord>> LoadAsync(string flowName, string runId, CancellationToken cancellationToken);
}
