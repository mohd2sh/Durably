using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Durably.Load.Tests;

public abstract class DrainScenarios<TFixture> : LoadTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected DrainScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Backlog_of_10k_drains_faster_than_poll_interval_cap()
    {
        // Arrange
        await ResetAsync();
        const int backlog = LoadLimits.DrainBacklog;
        const int batchSize = LoadLimits.WorkerBatchSize;
        var referencePoll = LoadLimits.ReferencePollInterval;
        var executeCount = 0;
        var flow = LoadFlows.CreateCountingStep(() => Interlocked.Increment(ref executeCount));
        var contextJson = JsonSerializer.Serialize(new LoadState());

        await using var host = await StartHostAsync(
            d => d.AddFlow(flow),
            o =>
            {
                o.WorkerEnabled = true;
                o.BatchSize = batchSize;
                o.MaxDegreeOfParallelism = LoadLimits.WorkerMaxDegreeOfParallelism;
                o.PollInterval = LoadLimits.WorkerPollInterval;
                o.LeaseDuration = LoadLimits.LeaseDuration;
                o.RunnerId = "load-drain-1";
            });

        await LoadBacklogSeeder.SeedPendingAsync(
            host.Store,
            flow.Name,
            "drain-",
            backlog,
            contextJson);

        // Act
        var watch = Stopwatch.StartNew();
        await LoadCompletionWait.WaitUntilCompletedCountAsync(
            host.Query,
            flow.Name,
            backlog,
            LoadLimits.DrainTimeout);
        watch.Stop();

        // Assert
        var oldFloorPerSec = batchSize / referencePoll.TotalSeconds;
        var actualPerSec = backlog / Math.Max(watch.Elapsed.TotalSeconds, 0.001);
        Assert.True(
            actualPerSec > oldFloorPerSec * 2,
            $"Expected drain rate well above old floor ({oldFloorPerSec:F1}/s); got {actualPerSec:F1}/s for {backlog} rows in {watch.Elapsed}.");
        Assert.Equal(backlog, executeCount);
    }
}

[Collection(SqlServerLoadCollection.Name)]
public sealed class SqlServerDrainScenarios : DrainScenarios<SqlServerDatabaseFixture>
{
    public SqlServerDrainScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresLoadCollection.Name)]
public sealed class PostgresDrainScenarios : DrainScenarios<PostgresDatabaseFixture>
{
    public PostgresDrainScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
