using Durably.E2E.Tests.Flows;
using Durably.E2E.Tests.Models;
using Xunit;

namespace Durably.E2E.Tests.TestScenarios;

public abstract class WorkerScenarios<TFixture> : ScenarioTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected WorkerScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Worker_processes_pending_without_manual_execution_processing()
    {
        await ResetAsync();
        var flow = ScenarioFlows.CreateHappyPath();

        await using var host = await StartHostAsync(
            d => d.AddFlow(flow),
            o => o.WorkerEnabled = true);

        await host.Engine.StartAsync(flow, "worker-1", new OrderState());
        await host.WaitForStatusAsync(flow.Name, "worker-1", ExecutionStatus.Completed, TestLimits.ShortWaitTimeout);
        var status = await host.Engine.GetStatusAsync(flow.Name, "worker-1");
        Assert.Equal(ExecutionStatus.Completed, status?.Status);
    }

    [Fact]
    public async Task Worker_batch_completes_multiple_instances()
    {
        await ResetAsync();
        var flow = ScenarioFlows.CreateHappyPath();

        await using var host = await StartHostAsync(
            d => d.AddFlow(flow),
            o =>
            {
                o.BatchSize = 8;
                o.MaxDegreeOfParallelism = 4;
                o.PollInterval = TestLimits.DefaultPollInterval;
            });

        const int count = 12;
        for (var i = 0; i < count; i++)
        {
            await host.Engine.StartAsync(flow, $"batch-{i}", new OrderState());
        }

        var deadline = DateTime.UtcNow + TestLimits.DefaultWaitTimeout + TestLimits.ShortWaitTimeout;
        while (DateTime.UtcNow < deadline)
        {
            var completed = 0;
            for (var i = 0; i < count; i++)
            {
                var status = await host.Engine.GetStatusAsync(flow.Name, $"batch-{i}");
                if (status?.Status == ExecutionStatus.Completed)
                {
                    completed++;
                }
            }

            if (completed == count)
            {
                Assert.Equal(count, completed);
                return;
            }

            await Task.Delay(TestLimits.BriefDelay);
        }

        throw new TimeoutException("Timed out waiting for batch instances to complete.");
    }

    [Fact]
    public async Task Expired_lease_on_running_is_reclaimed_by_new_runner()
    {
        await ResetAsync();
        var flow = ScenarioFlows.CreateHappyPath();
        var store = NewExecutionStore();
        var now = DateTimeOffset.UtcNow;

        await store.CreateAsync(new ExecutionRecord
        {
            FlowName = flow.Name,
            RunId = Guid.NewGuid().ToString("N"),
            InstanceId = "reclaim-1",
            Status = ExecutionStatus.Running,
            CurrentStep = 0,
            ContextJson = "{}",
            Attempts = 0,
            Version = 0,
            LockedBy = "dead-runner",
            LockedUntil = now.AddMinutes(-1),
            CreatedAt = now,
            UpdatedAt = now
        }, default);

        await using var host = await StartHostAsync(
            d => d.AddFlow(flow),
            o => o.RunnerId = "reclaimer");

        await host.WaitForStatusAsync(flow.Name, "reclaim-1", ExecutionStatus.Completed, TestLimits.MediumWaitTimeout);
        var status = await host.Engine.GetStatusAsync(flow.Name, "reclaim-1");
        Assert.Equal(ExecutionStatus.Completed, status?.Status);
    }

    [Fact]
    public async Task StartAsync_work_signal_wakes_worker_under_long_poll_interval()
    {
        await ResetAsync();
        var flow = ScenarioFlows.CreateHappyPath();

        await using var host = await StartHostAsync(
            d => d.AddFlow(flow),
            o =>
            {
                // Long poll; StartAsync must Notify so we complete well before PollInterval elapses.
                o.PollInterval = TestLimits.SlowPollInterval;
                o.WorkerEnabled = true;
            });

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await host.Engine.StartAsync(flow, "signal-1", new OrderState());
        await host.WaitForStatusAsync(flow.Name, "signal-1", ExecutionStatus.Completed, TestLimits.SignalWakeTimeout);
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TestLimits.SignalWakeMaxElapsed, $"Expected work-signal wake; elapsed {stopwatch.Elapsed}.");
    }
}

[Collection(SqlServerE2ECollection.Name)]
public sealed class SqlServerWorkerScenarios : WorkerScenarios<SqlServerDatabaseFixture>
{
    public SqlServerWorkerScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresE2ECollection.Name)]
public sealed class PostgresWorkerScenarios : WorkerScenarios<PostgresDatabaseFixture>
{
    public PostgresWorkerScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
