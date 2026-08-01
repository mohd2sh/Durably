using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Durably.Extensions.DependencyInjection.UnitTests;

public sealed class AddDurablyTests
{
    private const string InstanceId = "di-1";
    private const int BriefPollMilliseconds = 100;
    private const int ShortLeaseSeconds = 15;
    private const int ClaimBatchSize = 50;

    [Fact]
    public void AddDurably_registers_engine_services_without_a_store()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDurably();

        // Act
        using var provider = services.BuildServiceProvider();

        // Assert — store/query come from a persistence provider; engine types that need
        // IExecutionStore are registered but not resolvable until UseInMemoryStore / EF / Dapper.
        Assert.Null(provider.GetService<IExecutionStore>());
        Assert.IsType<ExecutionWorkSignal>(provider.GetRequiredService<IExecutionWorkSignal>());
        Assert.NotNull(provider.GetRequiredService<IStateSerializer>());
        Assert.NotNull(provider.GetRequiredService<IFlowRegistry>());
        Assert.Contains(services, d => d.ServiceType == typeof(IFlowEngine));
        Assert.Contains(services, d => d.ServiceType == typeof(ExecutionProcessor));
    }

    [Fact]
    public void UseInMemoryStore_registers_InMemoryExecutionStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDurably().UseInMemoryStore();

        // Act
        using var provider = services.BuildServiceProvider();

        // Assert
        Assert.IsType<InMemoryExecutionStore>(provider.GetRequiredService<IExecutionStore>());
        Assert.NotNull(provider.GetRequiredService<IExecutionQuery>());
        Assert.NotNull(provider.GetRequiredService<ITraceStore>());
    }

    [Fact]
    public async Task AddFlow_registers_flow_that_processor_can_run()
    {
        // Arrange
        var services = new ServiceCollection();
        var flow = Flow.For<DiHappyFlow, DiState>()
            .Step("set", (s, _) =>
            {
                s.Value = "ok";
                return Task.CompletedTask;
            });
        services.AddDurably().UseInMemoryStore().AddFlow(flow);
        await using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IFlowEngine>();
        var processor = provider.GetRequiredService<ExecutionProcessor>();
        var store = provider.GetRequiredService<IExecutionStore>();

        // Act
        await engine.StartAsync(flow, InstanceId, new DiState());
        var result = await ProcessOnceAsync(store, processor, flow.Name, InstanceId);

        // Assert
        Assert.Equal(FlowStatus.Completed, result.Status);
    }

    [Fact]
    public async Task Unregistered_flow_quarantines_as_Failed_when_processed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDurably().UseInMemoryStore();
        await using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IFlowEngine>();
        var processor = provider.GetRequiredService<ExecutionProcessor>();
        var store = provider.GetRequiredService<IExecutionStore>();
        var flow = Flow.For<UnregisteredDiFlow, DiState>()
            .Step("noop", (_, _) => Task.CompletedTask);

        await engine.StartAsync(flow, InstanceId, new DiState());

        // Act
        var result = await ProcessOnceAsync(store, processor, flow.Name, InstanceId);

        // Assert
        Assert.Equal(FlowStatus.Failed, result.Status);
        Assert.Contains("not registered", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
        var record = await store.LoadLatestAsync(flow.Name, InstanceId, CancellationToken.None);
        Assert.Equal(ExecutionStatus.Failed, record!.Status);
    }

    [Fact]
    public void ConfigureWorker_applies_options()
    {
        // Arrange
        var expectedPoll = TimeSpan.FromMilliseconds(BriefPollMilliseconds);
        var expectedLease = TimeSpan.FromSeconds(ShortLeaseSeconds);
        const int expectedBatchSize = 8;
        const string expectedRunnerId = "unit-runner";
        var services = new ServiceCollection();
        services.AddDurably()
            .ConfigureWorker(o =>
            {
                o.Enabled = false;
                o.PollInterval = expectedPoll;
                o.BatchSize = expectedBatchSize;
                o.LeaseDuration = expectedLease;
                o.RunnerId = expectedRunnerId;
            });

        // Act
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<DurablyWorkerOptions>();

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal(expectedPoll, options.PollInterval);
        Assert.Equal(expectedBatchSize, options.BatchSize);
        Assert.Equal(expectedLease, options.LeaseDuration);
        Assert.Equal(expectedRunnerId, options.RunnerId);
    }

    [Fact]
    public async Task DurablyOptions_DefaultRetry_applies_when_step_does_not_override()
    {
        // Arrange — DefaultRetry is applied when IFlow.Build runs during Materialize.
        const int expectedAttempts = 3;
        GlobalRetryProbe.Reset();
        GlobalRetryProbe.FailUntilAttempt = expectedAttempts;

        var services = new ServiceCollection();
        services.AddDurably(o => o.DefaultRetry = RetryPolicy.Fixed(expectedAttempts, TimeSpan.Zero))
            .UseInMemoryStore()
            .AddFlow<GlobalRetryProbeFlow, DiState>();
        await using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IFlowEngine>();
        var processor = provider.GetRequiredService<ExecutionProcessor>();
        var store = provider.GetRequiredService<IExecutionStore>();
        var flowName = typeof(GlobalRetryProbeFlow).FullName!;

        // Act
        await engine.StartAsync<GlobalRetryProbeFlow, DiState>("retry-1", new DiState());
        var result = await ProcessOnceAsync(store, processor, flowName, "retry-1");

        // Assert
        Assert.Equal(FlowStatus.Completed, result.Status);
        Assert.Equal(expectedAttempts, GlobalRetryProbe.Attempts);
    }

    [Fact]
    public void AddFlowsFromAssembly_registers_marker_flow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDurably().AddFlowsFromAssembly(typeof(ScannedSampleFlow).Assembly);

        // Act
        using var provider = services.BuildServiceProvider();
        var registrations = provider.GetServices<IFlowRegistration>().ToList();

        // Assert
        Assert.Contains(registrations, r => r.Name == typeof(ScannedSampleFlow).FullName);
    }

    private static async Task<FlowRunResult> ProcessOnceAsync(
        IExecutionStore store,
        ExecutionProcessor processor,
        string flowName,
        string instanceId)
    {
        const string runnerId = "di-unit-runner";
        var leaseDuration = TimeSpan.FromMinutes(1);
        var leaseUntil = DateTimeOffset.UtcNow.Add(leaseDuration);

        var claimed = await store.ClaimDueAsync(runnerId, leaseUntil, ClaimBatchSize, CancellationToken.None);
        var match = claimed.FirstOrDefault(r => r.FlowName == flowName && r.InstanceId == instanceId);
        if (match is not null)
        {
            return await processor.ProcessAsync(match, runnerId, leaseDuration);
        }

        var current = await store.LoadLatestAsync(flowName, instanceId, CancellationToken.None);
        Assert.NotNull(current);
        Assert.True(await store.TryAcquireLeaseAsync(flowName, current!.RunId, runnerId, leaseUntil, CancellationToken.None));
        var leased = await store.LoadAsync(flowName, current.RunId, CancellationToken.None);
        return await processor.ProcessAsync(leased!, runnerId, leaseDuration);
    }

    private sealed class DiHappyFlow;
    private sealed class UnregisteredDiFlow;

    public sealed class DiState
    {
        public string? Value { get; set; }
    }

    public sealed class ScannedSampleFlow : IFlow<DiState>
    {
        public void Build(IFlowBuilder<DiState> builder) =>
            builder.Step("noop", (_, _) => Task.CompletedTask);
    }

    public static class GlobalRetryProbe
    {
        public static int Attempts { get; set; }
        public static int FailUntilAttempt { get; set; } = 3;

        public static void Reset()
        {
            Attempts = 0;
            FailUntilAttempt = 3;
        }
    }

    public sealed class GlobalRetryProbeFlow : IFlow<DiState>
    {
        public void Build(IFlowBuilder<DiState> builder) =>
            builder.Step("flaky", (_, _) =>
            {
                GlobalRetryProbe.Attempts++;
                if (GlobalRetryProbe.Attempts < GlobalRetryProbe.FailUntilAttempt)
                {
                    throw new InvalidOperationException("transient");
                }

                return Task.CompletedTask;
            });
    }
}
