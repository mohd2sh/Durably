using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Durably.Traceability.IntegrationTests.Infrastructure;

internal static class TraceHostFactory
{
    public static IHost Create(
        Action<TraceabilityOptions>? configureTraceability = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                var durably = services.AddDurably().UseInMemoryStore();
                durably.AddTraceability(o =>
                {
                    o.FlushInterval = TestLimits.ShortFlush;
                    o.BatchSize = TestLimits.DefaultBatchSize;
                    configureTraceability?.Invoke(o);
                });
                configureServices?.Invoke(services);
            });

        return builder.Build();
    }

    public static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition().ConfigureAwait(false))
            {
                return;
            }

            await Task.Delay(15).ConfigureAwait(false);
        }

        throw new TimeoutException($"Condition not met within {timeout}.");
    }

    public static TraceRecord Sample(string flow, string instance, string step) => new()
    {
        FlowName = flow,
        RunId = instance,
        InstanceId = instance,
        StepKey = step,
        Attempt = 1,
        Outcome = TraceOutcome.Succeeded,
        InputJson = "{}",
        Timestamp = DateTimeOffset.UtcNow
    };
}

/// <summary>Fails the first N appends, then delegates to an inner store.</summary>
internal sealed class FailThenSucceedTraceStore : ITraceStore
{
    private readonly ITraceStore _inner;
    private int _remainingFailures;
    private int _appendAttempts;

    public FailThenSucceedTraceStore(ITraceStore inner, int failCount)
    {
        _inner = inner;
        _remainingFailures = failCount;
    }

    public int AppendAttempts => Volatile.Read(ref _appendAttempts);

    public Task AppendAsync(IReadOnlyList<TraceRecord> records, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _appendAttempts);
        if (Interlocked.Decrement(ref _remainingFailures) >= 0)
        {
            throw new InvalidOperationException("simulated intermittent trace store failure");
        }

        return _inner.AppendAsync(records, cancellationToken);
    }

    public Task<IReadOnlyList<TraceRecord>> LoadAsync(string flowName, string runId, CancellationToken cancellationToken)
        => _inner.LoadAsync(flowName, runId, cancellationToken);
}
