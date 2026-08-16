using System.Collections.Concurrent;

namespace Durably.Execution;

/// <summary>
/// An in-process <see cref="IExecutionStore"/> for testing and prototyping. Enforces optimistic
/// concurrency and distributed-style execution leases in memory.
/// </summary>
public sealed class InMemoryExecutionStore : IExecutionStore
{
    private readonly ConcurrentDictionary<string, ExecutionRecord> _records = new();
    private readonly object _claimGate = new();

    public Task<ExecutionRecord?> LoadAsync(string flowName, string runId, CancellationToken cancellationToken)
    {
        var found = _records.TryGetValue(Key(flowName, runId), out var record);
        return Task.FromResult(found ? Clone(record!) : null);
    }

    public Task<ExecutionRecord?> FindOpenAsync(string flowName, string instanceId, CancellationToken cancellationToken)
    {
        var open = _records.Values.FirstOrDefault(r =>
            string.Equals(r.FlowName, flowName, StringComparison.Ordinal)
            && string.Equals(r.InstanceId, instanceId, StringComparison.Ordinal)
            && r.Status.IsOpen());
        return Task.FromResult(open is null ? null : Clone(open));
    }

    public Task<ExecutionRecord?> LoadLatestAsync(string flowName, string instanceId, CancellationToken cancellationToken)
    {
        var latest = _records.Values
            .Where(r =>
                string.Equals(r.FlowName, flowName, StringComparison.Ordinal)
                && string.Equals(r.InstanceId, instanceId, StringComparison.Ordinal))
            .OrderByDescending(r => r.UpdatedAt)
            .ThenByDescending(r => r.CreatedAt)
            .FirstOrDefault();
        return Task.FromResult(latest is null ? null : Clone(latest));
    }

    public Task CreateAsync(ExecutionRecord record, CancellationToken cancellationToken)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        if (string.IsNullOrWhiteSpace(record.RunId))
        {
            throw new ArgumentException("Run id is required.", nameof(record));
        }

        lock (_claimGate)
        {
            if (record.Status.IsOpen()
                && _records.Values.Any(r =>
                    string.Equals(r.FlowName, record.FlowName, StringComparison.Ordinal)
                    && string.Equals(r.InstanceId, record.InstanceId, StringComparison.Ordinal)
                    && r.Status.IsOpen()))
            {
                throw new ExecutionAlreadyExistsException(record.FlowName, record.InstanceId, record.RunId);
            }

            if (!_records.TryAdd(Key(record.FlowName, record.RunId), Clone(record)))
            {
                throw new ExecutionAlreadyExistsException(record.FlowName, record.InstanceId, record.RunId);
            }
        }

        return Task.CompletedTask;
    }

    public Task SaveCheckpointAsync(
        ExecutionRecord record,
        string runnerId,
        DateTimeOffset leaseUntil,
        CancellationToken cancellationToken)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        if (string.IsNullOrWhiteSpace(runnerId))
        {
            throw new ArgumentException("Runner id is required.", nameof(runnerId));
        }

        var key = Key(record.FlowName, record.RunId);
        if (!_records.TryGetValue(key, out var current))
        {
            throw new InvalidOperationException(
                $"Flow '{record.FlowName}' run '{record.RunId}' does not exist.");
        }

        if (!string.Equals(current.LockedBy, runnerId, StringComparison.Ordinal))
        {
            throw new LeaseLostException(record.FlowName, record.RunId, record.InstanceId);
        }

        if (current.Version != record.Version)
        {
            throw new ConcurrencyConflictException(record.FlowName, record.RunId, record.InstanceId);
        }

        var next = Clone(record);
        next.Version = record.Version + 1;
        next.LockedBy = runnerId;
        next.LockedUntil = leaseUntil;
        _records[key] = next;
        record.Version = next.Version;
        record.LockedBy = runnerId;
        record.LockedUntil = leaseUntil;
        return Task.CompletedTask;
    }

    public Task<bool> TryAcquireLeaseAsync(
        string flowName,
        string runId,
        string runnerId,
        DateTimeOffset leaseUntil,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runnerId))
        {
            throw new ArgumentException("Runner id is required.", nameof(runnerId));
        }

        lock (_claimGate)
        {
            var key = Key(flowName, runId);
            if (!_records.TryGetValue(key, out var current))
            {
                return Task.FromResult(false);
            }

            var now = DateTimeOffset.UtcNow;
            if (current.LockedBy is not null
                && current.LockedUntil is not null
                && current.LockedUntil > now
                && !string.Equals(current.LockedBy, runnerId, StringComparison.Ordinal))
            {
                return Task.FromResult(false);
            }

            current.LockedBy = runnerId;
            current.LockedUntil = leaseUntil;
            current.UpdatedAt = now;
            _records[key] = Clone(current);
            return Task.FromResult(true);
        }
    }

    public Task ReleaseLeaseAsync(string flowName, string runId, string runnerId, CancellationToken cancellationToken)
    {
        lock (_claimGate)
        {
            var key = Key(flowName, runId);
            if (!_records.TryGetValue(key, out var current))
            {
                return Task.CompletedTask;
            }

            if (!string.Equals(current.LockedBy, runnerId, StringComparison.Ordinal))
            {
                return Task.CompletedTask;
            }

            current.LockedBy = null;
            current.LockedUntil = null;
            current.UpdatedAt = DateTimeOffset.UtcNow;
            _records[key] = Clone(current);
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<ExecutionRecord>> ClaimDueAsync(
        string runnerId,
        DateTimeOffset leaseUntil,
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runnerId))
        {
            throw new ArgumentException("Runner id is required.", nameof(runnerId));
        }

        if (batchSize <= 0)
        {
            return Task.FromResult<IReadOnlyList<ExecutionRecord>>(Array.Empty<ExecutionRecord>());
        }

        lock (_claimGate)
        {
            var now = DateTimeOffset.UtcNow;
            var claimed = new List<ExecutionRecord>(batchSize);
            foreach (var candidate in _records.Values
                         .Where(r => IsClaimable(r, now))
                         .OrderBy(r => r.CreatedAt)
                         .Take(batchSize)
                         .ToList())
            {
                candidate.LockedBy = runnerId;
                candidate.LockedUntil = leaseUntil;
                candidate.UpdatedAt = now;
                _records[Key(candidate.FlowName, candidate.RunId)] = Clone(candidate);
                claimed.Add(Clone(candidate));
            }

            return Task.FromResult<IReadOnlyList<ExecutionRecord>>(claimed);
        }
    }

    private static bool IsClaimable(ExecutionRecord record, DateTimeOffset now)
    {
        if (!record.Status.IsOpen())
        {
            return false;
        }

        return record.LockedUntil is null || record.LockedUntil <= now;
    }

    private static string Key(string flowName, string runId)
        => flowName + DurablyLimits.InMemoryKeySeparator + runId;

    internal IReadOnlyList<ExecutionRecord> SnapshotAll()
    {
        return _records.Values.Select(Clone).ToList();
    }

    private static ExecutionRecord Clone(ExecutionRecord source) => new()
    {
        FlowName = source.FlowName,
        RunId = source.RunId,
        InstanceId = source.InstanceId,
        Status = source.Status,
        CurrentStep = source.CurrentStep,
        ContextJson = source.ContextJson,
        StepPathHash = source.StepPathHash,
        Attempts = source.Attempts,
        FailedStep = source.FailedStep,
        ErrorMessage = source.ErrorMessage,
        Version = source.Version,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt,
        LockedBy = source.LockedBy,
        LockedUntil = source.LockedUntil,
        MetadataJson = source.MetadataJson
    };
}
