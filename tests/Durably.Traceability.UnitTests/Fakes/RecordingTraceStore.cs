namespace Durably.Traceability.UnitTests.Fakes;

internal sealed class RecordingTraceStore : ITraceStore
{
    private readonly object _gate = new();
    private readonly List<IReadOnlyList<TraceRecord>> _batches = new();
    private readonly List<TraceRecord> _all = new();
    private int _appendCalls;
    private int _failNextAppends;

    public IReadOnlyList<IReadOnlyList<TraceRecord>> Batches
    {
        get
        {
            lock (_gate)
            {
                return _batches.Select(b => (IReadOnlyList<TraceRecord>)b.ToList()).ToList();
            }
        }
    }

    public IReadOnlyList<TraceRecord> All
    {
        get
        {
            lock (_gate)
            {
                return _all.ToList();
            }
        }
    }

    public int AppendCallCount
    {
        get
        {
            lock (_gate)
            {
                return _appendCalls;
            }
        }
    }

    public void FailNextAppends(int count) => _failNextAppends = count;

    public Task AppendAsync(IReadOnlyList<TraceRecord> records, CancellationToken cancellationToken)
    {
        if (records is null || records.Count == 0)
        {
            return Task.CompletedTask;
        }

        lock (_gate)
        {
            _appendCalls++;
            if (_failNextAppends > 0)
            {
                _failNextAppends--;
                throw new InvalidOperationException("simulated trace store failure");
            }

            var copy = records.Select(Clone).ToList();
            _batches.Add(copy);
            _all.AddRange(copy);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TraceRecord>> LoadAsync(string flowName, string instanceId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var matches = _all
                .Where(r => r.FlowName == flowName && r.InstanceId == instanceId)
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
