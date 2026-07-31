using Durably.E2E.Tests.Flows;
using Durably.E2E.Tests.Models;
using Xunit;

namespace Durably.E2E.Tests.TestScenarios;

public abstract class HappyPathScenarios<TFixture> : ScenarioTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected HappyPathScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Worker_completes_happy_path_and_persists_context()
    {
        await ResetAsync();
        var flow = ScenarioFlows.CreateHappyPath();

        await using var host = await StartHostAsync(d => d.AddFlow(flow));

        await host.Engine.StartAsync(flow, "happy-1", new OrderState());
        await host.WaitForStatusAsync(flow.Name, "happy-1", ExecutionStatus.Completed);

        var state = await host.LoadStateAsync<OrderState>(flow.Name, "happy-1");
        Assert.Equal("report", state.Report);
        Assert.True(state.EmailSent);
        Assert.True(state.Finalized);
    }

    [Fact]
    public async Task Oop_flow_completes_identically()
    {
        await ResetAsync();

        await using var host = await StartHostAsync(d => d.AddFlow<OrderFlow, OrderState>());

        await host.Engine.StartAsync<OrderFlow, OrderState>("oop-1", new OrderState());
        var flowName = typeof(OrderFlow).FullName!;
        await host.WaitForStatusAsync(flowName, "oop-1", ExecutionStatus.Completed);

        var state = await host.LoadStateAsync<OrderState>(flowName, "oop-1");
        Assert.Equal("report", state.Report);
        Assert.True(state.EmailSent);
        Assert.True(state.Finalized);
    }

    [Fact]
    public async Task Start_options_metadata_is_searchable()
    {
        await ResetAsync();
        var flow = ScenarioFlows.CreateHappyPath();

        await using var host = await StartHostAsync(d => d.AddFlow(flow));

        await host.Engine.StartAsync(
            flow,
            "meta-1",
            new OrderState(),
            new FlowStartOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["orderId"] = "42",
                    ["customerId"] = "c-9"
                }
            });

        await host.WaitForStatusAsync(flow.Name, "meta-1", ExecutionStatus.Completed);

        var byMetadata = await host.Query.SearchAsync(new ExecutionSearchCriteria
        {
            MetadataKey = "customerId",
            MetadataValue = "c-9"
        }, default);

        Assert.Contains(byMetadata.Items, i => i.InstanceId == "meta-1");
    }
}

[Collection(SqlServerE2ECollection.Name)]
public sealed class SqlServerHappyPathScenarios : HappyPathScenarios<SqlServerDatabaseFixture>
{
    public SqlServerHappyPathScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresE2ECollection.Name)]
public sealed class PostgresHappyPathScenarios : HappyPathScenarios<PostgresDatabaseFixture>
{
    public PostgresHappyPathScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
