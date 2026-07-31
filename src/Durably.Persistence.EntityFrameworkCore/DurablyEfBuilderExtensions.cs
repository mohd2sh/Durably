using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Durably;

/// <summary>Registers the EF Core-backed persistence layer on an <see cref="IDurablyBuilder"/>.</summary>
public static class DurablyEfBuilderExtensions
{
    /// <summary>Use SQL Server with a connection string.</summary>
    public static IDurablyBuilder UseSqlServer(
        this IDurablyBuilder builder,
        string connectionString,
        Action<EfStoreOptions>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        var options = CreateOptions(configure);
        options.ConnectionString = connectionString;
        return RegisterProvider(
            builder,
            options,
            db => db.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", options.Schema);
                sql.CommandTimeout(options.CommandTimeoutSeconds);
            }));
    }

    /// <summary>Use PostgreSQL with a connection string.</summary>
    public static IDurablyBuilder UsePostgres(
        this IDurablyBuilder builder,
        string connectionString,
        Action<EfStoreOptions>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        var options = CreateOptions(configure);
        options.ConnectionString = connectionString;
        return RegisterProvider(
            builder,
            options,
            db => db.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", options.Schema);
                npgsql.CommandTimeout(options.CommandTimeoutSeconds);
            }));
    }

    /// <summary>Use SQLite with a connection string (tests and local dev).</summary>
    public static IDurablyBuilder UseSqlite(
        this IDurablyBuilder builder,
        string connectionString,
        Action<EfStoreOptions>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        var options = CreateOptions(configure);
        options.ConnectionString = connectionString;
        return RegisterProvider(
            builder,
            options,
            db => db.UseSqlite(connectionString, sqlite =>
            {
                sqlite.MigrationsHistoryTable("__EFMigrationsHistory", options.Schema);
                sqlite.CommandTimeout(options.CommandTimeoutSeconds);
            }));
    }

    private static EfStoreOptions CreateOptions(Action<EfStoreOptions>? configure)
    {
        var options = new EfStoreOptions();
        configure?.Invoke(options);
        if (string.IsNullOrWhiteSpace(options.Schema))
        {
            options.Schema = "durable";
        }

        return options;
    }

    private static IDurablyBuilder RegisterProvider(
        IDurablyBuilder builder,
        EfStoreOptions options,
        Action<DbContextOptionsBuilder> configureProvider)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.RemoveAll<EfStoreOptions>();
        builder.Services.AddSingleton(options);

        builder.Services.RemoveAll<IDbContextFactory<DurablyDbContext>>();
        builder.Services.AddDbContextFactory<DurablyDbContext>((_, db) => configureProvider(db));

        builder.Services.RemoveAll<IExecutionStore>();
        builder.Services.AddSingleton<IExecutionStore, EfExecutionStore>();

        builder.Services.RemoveAll<ITraceStore>();
        builder.Services.AddSingleton<ITraceStore, EfTraceStore>();

        builder.Services.RemoveAll<IExecutionQuery>();
        builder.Services.AddSingleton<IExecutionQuery, EfExecutionQuery>();

        builder.Services.RemoveAll<ITraceQuery>();
        builder.Services.AddSingleton<ITraceQuery>(sp =>
            new TraceStoreQuery(sp.GetRequiredService<ITraceStore>()));

        return builder;
    }
}
