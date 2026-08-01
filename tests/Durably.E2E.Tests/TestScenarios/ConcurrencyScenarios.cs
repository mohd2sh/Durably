using Durably.E2E.Tests.Flows;
using Durably.E2E.Tests.Models;
using Xunit;

namespace Durably.E2E.Tests.TestScenarios;

public abstract class ConcurrencyScenarios<TFixture> : ScenarioTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected ConcurrencyScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Optimistic_concurrency_conflicts_across_database_connections()
    {
        await ResetAsync();
        var store = NewExecutionStore();
        var now = DateTimeOffset.UtcNow;

        var runId = Guid.NewGuid().ToString("N");
        await store.CreateAsync(new ExecutionRecord
        {
            FlowName = "concurrency",
            RunId = runId,
            InstanceId = "one",
            Status = ExecutionStatus.Running,
            CurrentStep = 0,
            ContextJson = "{}",
            Attempts = 0,
            Version = 0,
            CreatedAt = now,
            UpdatedAt = now
        }, default);

        var leaseUntil = DateTimeOffset.UtcNow.Add(TestLimits.DefaultLeaseDuration);
        const string runner = "e2e-runner";
        Assert.True(await store.TryAcquireLeaseAsync("concurrency", runId, runner, leaseUntil, CancellationToken.None));

        var runnerA = await store.LoadAsync("concurrency", runId, default);
        var runnerB = await store.LoadAsync("concurrency", runId, default);

        runnerA!.CurrentStep = 1;
        await store.SaveCheckpointAsync(runnerA, runner, leaseUntil, default);

        runnerB!.CurrentStep = 2;
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            store.SaveCheckpointAsync(runnerB, runner, leaseUntil, default));
    }

    [Fact]
    public async Task Concurrent_StartAsync_same_instance_one_created_rest_conflict()
    {
        await ResetAsync();
        var flow = ScenarioFlows.CreateHappyPath();

        // Keep the open run Pending so losers resolve Conflict via FindOpen (not LoadLatest of a completed winner).
        await using var host = await StartHostAsync(
            d => d.AddFlow(flow),
            o => o.WorkerEnabled = false);

        var tasks = Enumerable.Range(0, 8)
            .Select(_ => host.Engine.StartAsync(flow, "same", new OrderState()))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        Assert.Equal(1, results.Count(r => r.Outcome == FlowStartOutcome.Created));
        Assert.Equal(7, results.Count(r => r.Outcome == FlowStartOutcome.Conflict));

        var open = await host.Store.FindOpenAsync(flow.Name, "same", CancellationToken.None);
        Assert.NotNull(open);
        Assert.Equal(ExecutionStatus.Pending, open!.Status);
    }

    [Fact]
    public async Task Second_lease_while_held_returns_AlreadyRunning_without_double_side_effects()
    {
        await ResetAsync();
        var runs = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var flow = Flow.For<LeaseConflictFlow, OrderState>()
            .Step("slow", async (_, ct) =>
            {
                Interlocked.Increment(ref runs);
                await gate.Task.WaitAsync(ct);
            });

        await using var host = await StartHostAsync(
            d => d.AddFlow(flow),
            o =>
            {
                o.WorkerEnabled = false;
                o.RunnerId = "primary";
            });

        var start = await host.Engine.StartAsync(flow, "lease-1", new OrderState());

        var leaseUntil = DateTimeOffset.UtcNow.Add(TestLimits.DefaultLeaseDuration);
        Assert.True(await host.Store.TryAcquireLeaseAsync(flow.Name, start.RunId, "primary", leaseUntil, CancellationToken.None));
        var record = await host.Store.LoadAsync(flow.Name, start.RunId, CancellationToken.None);
        var processing = host.Processor.ProcessAsync(record!, "primary", TestLimits.DefaultLeaseDuration);

        await Task.Delay(TestLimits.BriefDelay);
        Assert.False(await host.Store.TryAcquireLeaseAsync(
            flow.Name, start.RunId, "contender", DateTimeOffset.UtcNow.Add(TestLimits.DefaultLeaseDuration), CancellationToken.None));

        gate.TrySetResult();
        var result = await processing;
        Assert.Equal(FlowStatus.Completed, result.Status);
        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task Two_workers_complete_each_instance_exactly_once()
    {
        // Arrange
        await ResetAsync();
        const int backlog = 12;
        var executeCount = 0;
        var flow = Flow.For<DualWorkerFlow, OrderState>()
            .Step("work", (_, _) =>
            {
                Interlocked.Increment(ref executeCount);
                return Task.CompletedTask;
            });

        await using var hostA = await StartHostAsync(
            d => d.AddFlow(flow),
            o =>
            {
                o.WorkerEnabled = true;
                o.BatchSize = 8;
                o.MaxDegreeOfParallelism = 4;
                o.PollInterval = TestLimits.DefaultPollInterval;
                o.LeaseDuration = TimeSpan.FromMinutes(1);
                o.RunnerId = "e2e-worker-a";
            });
        await using var hostB = await StartHostAsync(
            d => d.AddFlow(flow),
            o =>
            {
                o.WorkerEnabled = true;
                o.BatchSize = 8;
                o.MaxDegreeOfParallelism = 4;
                o.PollInterval = TestLimits.DefaultPollInterval;
                o.LeaseDuration = TimeSpan.FromMinutes(1);
                o.RunnerId = "e2e-worker-b";
            });

        // Act
        for (var i = 0; i < backlog; i++)
        {
            await hostA.Engine.StartAsync(flow, $"dual-{i}", new OrderState());
        }

        await hostA.WaitForCompletedCountAsync(flow.Name, backlog, "dual-", TestLimits.DefaultWaitTimeout);

        // Assert
        Assert.Equal(backlog, executeCount);
    }

    private sealed class LeaseConflictFlow;

    private sealed class DualWorkerFlow;
}

[Collection(SqlServerE2ECollection.Name)]
public sealed class SqlServerConcurrencyScenarios : ConcurrencyScenarios<SqlServerDatabaseFixture>
{
    public SqlServerConcurrencyScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresE2ECollection.Name)]
public sealed class PostgresConcurrencyScenarios : ConcurrencyScenarios<PostgresDatabaseFixture>
{
    public PostgresConcurrencyScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
