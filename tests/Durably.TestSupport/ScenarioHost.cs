using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Durably.TestSupport;

/// <summary>
/// Hosted Durably stack for integration / E2E / load scenarios: real provider store,
/// optional worker, optional traceability.
/// </summary>
public sealed class ScenarioHost : IAsyncDisposable
{
    private readonly IHost _host;

    private ScenarioHost(IHost host)
    {
        _host = host;
    }

    public IServiceProvider Services => _host.Services;

    public IFlowEngine Engine => Services.GetRequiredService<IFlowEngine>();

    public IExecutionStore Store => Services.GetRequiredService<IExecutionStore>();

    public IExecutionQuery Query => Services.GetRequiredService<IExecutionQuery>();

    internal ExecutionProcessor Processor => Services.GetRequiredService<ExecutionProcessor>();

    public IStateSerializer Serializer => Services.GetRequiredService<IStateSerializer>();

    public ITraceStore? TraceStore => Services.GetService<ITraceStore>();

    public static async Task<ScenarioHost> StartAsync(
        IDatabaseFixture database,
        Action<IDurablyBuilder> configure,
        Action<ScenarioHostOptions>? configureOptions = null)
    {
        if (database is null)
        {
            throw new ArgumentNullException(nameof(database));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var options = new ScenarioHostOptions();
        configureOptions?.Invoke(options);

        var builder = Host.CreateApplicationBuilder();
        var durably = database.ConfigureDurably(builder.Services.AddDurably(), o => o.AutoMigrate = false)
            .ConfigureWorker(o =>
            {
                o.Enabled = options.WorkerEnabled;
                o.PollInterval = options.PollInterval;
                o.BatchSize = options.BatchSize;
                o.MaxDegreeOfParallelism = options.MaxDegreeOfParallelism;
                o.LeaseDuration = options.LeaseDuration;
                o.RunnerId = options.RunnerId;
            });

        configure(durably);

        if (options.EnableTraceability)
        {
            durably.AddTraceability(o =>
            {
                o.FlushInterval = options.TraceFlushInterval;
                o.BatchSize = 10;
            });
        }

        options.ConfigureServices?.Invoke(builder.Services);

        var host = builder.Build();
        await host.StartAsync();
        return new ScenarioHost(host);
    }

    public Task<TState> LoadStateAsync<TState>(string flowName, string instanceId)
        where TState : class, new()
    {
        return ScenarioWait.LoadStateAsync<TState>(Store, Serializer, flowName, instanceId);
    }

    public Task<ExecutionStatusInfo> WaitForStatusAsync(
        string flowName,
        string instanceId,
        ExecutionStatus status,
        TimeSpan? timeout = null)
        => ScenarioWait.WaitForStatusAsync(Engine, flowName, instanceId, status, timeout);

    public Task WaitForCompletedCountAsync(
        string flowName,
        int expected,
        string instancePrefix,
        TimeSpan timeout)
        => ScenarioWait.WaitForCompletedCountAsync(Engine, flowName, expected, instancePrefix, timeout);

    public Task<FlowRunResult> ResumeFailedAsync(string flowName, string instanceId)
        => ExecutionResume.ResumeFailedAsync(Store, Processor, flowName, instanceId);

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}
