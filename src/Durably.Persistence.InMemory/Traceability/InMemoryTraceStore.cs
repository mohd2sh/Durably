namespace Durably.Traceability;

/// <summary>In-memory <see cref="ITraceStore"/> for tests and local development.</summary>
internal sealed class InMemoryTraceStore : ITraceStore
{
    private readonly object _gate = new();
    private readonly List<TraceRecord> _records = new();

    public Task AppendAsync(IReadOnlyList<TraceRecord> records, CancellationToken cancellationToken)
    {
        if (records is null || records.Count == 0)
        {
            return Task.CompletedTask;
        }

        lock (_gate)
        {
            foreach (var record in records)
            {
                _records.Add(Clone(record));
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TraceRecord>> LoadAsync(string flowName, string instanceId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var matches = _records
                .Where(r => r.FlowName == flowName && r.InstanceId == instanceId)
                .OrderBy(r => r.Timestamp)
                .Select(Clone)
                .ToList();
            return Task.FromResult<IReadOnlyList<TraceRecord>>(matches);
        }
    }

    private static TraceRecord Clone(TraceRecord source) => new()
    {
        FlowName = source.FlowName,
        InstanceId = source.InstanceId,
        StepKey = source.StepKey,
        Attempt = source.Attempt,
        Outcome = source.Outcome,
        InputJson = source.InputJson,
        OutputJson = source.OutputJson,
        DurationMs = source.DurationMs,
        ExceptionMessage = source.ExceptionMessage,
        Timestamp = source.Timestamp
    };
}
