using Microsoft.Extensions.DependencyInjection;

namespace Durably.TestSupport;

public abstract class ScenarioTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected ScenarioTestsBase(TFixture database)
    {
        Database = database;
    }

    protected TFixture Database { get; }

    protected Task ResetAsync() => Database.ResetAsync();

    protected Task<ScenarioHost> StartHostAsync(
        Action<IDurablyBuilder> configure,
        Action<ScenarioHostOptions>? configureOptions = null)
        => ScenarioHost.StartAsync(Database, configure, configureOptions);

    protected IExecutionStore NewExecutionStore()
    {
        var services = new ServiceCollection();
        Database.ConfigureDurably(services.AddDurably(), o => o.AutoMigrate = false);
        return services.BuildServiceProvider().GetRequiredService<IExecutionStore>();
    }
}
