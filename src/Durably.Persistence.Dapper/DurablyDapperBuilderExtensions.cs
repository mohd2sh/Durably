using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
#if NET8_0_OR_GREATER
using Npgsql;
#endif

namespace Durably;

/// <summary>Registers the Dapper-backed <see cref="IExecutionStore"/> on an <see cref="IDurablyBuilder"/>.</summary>
public static class DurablyDapperBuilderExtensions
{
    /// <summary>
    /// Use the Dapper store with an explicit dialect and connection factory. This is the
    /// driver-agnostic entry point: pass any ADO.NET connection (SQL Server, PostgreSQL, SQLite, ...).
    /// </summary>
    public static IDurablyBuilder UseDapper(
        this IDurablyBuilder builder,
        ISqlDialect dialect,
        Func<DbConnection> connectionFactory,
        Action<DapperStoreOptions>? configure = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var options = new DapperStoreOptions();
        configure?.Invoke(options);

        var factory = new DelegateDbConnectionFactory(connectionFactory);

        builder.Services.RemoveAll<IExecutionStore>();
        builder.Services.AddSingleton<IExecutionStore>(_ => new DapperExecutionStore(factory, dialect, options));

        builder.Services.RemoveAll<ITraceStore>();
        builder.Services.AddSingleton<ITraceStore>(_ => new DapperTraceStore(factory, dialect, options));

        builder.Services.RemoveAll<IExecutionQuery>();
        builder.Services.AddSingleton<IExecutionQuery>(_ => new DapperExecutionQuery(factory, dialect, options));

        builder.Services.RemoveAll<ITraceQuery>();
        builder.Services.AddSingleton<ITraceQuery>(sp => new TraceStoreQuery(sp.GetRequiredService<ITraceStore>()));

        return builder;
    }

    /// <summary>Use SQL Server with a connection string. Durably owns connection creation.</summary>
    public static IDurablyBuilder UseSqlServer(
        this IDurablyBuilder builder,
        string connectionString,
        Action<DapperStoreOptions>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        return builder.UseSqlServer(() => new SqlConnection(connectionString), configure);
    }

    /// <summary>Convenience wrapper for SQL Server when callers need custom connection creation.</summary>
    public static IDurablyBuilder UseSqlServer(
        this IDurablyBuilder builder,
        Func<DbConnection> connectionFactory,
        Action<DapperStoreOptions>? configure = null)
        => builder.UseDapper(new SqlServerDialect(), connectionFactory, configure);

#if NET8_0_OR_GREATER
    /// <summary>Use PostgreSQL with a connection string. Durably owns connection creation.</summary>
    public static IDurablyBuilder UsePostgreSql(
        this IDurablyBuilder builder,
        string connectionString,
        Action<DapperStoreOptions>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        return builder.UsePostgreSql(() => new NpgsqlConnection(connectionString), configure);
    }
#endif

    /// <summary>Convenience wrapper for PostgreSQL when callers need custom connection creation.</summary>
    public static IDurablyBuilder UsePostgreSql(
        this IDurablyBuilder builder,
        Func<DbConnection> connectionFactory,
        Action<DapperStoreOptions>? configure = null)
        => builder.UseDapper(new PostgreSqlDialect(), connectionFactory, configure);

#if NET8_0_OR_GREATER
    /// <summary>Alias for <see cref="UsePostgreSql(IDurablyBuilder, string, Action{DapperStoreOptions})"/>.</summary>
    public static IDurablyBuilder UsePostgres(
        this IDurablyBuilder builder,
        string connectionString,
        Action<DapperStoreOptions>? configure = null)
        => builder.UsePostgreSql(connectionString, configure);
#endif

    /// <summary>Alias for <see cref="UsePostgreSql(IDurablyBuilder, Func{DbConnection}, Action{DapperStoreOptions})"/>.</summary>
    public static IDurablyBuilder UsePostgres(
        this IDurablyBuilder builder,
        Func<DbConnection> connectionFactory,
        Action<DapperStoreOptions>? configure = null)
        => builder.UsePostgreSql(connectionFactory, configure);

    /// <summary>Convenience wrapper for SQLite. Requires a SqliteConnection factory supplied by the caller.</summary>
    public static IDurablyBuilder UseSqlite(
        this IDurablyBuilder builder,
        Func<DbConnection> connectionFactory,
        Action<DapperStoreOptions>? configure = null)
        => builder.UseDapper(new SqliteDialect(), connectionFactory, configure);
}
