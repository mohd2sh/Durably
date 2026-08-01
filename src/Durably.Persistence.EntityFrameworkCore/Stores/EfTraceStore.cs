using Microsoft.EntityFrameworkCore;

namespace Durably;

/// <summary>EF Core-backed <see cref="ITraceStore"/>.</summary>
internal sealed class EfTraceStore : ITraceStore
{
    private readonly IDbContextFactory<DurablyDbContext> _contextFactory;
    private readonly EfStoreOptions _options;

    public EfTraceStore(IDbContextFactory<DurablyDbContext> contextFactory, EfStoreOptions options)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task AppendAsync(IReadOnlyList<TraceRecord> records, CancellationToken cancellationToken)
    {
        if (records is null || records.Count == 0)
        {
            return;
        }

        await EfDatabaseInitializer.EnsureReadyAsync(_contextFactory, _options, cancellationToken).ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        context.Traces.AddRange(records.Select(TraceMapper.ToEntity));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TraceRecord>> LoadAsync(
        string flowName,
        string runId,
        CancellationToken cancellationToken)
    {
        await EfDatabaseInitializer.EnsureReadyAsync(_contextFactory, _options, cancellationToken).ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = await context.Traces
            .AsNoTracking()
            .Where(t => t.FlowName == flowName && t.RunId == runId)
            .OrderBy(t => t.Timestamp)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(TraceMapper.ToRecord).ToList();
    }
}
