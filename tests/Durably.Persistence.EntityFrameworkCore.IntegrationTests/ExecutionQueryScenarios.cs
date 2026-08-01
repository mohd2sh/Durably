using Xunit;

namespace Durably.Persistence.EntityFrameworkCore.IntegrationTests;

public abstract class ExecutionQueryScenarios<TFixture> : ProviderTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    private const string OrdersFlow = "OrdersFlow";
    private const string InstanceA = "order-100";
    private const string InstanceB = "order-200";
    private const string CustomerMetadataKey = "customerId";
    private const string CustomerMetadataValue = "c1";
    private const string MetadataJson = """{"orderId":"100","customerId":"c1"}""";

    protected ExecutionQueryScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task SearchAsync_filters_by_instance_id_and_metadata()
    {
        // Arrange
        await ResetAsync();
        var store = NewStore();
        var query = NewQuery();
        var now = DateTimeOffset.UtcNow;

        await store.CreateAsync(new ExecutionRecord
        {
            FlowName = OrdersFlow,
            RunId = Guid.NewGuid().ToString("N"),
            InstanceId = InstanceA,
            Status = ExecutionStatus.Completed,
            CurrentStep = 3,
            ContextJson = TestConstants.EmptyContextJson,
            MetadataJson = MetadataJson,
            Version = 0,
            CreatedAt = now,
            UpdatedAt = now
        }, CancellationToken.None);

        await store.CreateAsync(new ExecutionRecord
        {
            FlowName = OrdersFlow,
            RunId = Guid.NewGuid().ToString("N"),
            InstanceId = InstanceB,
            Status = ExecutionStatus.Pending,
            CurrentStep = 0,
            ContextJson = TestConstants.EmptyContextJson,
            MetadataJson = """{"customerId":"other"}""",
            Version = 0,
            CreatedAt = now,
            UpdatedAt = now
        }, CancellationToken.None);

        // Act
        var byInstance = await query.SearchAsync(new ExecutionSearchCriteria
        {
            InstanceId = InstanceA
        }, CancellationToken.None);

        var byMetadata = await query.SearchAsync(new ExecutionSearchCriteria
        {
            MetadataKey = CustomerMetadataKey,
            MetadataValue = CustomerMetadataValue
        }, CancellationToken.None);

        var byStatus = await query.SearchAsync(new ExecutionSearchCriteria
        {
            FlowName = OrdersFlow,
            Status = ExecutionStatus.Completed
        }, CancellationToken.None);

        // Assert
        Assert.Single(byInstance.Items);
        Assert.Equal(InstanceA, byInstance.Items[0].InstanceId);
        Assert.Contains(byMetadata.Items, i => i.InstanceId == InstanceA);
        Assert.Single(byStatus.Items);
    }

    [Fact]
    public async Task SearchAsync_applies_skip_and_take()
    {
        // Arrange
        await ResetAsync();
        var store = NewStore();
        var query = NewQuery();
        const int total = 5;
        const int skip = 1;
        const int take = 2;
        for (var i = 0; i < total; i++)
        {
            await store.CreateAsync(
                ExecutionRecordFactory.Create(OrdersFlow, $"page-{i}", ExecutionStatus.Pending),
                CancellationToken.None);
        }

        // Act
        var page = await query.SearchAsync(new ExecutionSearchCriteria
        {
            FlowName = OrdersFlow,
            Skip = skip,
            Take = take
        }, CancellationToken.None);

        // Assert
        Assert.Equal(total, page.TotalCount);
        Assert.Equal(take, page.Items.Count);
        Assert.Equal(skip, page.Skip);
        Assert.Equal(take, page.Take);
    }
}

[Collection(SqlServerEfIntegrationCollection.Name)]
public sealed class SqlServerExecutionQueryScenarios : ExecutionQueryScenarios<SqlServerDatabaseFixture>
{
    public SqlServerExecutionQueryScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresEfIntegrationCollection.Name)]
public sealed class PostgresExecutionQueryScenarios : ExecutionQueryScenarios<PostgresDatabaseFixture>
{
    public PostgresExecutionQueryScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
