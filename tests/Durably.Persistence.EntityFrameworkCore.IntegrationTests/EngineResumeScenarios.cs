using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Durably.Persistence.EntityFrameworkCore.IntegrationTests;

/// <summary>Short top-level marker so FlowName stays within the 200-char column limit.</summary>
internal sealed class FailOnceEmailFlow;

public sealed class ResumeState
{
    public string? Report { get; set; }
    public bool EmailSent { get; set; }
    public bool Finalized { get; set; }
}

public abstract class EngineResumeScenarios<TFixture> : ProviderTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    private const string InstanceId = "resume-1";
    private const string GenerateStep = "generate";
    private const string EmailStep = "email";
    private const string FinalizeStep = "finalize";
    private const string SmtpErrorMessage = "smtp down";

    protected EngineResumeScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Engine_resumes_from_failed_step_without_rerunning_prior_steps()
    {
        // Arrange
        await ResetAsync();
        var counters = new StepCounters();
        var flow = CreateFailOnceEmailFlow(counters);

        await using var services = CreateProviderWithFlow(flow);
        var engine = services.GetRequiredService<IFlowEngine>();
        var processor = services.GetRequiredService<ExecutionProcessor>();
        var store = services.GetRequiredService<IExecutionStore>();

        // Act + Assert — intermediate counters must be checked before resume
        await engine.StartAsync(flow, InstanceId, new ResumeState());
        var first = await ExecutionResume.ProcessAsync(store, processor, flow.Name, InstanceId);

        Assert.Equal(FlowStatus.Failed, first.Status);
        Assert.Equal(EmailStep, first.FailedStep);
        Assert.Equal(1, counters.Generate);
        Assert.Equal(1, counters.Email);
        Assert.Equal(0, counters.Finalize);

        var second = await ExecutionResume.ProcessAsync(store, processor, flow.Name, InstanceId);

        Assert.Equal(FlowStatus.Completed, second.Status);
        Assert.Equal(1, counters.Generate);
        Assert.Equal(2, counters.Email);
        Assert.Equal(1, counters.Finalize);
    }

    private ServiceProvider CreateProviderWithFlow(IFlowBuilder<ResumeState> flow)
    {
        var services = new ServiceCollection();
        Database.ConfigureDurably(services.AddDurably(), o => o.AutoMigrate = false)
            .ConfigureWorker(o => o.Enabled = false)
            .AddFlow(flow);
        return services.BuildServiceProvider();
    }

    private static IFlowBuilder<ResumeState> CreateFailOnceEmailFlow(StepCounters counters)
    {
        return Flow.For<FailOnceEmailFlow, ResumeState>()
            .Step(GenerateStep, (s, _) =>
            {
                Interlocked.Increment(ref counters.Generate);
                s.Report = "report";
                return Task.CompletedTask;
            })
            .Step(EmailStep, (s, _) =>
            {
                Interlocked.Increment(ref counters.Email);
                if (counters.FailOnce)
                {
                    counters.FailOnce = false;
                    throw new InvalidOperationException(SmtpErrorMessage);
                }

                s.EmailSent = true;
                return Task.CompletedTask;
            })
            .Step(FinalizeStep, (s, _) =>
            {
                Interlocked.Increment(ref counters.Finalize);
                s.Finalized = true;
                return Task.CompletedTask;
            });
    }

    private sealed class StepCounters
    {
        public int Generate;
        public int Email;
        public int Finalize;
        public bool FailOnce = true;
    }
}

[Collection(SqlServerEfIntegrationCollection.Name)]
public sealed class SqlServerEngineResumeScenarios : EngineResumeScenarios<SqlServerDatabaseFixture>
{
    public SqlServerEngineResumeScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresEfIntegrationCollection.Name)]
public sealed class PostgresEngineResumeScenarios : EngineResumeScenarios<PostgresDatabaseFixture>
{
    public PostgresEngineResumeScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
