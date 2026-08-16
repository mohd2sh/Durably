using Xunit;

namespace Durably.Core.UnitTests.Queries;
public sealed class InMemoryExecutionQueryTests
{
    private const string OrdersFlow = "OrdersFlow";
    private const string InstanceA = "order-100";
    private const string InstanceB = "order-200";
    private const string CustomerMetadataKey = "customerId";
    private const string CustomerMetadataValue = "c1";

    [Fact]
    public async Task SearchAsync_filters_by_instance_id()
    {
        // Arrange
        var store = new InMemoryExecutionStore();
        var query = new InMemoryExecutionQuery(store);
        await SeedAsync(store, OrdersFlow, InstanceA, ExecutionStatus.Completed, $$"""{"{{CustomerMetadataKey}}":"{{CustomerMetadataValue}}"}""");
        await SeedAsync(store, OrdersFlow, InstanceB, ExecutionStatus.Pending, "{}");

        // Act
        var page = await query.SearchAsync(new ExecutionSearchCriteria { InstanceId = InstanceA }, CancellationToken.None);

        // Assert
        Assert.Single(page.Items);
        Assert.Equal(InstanceA, page.Items[0].InstanceId);
    }

    [Fact]
    public async Task SearchAsync_filters_by_metadata()
    {
        // Arrange
        var store = new InMemoryExecutionStore();
        var query = new InMemoryExecutionQuery(store);
        await SeedAsync(store, OrdersFlow, InstanceA, ExecutionStatus.Completed, $$"""{"{{CustomerMetadataKey}}":"{{CustomerMetadataValue}}"}""");
        await SeedAsync(store, OrdersFlow, InstanceB, ExecutionStatus.Completed, """{"customerId":"other"}""");

        // Act
        var page = await query.SearchAsync(new ExecutionSearchCriteria
        {
            MetadataKey = CustomerMetadataKey,
            MetadataValue = CustomerMetadataValue
        }, CancellationToken.None);

        // Assert
        Assert.Single(page.Items);
        Assert.Equal(InstanceA, page.Items[0].InstanceId);
    }

    [Fact]
    public async Task SearchAsync_applies_skip_and_take()
    {
        // Arrange
        var store = new InMemoryExecutionStore();
        var query = new InMemoryExecutionQuery(store);
        const int totalRecords = 5;
        const int skip = 2;
        const int take = 2;
        for (var i = 0; i < totalRecords; i++)
        {
            await SeedAsync(store, OrdersFlow, $"inst-{i}", ExecutionStatus.Pending, "{}");
        }

        // Act
        var page = await query.SearchAsync(new ExecutionSearchCriteria
        {
            FlowName = OrdersFlow,
            Skip = skip,
            Take = take
        }, CancellationToken.None);

        // Assert
        Assert.Equal(totalRecords, page.TotalCount);
        Assert.Equal(take, page.Items.Count);
        Assert.Equal(skip, page.Skip);
        Assert.Equal(take, page.Take);
    }

    [Fact]
    public async Task GetAsync_returns_detail_when_present()
    {
        // Arrange
        var store = new InMemoryExecutionStore();
        var query = new InMemoryExecutionQuery(store);
        await SeedAsync(store, OrdersFlow, InstanceA, ExecutionStatus.Running, "{}");

        // Act
        var detail = await query.GetAsync(OrdersFlow, InstanceA, runId: null, CancellationToken.None);

        // Assert
        Assert.NotNull(detail);
        Assert.Equal(OrdersFlow, detail!.FlowName);
        Assert.Equal(InstanceA, detail.InstanceId);
        Assert.Equal(ExecutionStatus.Running, detail.Status);
    }

    private static async Task SeedAsync(
        InMemoryExecutionStore store,
        string flowName,
        string instanceId,
        ExecutionStatus status,
        string metadataJson)
    {
        var now = DateTimeOffset.UtcNow;
        await store.CreateAsync(new ExecutionRecord
        {
            FlowName = flowName,
            RunId = Guid.NewGuid().ToString("N"),
            InstanceId = instanceId,
            Status = status,
            CurrentStep = 0,
            ContextJson = "{}",
            MetadataJson = metadataJson,
            Attempts = 0,
            Version = 0,
            CreatedAt = now,
            UpdatedAt = now
        }, CancellationToken.None);
    }
}
