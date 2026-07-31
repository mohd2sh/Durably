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
        string instanceId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(flowName))
        {
            throw new ArgumentException("Flow name is required.", nameof(flowName));
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance id is required.", nameof(instanceId));
        }

        return _store.LoadAsync(flowName, instanceId, cancellationToken);
    }
}
