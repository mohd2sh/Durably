using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

namespace Durably;

internal static class EfDatabaseInitializer
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly ConcurrentDictionary<string, byte> Migrated = new(StringComparer.Ordinal);

    public static async Task EnsureReadyAsync(
        IDbContextFactory<DurablyDbContext> contextFactory,
        EfStoreOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.AutoMigrate)
        {
            return;
        }

        var key = options.ConnectionString;
        if (!string.IsNullOrEmpty(key) && Migrated.ContainsKey(key))
        {
            return;
        }

        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            key ??= string.Empty;
            if (string.IsNullOrEmpty(key))
            {
                await using var probe = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                key = probe.Database.GetConnectionString() ?? string.Empty;
            }

            if (Migrated.ContainsKey(key))
            {
                return;
            }

            await MigrateWithRetryAsync(contextFactory, cancellationToken).ConfigureAwait(false);
            Migrated.TryAdd(key, 0);
            if (!string.IsNullOrEmpty(options.ConnectionString))
            {
                Migrated.TryAdd(options.ConnectionString, 0);
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task MigrateWithRetryAsync(
        IDbContextFactory<DurablyDbContext> contextFactory,
        CancellationToken cancellationToken)
    {
        var maxAttempts = DurablyLimits.DatabaseMigrateMaxAttempts;
        var delay = TimeSpan.FromSeconds(1);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                await ApplySchemaAsync(context, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception) when (attempt < maxAttempts)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = TimeSpan.FromMilliseconds(Math.Min(
                    delay.TotalMilliseconds * DurablyLimits.DatabaseMigrateBackoffMultiplier,
                    DurablyLimits.DatabaseMigrateInitialBackoffMilliseconds));
            }
        }

        await using var finalContext = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await ApplySchemaAsync(finalContext, cancellationToken).ConfigureAwait(false);
    }

    private static Task ApplySchemaAsync(DurablyDbContext context, CancellationToken cancellationToken)
    {
        // Migrations are authored against SQL Server (design-time factory). SQLite and Postgres
        // cannot apply that DDL (e.g. nvarchar); build schema from the provider-agnostic model instead.
        if (context.Database.IsSqlite() || context.Database.IsNpgsql())
        {
            return context.Database.EnsureCreatedAsync(cancellationToken);
        }

        return context.Database.MigrateAsync(cancellationToken);
    }
}
