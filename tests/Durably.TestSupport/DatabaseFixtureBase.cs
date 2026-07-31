using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Respawn;

namespace Durably.TestSupport;

public abstract class DatabaseFixtureBase : IDatabaseFixture
{
    private Respawner? _respawner;

    public abstract string ProviderName { get; }

    public abstract string ConnectionString { get; }

    public abstract DbConnection CreateConnection();

    public abstract Task InitializeAsync();

    protected abstract RespawnerOptions CreateRespawnerOptions();

    public abstract IDurablyBuilder ConfigureDurably(IDurablyBuilder builder, Action<EfStoreOptions>? configure = null);

    public virtual async Task DisposeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task ResetAsync()
    {
        if (_respawner is null)
        {
            await InitializeRespawnerAsync();
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await _respawner!.ResetAsync(connection);
    }

    protected async Task BootstrapSchemaAsync()
    {
        var services = new ServiceCollection();
        ConfigureDurably(services.AddDurably(), options => options.AutoMigrate = true);
        await using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IExecutionStore>();
        _ = await store.LoadAsync(
            TestLimits.BootstrapFlowName,
            TestLimits.BootstrapInstanceId,
            CancellationToken.None);
    }

    private async Task InitializeRespawnerAsync()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, CreateRespawnerOptions());
    }

    protected static string[] SchemasToReset { get; } = { TestLimits.DurableSchema };
}
