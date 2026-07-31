using Microsoft.Extensions.DependencyInjection;

namespace Durably.Persistence.EntityFrameworkCore.IntegrationTests;

public abstract class ProviderTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected ProviderTestsBase(TFixture database)
    {
        Database = database;
    }

    protected TFixture Database { get; }

    protected Task ResetAsync() => Database.ResetAsync();

    protected IServiceProvider CreateProvider(Action<EfStoreOptions>? configureStore = null)
    {
        var services = new ServiceCollection();
        Database.ConfigureDurably(
            services.AddDurably(),
            o =>
            {
                o.AutoMigrate = false;
                configureStore?.Invoke(o);
            });
        return services.BuildServiceProvider();
    }

    protected IExecutionStore NewStore()
        => CreateProvider().GetRequiredService<IExecutionStore>();

    protected IExecutionQuery NewQuery()
        => CreateProvider().GetRequiredService<IExecutionQuery>();

    protected ITraceStore NewTraceStore()
        => CreateProvider().GetRequiredService<ITraceStore>();
}
