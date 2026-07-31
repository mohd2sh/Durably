using Durably.E2E.Tests.Flows;
using Durably.E2E.Tests.Models;
using Xunit;

namespace Durably.E2E.Tests.TestScenarios;

public abstract class FailureScenarios<TFixture> : ScenarioTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected FailureScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Killer_resume_does_not_rerun_completed_steps()
    {
        await ResetAsync();
        var (flow, counters) = ScenarioFlows.CreateFailOnceEmail();

        await using var host = await StartHostAsync(d => d.AddFlow(flow));

        await host.Engine.StartAsync(flow, "order-1", new OrderState());
        await host.WaitForStatusAsync(flow.Name, "order-1", ExecutionStatus.Failed);

        Assert.Equal(1, counters.Generate);
        Assert.Equal(1, counters.Email);
        Assert.Equal(0, counters.Finalize);

        var status = await host.Engine.GetStatusAsync(flow.Name, "order-1");
        Assert.Equal("email", status!.FailedStep);

        var resumed = await host.ResumeFailedAsync(flow.Name, "order-1");
        Assert.Equal(FlowStatus.Completed, resumed.Status);
        Assert.Equal(1, counters.Generate);
        Assert.Equal(2, counters.Email);
        Assert.Equal(1, counters.Finalize);
    }

    [Fact]
    public async Task Transient_retry_succeeds_without_terminal_failure()
    {
        await ResetAsync();
        var (flow, counters) = ScenarioFlows.CreateTransientRetry();

        await using var host = await StartHostAsync(d => d.AddFlow(flow));

        await host.Engine.StartAsync(flow, "retry-ok", new OrderState());
        await host.WaitForStatusAsync(flow.Name, "retry-ok", ExecutionStatus.Completed);

        Assert.Equal(3, counters.Flaky);
        var status = await host.Engine.GetStatusAsync(flow.Name, "retry-ok");
        Assert.Null(status!.FailedStep);
    }

    [Fact]
    public async Task Retry_exhaustion_fails_then_explicit_resume_completes()
    {
        await ResetAsync();
        var fail = true;
        var attempts = 0;
        var flow = Flow.For<RetryExhaustResumeFlow, OrderState>()
            .Step("flaky", (_, _) =>
            {
                Interlocked.Increment(ref attempts);
                if (fail)
                {
                    throw new InvalidOperationException("nope");
                }

                return Task.CompletedTask;
            }, o => o.Retry(RetryPolicy.Fixed(2, TimeSpan.Zero)));

        await using var host = await StartHostAsync(d => d.AddFlow(flow));

        await host.Engine.StartAsync(flow, "retry-fail", new OrderState());
        await host.WaitForStatusAsync(flow.Name, "retry-fail", ExecutionStatus.Failed);

        Assert.Equal(2, attempts);
        var status = await host.Engine.GetStatusAsync(flow.Name, "retry-fail");
        Assert.Equal("flaky", status!.FailedStep);

        fail = false;
        var resumed = await host.ResumeFailedAsync(flow.Name, "retry-fail");
        Assert.Equal(FlowStatus.Completed, resumed.Status);
        Assert.Equal(3, attempts);
    }

    private sealed class RetryExhaustResumeFlow;

    [Fact]
    public async Task Crash_mid_flow_new_worker_reclaims_running_without_rerunning_generate()
    {
        await ResetAsync();
        var (flow, counters, gate) = ScenarioFlows.CreateBlockingAfterGenerate();

        await using (var hostA = await StartHostAsync(
            d => d.AddFlow(flow),
            o => o.LeaseDuration = TestLimits.ShortLeaseDuration))
        {
            await hostA.Engine.StartAsync(flow, "crash-1", new OrderState());
            await hostA.WaitForStatusAsync(flow.Name, "crash-1", ExecutionStatus.Running);

            var deadline = DateTime.UtcNow + TestLimits.TraceWaitTimeout;
            while (Volatile.Read(ref counters.Blocking) == 0 && DateTime.UtcNow < deadline)
            {
                await Task.Delay(TestLimits.TinyDelay);
            }

            Assert.Equal(1, counters.Generate);
            Assert.True(counters.Blocking >= 1);
        }

        // Host A disposed: lease released, status remains Running at the blocking step.
        gate.TrySetResult();
        await using var hostB = await StartHostAsync(d => d.AddFlow(flow));
        await hostB.WaitForStatusAsync(flow.Name, "crash-1", ExecutionStatus.Completed, TestLimits.DefaultWaitTimeout);

        Assert.Equal(1, counters.Generate);
        Assert.Equal(1, counters.Finalize);
    }
}

[Collection(SqlServerE2ECollection.Name)]
public sealed class SqlServerFailureScenarios : FailureScenarios<SqlServerDatabaseFixture>
{
    public SqlServerFailureScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresE2ECollection.Name)]
public sealed class PostgresFailureScenarios : FailureScenarios<PostgresDatabaseFixture>
{
    public PostgresFailureScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
