using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Durably.Load.Tests;

public abstract class MultiWorkerScenarios<TFixture> : LoadTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected MultiWorkerScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Two_workers_drain_10k_exactly_once()
    {
        // Arrange
        await ResetAsync();
        const int backlog = LoadLimits.MultiWorkerBacklog;
        var executeCount = 0;
        var flow = LoadFlows.CreateCountingStep(() => Interlocked.Increment(ref executeCount));
        var contextJson = JsonSerializer.Serialize(new LoadState());

        await using var hostA = await StartHostAsync(
            d => d.AddFlow(flow),
            o =>
            {
                o.WorkerEnabled = true;
                o.BatchSize = LoadLimits.WorkerBatchSize;
                o.MaxDegreeOfParallelism = LoadLimits.WorkerMaxDegreeOfParallelism;
                o.PollInterval = LoadLimits.WorkerPollInterval;
                o.LeaseDuration = LoadLimits.LeaseDuration;
                o.RunnerId = "load-a";
            });
        await using var hostB = await StartHostAsync(
            d => d.AddFlow(flow),
            o =>
            {
                o.WorkerEnabled = true;
                o.BatchSize = LoadLimits.WorkerBatchSize;
                o.MaxDegreeOfParallelism = LoadLimits.WorkerMaxDegreeOfParallelism;
                o.PollInterval = LoadLimits.WorkerPollInterval;
                o.LeaseDuration = LoadLimits.LeaseDuration;
                o.RunnerId = "load-b";
            });

        await LoadBacklogSeeder.SeedPendingAsync(
            hostA.Store,
            flow.Name,
            "multi-",
            backlog,
            contextJson);

        // Act
        var watch = Stopwatch.StartNew();
        await LoadCompletionWait.WaitUntilCompletedCountAsync(
            hostA.Query,
            flow.Name,
            backlog,
            LoadLimits.MultiWorkerTimeout);
        watch.Stop();

        // Assert
        Assert.Equal(backlog, executeCount);
        Assert.True(
            watch.Elapsed < LoadLimits.MultiWorkerTimeout,
            $"10k dual-worker drain took {watch.Elapsed}.");
    }
}

[Collection(SqlServerLoadCollection.Name)]
public sealed class SqlServerMultiWorkerScenarios : MultiWorkerScenarios<SqlServerDatabaseFixture>
{
    public SqlServerMultiWorkerScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresLoadCollection.Name)]
public sealed class PostgresMultiWorkerScenarios : MultiWorkerScenarios<PostgresDatabaseFixture>
{
    public PostgresMultiWorkerScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
