using System.Diagnostics;
using Durably.E2E.Tests.Flows;
using Durably.E2E.Tests.Models;
using Xunit;

namespace Durably.E2E.Tests.TestScenarios;

public abstract class CrossHostLatencyScenarios<TFixture> : ScenarioTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected CrossHostLatencyScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Enqueue_on_producer_starts_on_worker_within_poll_window()
    {
        // Arrange
        await ResetAsync();
        var flow = ScenarioFlows.CreateHappyPath();
        var pollInterval = TimeSpan.FromMilliseconds(500);
        await using var producer = await StartHostAsync(
            d => d.AddFlow(flow),
            o =>
            {
                o.WorkerEnabled = false;
                o.RunnerId = "e2e-producer";
            });
        await using var worker = await StartHostAsync(
            d => d.AddFlow(flow),
            o =>
            {
                o.WorkerEnabled = true;
                o.BatchSize = 8;
                o.MaxDegreeOfParallelism = 4;
                o.PollInterval = pollInterval;
                o.LeaseDuration = TimeSpan.FromMinutes(1);
                o.RunnerId = "e2e-consumer";
            });

        // Act
        var watch = Stopwatch.StartNew();
        await producer.Engine.StartAsync(flow, "cross-host-1", new OrderState());
        await worker.WaitForStatusAsync(
            flow.Name,
            "cross-host-1",
            ExecutionStatus.Completed,
            TestLimits.DefaultWaitTimeout);
        watch.Stop();

        // Assert
        Assert.True(
            watch.Elapsed < pollInterval + TimeSpan.FromSeconds(3),
            $"Cross-host start latency {watch.Elapsed} exceeded poll window.");
    }
}

[Collection(SqlServerE2ECollection.Name)]
public sealed class SqlServerCrossHostLatencyScenarios : CrossHostLatencyScenarios<SqlServerDatabaseFixture>
{
    public SqlServerCrossHostLatencyScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresE2ECollection.Name)]
public sealed class PostgresCrossHostLatencyScenarios : CrossHostLatencyScenarios<PostgresDatabaseFixture>
{
    public PostgresCrossHostLatencyScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
