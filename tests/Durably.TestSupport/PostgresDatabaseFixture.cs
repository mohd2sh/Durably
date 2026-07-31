using System.Data.Common;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace Durably.TestSupport;

public sealed class PostgresDatabaseFixture : DatabaseFixtureBase
{
    private PostgreSqlContainer? _container;

    public override string ProviderName => "Postgres";

    public override string ConnectionString =>
        _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Postgres container has not been started.");

    public override DbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);

    public override IDurablyBuilder ConfigureDurably(IDurablyBuilder builder, Action<EfStoreOptions>? configure = null) =>
        builder.UsePostgres(ConnectionString, configure);

    protected override RespawnerOptions CreateRespawnerOptions() => new()
    {
        DbAdapter = DbAdapter.Postgres,
        SchemasToInclude = SchemasToReset
    };

    public override async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder(TestLimits.PostgresImage)
            .WithDatabase(TestLimits.PostgresDatabaseName)
            .WithUsername(TestLimits.PostgresUsername)
            .WithPassword(TestLimits.PostgresPassword)
            .Build();
        await _container.StartAsync();
        await BootstrapSchemaAsync();
    }

    public override async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
