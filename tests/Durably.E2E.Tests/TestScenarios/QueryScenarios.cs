using Durably.E2E.Tests.Flows;
using Durably.E2E.Tests.Models;
using Xunit;

namespace Durably.E2E.Tests.TestScenarios;

public abstract class QueryScenarios<TFixture> : ScenarioTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected QueryScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task SearchAsync_filters_by_instance_id_and_metadata()
    {
        await ResetAsync();
        var flow = ScenarioFlows.CreateHappyPath();

        await using var host = await StartHostAsync(d => d.AddFlow(flow));

        await host.Engine.StartAsync(
            flow,
            "order-100",
            new OrderState(),
            new FlowStartOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["orderId"] = "100",
                    ["customerId"] = "c1"
                }
            });

        await host.WaitForStatusAsync(flow.Name, "order-100", ExecutionStatus.Completed);

        var byInstance = await host.Query.SearchAsync(new ExecutionSearchCriteria
        {
            InstanceId = "order-100"
        }, default);

        Assert.Single(byInstance.Items);
        Assert.Equal("order-100", byInstance.Items[0].InstanceId);

        var byMetadata = await host.Query.SearchAsync(new ExecutionSearchCriteria
        {
            MetadataKey = "customerId",
            MetadataValue = "c1"
        }, default);

        Assert.Contains(byMetadata.Items, i => i.InstanceId == "order-100");
    }
}

[Collection(SqlServerE2ECollection.Name)]
public sealed class SqlServerQueryScenarios : QueryScenarios<SqlServerDatabaseFixture>
{
    public SqlServerQueryScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresE2ECollection.Name)]
public sealed class PostgresQueryScenarios : QueryScenarios<PostgresDatabaseFixture>
{
    public PostgresQueryScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
