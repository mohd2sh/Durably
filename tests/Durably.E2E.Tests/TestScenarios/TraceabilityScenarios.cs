using Durably.E2E.Tests.Flows;
using Durably.E2E.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Durably.E2E.Tests.TestScenarios;

public abstract class TraceabilityScenarios<TFixture> : ScenarioTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected TraceabilityScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Traceability_flushes_succeeded_steps_to_provider_trace_store()
    {
        await ResetAsync();
        var flow = ScenarioFlows.CreateHappyPath();

        await using var host = await StartHostAsync(
            d => d.AddFlow(flow),
            o => o.EnableTraceability = true);

        await host.Engine.StartAsync(flow, "trace-1", new OrderState());
        var status = await host.WaitForStatusAsync(flow.Name, "trace-1", ExecutionStatus.Completed);

        var traces = await ScenarioWait.WaitForTracesAsync(host.TraceStore!, flow.Name, status.RunId, minCount: 3);

        Assert.Contains(traces, t => t.StepKey == "generate" && t.Outcome == TraceOutcome.Succeeded);
        Assert.Contains(traces, t => t.StepKey == "email" && t.Outcome == TraceOutcome.Succeeded);
        Assert.Contains(traces, t => t.StepKey == "finalize" && t.Outcome == TraceOutcome.Succeeded);
        Assert.All(traces, t => Assert.False(string.IsNullOrWhiteSpace(t.InputJson)));
    }

    [Fact]
    public async Task Checkpoint_succeeds_even_when_trace_store_throws()
    {
        await ResetAsync();
        var flow = ScenarioFlows.CreateHappyPath();

        await using var host = await StartHostAsync(
            d => d.AddFlow(flow),
            o =>
            {
                o.EnableTraceability = true;
                o.ConfigureServices = services =>
                {
                    services.RemoveAll<ITraceStore>();
                    services.AddSingleton<ITraceStore, ThrowingTraceStore>();
                };
            });

        await host.Engine.StartAsync(flow, "trace-throw-1", new OrderState());
        await host.WaitForStatusAsync(flow.Name, "trace-throw-1", ExecutionStatus.Completed, TestLimits.MediumWaitTimeout);
        var status = await host.Engine.GetStatusAsync(flow.Name, "trace-throw-1");
        Assert.Equal(ExecutionStatus.Completed, status?.Status);
    }
}

[Collection(SqlServerE2ECollection.Name)]
public sealed class SqlServerTraceabilityScenarios : TraceabilityScenarios<SqlServerDatabaseFixture>
{
    public SqlServerTraceabilityScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresE2ECollection.Name)]
public sealed class PostgresTraceabilityScenarios : TraceabilityScenarios<PostgresDatabaseFixture>
{
    public PostgresTraceabilityScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
