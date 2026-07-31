using Durably.E2E.Tests.Flows;
using Durably.E2E.Tests.Models;
using Xunit;

namespace Durably.E2E.Tests.TestScenarios;

public abstract class HostReclaimScenarios<TFixture> : ScenarioTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected HostReclaimScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Survivor_host_reclaims_batch_after_first_host_stops()
    {
        // Arrange
        await ResetAsync();
        const int batch = 16;
        var flow = ScenarioFlows.CreateHappyPath();
        await using var first = await StartHostAsync(
            d => d.AddFlow(flow),
            o =>
            {
                o.WorkerEnabled = true;
                o.BatchSize = 8;
                o.MaxDegreeOfParallelism = 4;
                o.PollInterval = TestLimits.DefaultPollInterval;
                o.LeaseDuration = TestLimits.ShortLeaseDuration;
                o.RunnerId = "e2e-crash-1";
            });

        for (var i = 0; i < batch; i++)
        {
            await first.Engine.StartAsync(flow, $"reclaim-{i}", new OrderState());
        }

        // Act
        await first.DisposeAsync();
        await using var second = await StartHostAsync(
            d => d.AddFlow(flow),
            o =>
            {
                o.WorkerEnabled = true;
                o.BatchSize = 8;
                o.MaxDegreeOfParallelism = 4;
                o.PollInterval = TestLimits.DefaultPollInterval;
                o.LeaseDuration = TimeSpan.FromMinutes(1);
                o.RunnerId = "e2e-crash-2";
            });
        await second.WaitForCompletedCountAsync(
            flow.Name,
            batch,
            "reclaim-",
            TestLimits.DefaultWaitTimeout + TestLimits.ShortWaitTimeout);

        // Assert
        for (var i = 0; i < batch; i++)
        {
            var status = await second.Engine.GetStatusAsync(flow.Name, $"reclaim-{i}");
            Assert.Equal(ExecutionStatus.Completed, status?.Status);
        }
    }
}

[Collection(SqlServerE2ECollection.Name)]
public sealed class SqlServerHostReclaimScenarios : HostReclaimScenarios<SqlServerDatabaseFixture>
{
    public SqlServerHostReclaimScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresE2ECollection.Name)]
public sealed class PostgresHostReclaimScenarios : HostReclaimScenarios<PostgresDatabaseFixture>
{
    public PostgresHostReclaimScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
