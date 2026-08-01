using Microsoft.EntityFrameworkCore;

namespace Durably;

/// <summary>EF Core-backed <see cref="IExecutionStore"/> with optimistic concurrency and leases.</summary>
internal sealed class EfExecutionStore : IExecutionStore
{
    private readonly IDbContextFactory<DurablyDbContext> _contextFactory;
    private readonly EfStoreOptions _options;

    public EfExecutionStore(IDbContextFactory<DurablyDbContext> contextFactory, EfStoreOptions options)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ExecutionRecord?> LoadAsync(string flowName, string runId, CancellationToken cancellationToken)
    {
        await EfDatabaseInitializer.EnsureReadyAsync(_contextFactory, _options, cancellationToken).ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var entity = await context.Executions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.FlowName == flowName && e.RunId == runId,
                cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ExecutionMapper.ToRecord(entity);
    }

    public async Task<ExecutionRecord?> FindOpenAsync(
        string flowName,
        string instanceId,
        CancellationToken cancellationToken)
    {
        await EfDatabaseInitializer.EnsureReadyAsync(_contextFactory, _options, cancellationToken).ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var pending = (int)ExecutionStatus.Pending;
        var running = (int)ExecutionStatus.Running;
        var entity = await context.Executions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.FlowName == flowName
                    && e.InstanceId == instanceId
                    && (e.Status == pending || e.Status == running),
                cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ExecutionMapper.ToRecord(entity);
    }

    public async Task<ExecutionRecord?> LoadLatestAsync(
        string flowName,
        string instanceId,
        CancellationToken cancellationToken)
    {
        await EfDatabaseInitializer.EnsureReadyAsync(_contextFactory, _options, cancellationToken).ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var entity = await context.Executions
            .AsNoTracking()
            .Where(e => e.FlowName == flowName && e.InstanceId == instanceId)
            .OrderByDescending(e => e.UpdatedAt)
            .ThenByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ExecutionMapper.ToRecord(entity);
    }

    public async Task CreateAsync(ExecutionRecord record, CancellationToken cancellationToken)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        await EfDatabaseInitializer.EnsureReadyAsync(_contextFactory, _options, cancellationToken).ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        context.Executions.Add(ExecutionMapper.ToEntity(record));

        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (EfPersistenceExceptionHelper.IsDuplicateKey(ex))
        {
            throw new ExecutionAlreadyExistsException(record.FlowName, record.InstanceId, record.RunId);
        }
    }

    public async Task SaveCheckpointAsync(
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

        await EfDatabaseInitializer.EnsureReadyAsync(_contextFactory, _options, cancellationToken).ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var expectedVersion = record.Version;
        var updatedAt = record.UpdatedAt.UtcDateTime;
        var lockedUntil = leaseUntil.UtcDateTime;

        var affected = await context.Executions
            .Where(e => e.FlowName == record.FlowName
                && e.RunId == record.RunId
                && e.Version == expectedVersion
                && e.LockedBy == runnerId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(e => e.Status, (int)record.Status)
                    .SetProperty(e => e.CurrentStep, record.CurrentStep)
                    .SetProperty(e => e.ContextJson, record.ContextJson)
                    .SetProperty(e => e.StepPathHash, record.StepPathHash)
                    .SetProperty(e => e.Attempts, record.Attempts)
                    .SetProperty(e => e.FailedStep, record.FailedStep)
                    .SetProperty(e => e.ErrorMessage, record.ErrorMessage)
                    .SetProperty(e => e.LockedUntil, lockedUntil)
                    .SetProperty(e => e.UpdatedAt, updatedAt)
                    .SetProperty(e => e.Version, e => e.Version + 1),
                cancellationToken)
            .ConfigureAwait(false);

        if (affected == 0)
        {
            var current = await LoadAsync(record.FlowName, record.RunId, cancellationToken).ConfigureAwait(false);
            if (current is null || string.Equals(current.LockedBy, runnerId, StringComparison.Ordinal))
            {
                throw new ConcurrencyConflictException(record.FlowName, record.RunId, record.InstanceId);
            }

            throw new LeaseLostException(record.FlowName, record.RunId, record.InstanceId);
        }

        record.Version += 1;
        record.LockedBy = runnerId;
        record.LockedUntil = leaseUntil;
    }

    public async Task<bool> TryAcquireLeaseAsync(
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

        await EfDatabaseInitializer.EnsureReadyAsync(_contextFactory, _options, cancellationToken).ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var lockedUntil = leaseUntil.UtcDateTime;

        var affected = await context.Executions
            .Where(e => e.FlowName == flowName
                && e.RunId == runId
                && (e.LockedUntil == null || e.LockedUntil <= now || e.LockedBy == runnerId))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(e => e.LockedBy, runnerId)
                    .SetProperty(e => e.LockedUntil, lockedUntil)
                    .SetProperty(e => e.UpdatedAt, now),
                cancellationToken)
            .ConfigureAwait(false);

        return affected > 0;
    }

    public async Task ReleaseLeaseAsync(
        string flowName,
        string runId,
        string runnerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runnerId))
        {
            throw new ArgumentException("Runner id is required.", nameof(runnerId));
        }

        await EfDatabaseInitializer.EnsureReadyAsync(_contextFactory, _options, cancellationToken).ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await context.Executions
            .Where(e => e.FlowName == flowName && e.RunId == runId && e.LockedBy == runnerId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(e => e.LockedBy, (string?)null)
                    .SetProperty(e => e.LockedUntil, (DateTime?)null)
                    .SetProperty(e => e.UpdatedAt, DateTime.UtcNow),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ExecutionRecord>> ClaimDueAsync(
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
            return Array.Empty<ExecutionRecord>();
        }

        await EfDatabaseInitializer.EnsureReadyAsync(_contextFactory, _options, cancellationToken).ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await ClaimDueSql.ClaimAsync(
                context,
                _options.Schema,
                runnerId,
                leaseUntil,
                batchSize,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
