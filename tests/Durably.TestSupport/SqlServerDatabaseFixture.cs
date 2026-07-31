using System.Data.Common;
using Microsoft.Data.SqlClient;
using Respawn;
using Testcontainers.MsSql;

namespace Durably.TestSupport;

public sealed class SqlServerDatabaseFixture : DatabaseFixtureBase
{
    private MsSqlContainer? _container;

    public override string ProviderName => "SqlServer";

    public override string ConnectionString =>
        _container?.GetConnectionString()
        ?? throw new InvalidOperationException("SQL Server container has not been started.");

    public override DbConnection CreateConnection() => new SqlConnection(ConnectionString);

    public override IDurablyBuilder ConfigureDurably(IDurablyBuilder builder, Action<EfStoreOptions>? configure = null) =>
        builder.UseSqlServer(ConnectionString, configure);

    protected override RespawnerOptions CreateRespawnerOptions() => new()
    {
        DbAdapter = DbAdapter.SqlServer,
        SchemasToInclude = SchemasToReset
    };

    public override async Task InitializeAsync()
    {
        _container = new MsSqlBuilder(TestLimits.SqlServerImage)
            .WithPassword(TestLimits.SqlServerContainerPassword)
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
