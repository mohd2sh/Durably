using Durably.E2E.Tests.Flows;
using Durably.E2E.Tests.Models;
using Xunit;

namespace Durably.E2E.Tests.TestScenarios;

public abstract class PoisonQuarantineScenarios<TFixture> : ScenarioTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    private sealed class NeighborFlow;

    protected PoisonQuarantineScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Unregistered_flow_fails_without_blocking_neighbors()
    {
        // Arrange
        await ResetAsync();
        const int neighborCount = 20;
        const string poisonFlowName = "E2E.Missing.Flow";
        const string poisonInstanceId = "poison-1";
        var goodFlow = Flow.For<NeighborFlow, OrderState>()
            .Step("work", (_, _) => Task.CompletedTask);

        await using var host = await StartHostAsync(
            d => d.AddFlow(goodFlow),
            o =>
            {
                o.WorkerEnabled = true;
                o.BatchSize = 8;
                o.MaxDegreeOfParallelism = 4;
                o.PollInterval = TestLimits.DefaultPollInterval;
                o.LeaseDuration = TimeSpan.FromMinutes(1);
                o.RunnerId = "e2e-poison";
            });

        var now = DateTimeOffset.UtcNow;
        await host.Store.CreateAsync(
            new ExecutionRecord
            {
                FlowName = poisonFlowName,
                InstanceId = poisonInstanceId,
                Status = ExecutionStatus.Pending,
                CurrentStep = 0,
                ContextJson = "{}",
                Attempts = 0,
                Version = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            CancellationToken.None);

        // Act
        for (var i = 0; i < neighborCount; i++)
        {
            await host.Engine.StartAsync(goodFlow, $"ok-{i}", new OrderState());
        }

        await host.WaitForCompletedCountAsync(
            goodFlow.Name,
            neighborCount,
            "ok-",
            TestLimits.DefaultWaitTimeout);
        await host.WaitForStatusAsync(
            poisonFlowName,
            poisonInstanceId,
            ExecutionStatus.Failed,
            TestLimits.DefaultWaitTimeout);

        // Assert
        var poison = await host.Store.LoadAsync(poisonFlowName, poisonInstanceId, CancellationToken.None);
        Assert.Equal(ExecutionStatus.Failed, poison!.Status);
        Assert.Contains("not registered", poison.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }
}

[Collection(SqlServerE2ECollection.Name)]
public sealed class SqlServerPoisonQuarantineScenarios : PoisonQuarantineScenarios<SqlServerDatabaseFixture>
{
    public SqlServerPoisonQuarantineScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresE2ECollection.Name)]
public sealed class PostgresPoisonQuarantineScenarios : PoisonQuarantineScenarios<PostgresDatabaseFixture>
{
    public PostgresPoisonQuarantineScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
