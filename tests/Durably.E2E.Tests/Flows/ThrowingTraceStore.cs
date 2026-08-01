namespace Durably.E2E.Tests.Flows;

internal sealed class ThrowingTraceStore : ITraceStore
{
    public Task AppendAsync(IReadOnlyList<TraceRecord> records, CancellationToken cancellationToken)
        => throw new InvalidOperationException("trace db down");

    public Task<IReadOnlyList<TraceRecord>> LoadAsync(string flowName, string runId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<TraceRecord>>(Array.Empty<TraceRecord>());
}
