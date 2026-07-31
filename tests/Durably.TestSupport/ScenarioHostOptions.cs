using Microsoft.Extensions.DependencyInjection;

namespace Durably.TestSupport;

public sealed class ScenarioHostOptions
{
    public bool WorkerEnabled { get; set; } = true;

    public TimeSpan PollInterval { get; set; } = TestLimits.DefaultPollInterval;

    public int BatchSize { get; set; } = 16;

    public int MaxDegreeOfParallelism { get; set; } = 4;

    public TimeSpan LeaseDuration { get; set; } = TestLimits.DefaultWaitTimeout;

    public string? RunnerId { get; set; }

    public bool EnableTraceability { get; set; }

    public TimeSpan TraceFlushInterval { get; set; } = TestLimits.TinyDelay;

    public Action<IServiceCollection>? ConfigureServices { get; set; }
}
