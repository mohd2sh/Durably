namespace Durably.Queries;

/// <summary>Read-only trace queries backed by the configured <see cref="ITraceStore"/>.</summary>
internal sealed class TraceStoreQuery : ITraceQuery
{
    private readonly ITraceStore _store;

    public TraceStoreQuery(ITraceStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public Task<IReadOnlyList<TraceRecord>> GetTracesAsync(
        string flowName,
        string runId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(flowName))
        {
            throw new ArgumentException("Flow name is required.", nameof(flowName));
        }

        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("Run id is required.", nameof(runId));
        }

        return _store.LoadAsync(flowName, runId, cancellationToken);
    }
}
