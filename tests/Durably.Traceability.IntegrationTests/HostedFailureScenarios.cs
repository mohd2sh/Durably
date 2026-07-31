using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Durably.Traceability.IntegrationTests;

public sealed class HostedFailureScenarios
{
    [Fact]
    public async Task Intermittent_store_failure_does_not_stop_later_flushes()
    {
        // Arrange
        const string flow = "fail-flow";
        const string instance = "fail-1";
        const string lostStep = "lost";
        const string keptStep = "kept";
        const int failCount = 1;
        const int batchSize = 10;
        var recording = new RecordingTraceStore();
        var failing = new FailThenSucceedTraceStore(recording, failCount);
        var lostRecord = TraceHostFactory.Sample(flow, instance, lostStep);
        var keptRecord = TraceHostFactory.Sample(flow, instance, keptStep);
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                var durably = services.AddDurably()
                    .UseInMemoryStore()
                    .ConfigureWorker(o => o.Enabled = false);
                services.RemoveAll<ITraceStore>();
                services.AddSingleton<ITraceStore>(_ => failing);
                durably.AddTraceability(o =>
                {
                    o.FlushInterval = TestLimits.ShortFlush;
                    o.BatchSize = batchSize;
                });
            })
            .Build();
        await host.StartAsync();
        var sink = host.Services.GetRequiredService<ITraceSink>();

        try
        {
            // Act
            sink.Emit(lostRecord);
            await TraceHostFactory.WaitUntilAsync(
                () => Task.FromResult(failing.AppendAttempts >= 1),
                TestLimits.DefaultWait);
            sink.Emit(keptRecord);
            await TraceHostFactory.WaitUntilAsync(
                async () => (await recording.LoadAsync(flow, instance, CancellationToken.None)).Count >= 1,
                TestLimits.DefaultWait);
            var traces = await recording.LoadAsync(flow, instance, CancellationToken.None);

            // Assert
            Assert.Single(traces);
            Assert.Contains(traces, t => t.StepKey == keptStep);
            Assert.DoesNotContain(traces, t => t.StepKey == lostStep);
        }
        finally
        {
            await host.StopAsync();
        }
    }
}

/// <summary>Captures successful appends for integration assertions.</summary>
internal sealed class RecordingTraceStore : ITraceStore
{
    private readonly object _gate = new();
    private readonly List<TraceRecord> _all = new();

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _all.Count;
            }
        }
    }

    public Task AppendAsync(IReadOnlyList<TraceRecord> records, CancellationToken cancellationToken)
    {
        if (records is null || records.Count == 0)
        {
            return Task.CompletedTask;
        }

        lock (_gate)
        {
            foreach (var record in records)
            {
                _all.Add(Clone(record));
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TraceRecord>> LoadAsync(string flowName, string instanceId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var matches = _all
                .Where(r => r.FlowName == flowName && r.InstanceId == instanceId)
                .Select(Clone)
                .ToList();
            return Task.FromResult<IReadOnlyList<TraceRecord>>(matches);
        }
    }

    private static TraceRecord Clone(TraceRecord source) => new()
    {
        FlowName = source.FlowName,
        InstanceId = source.InstanceId,
        StepKey = source.StepKey,
        Attempt = source.Attempt,
        Outcome = source.Outcome,
        InputJson = source.InputJson,
        OutputJson = source.OutputJson,
        DurationMs = source.DurationMs,
        ExceptionMessage = source.ExceptionMessage,
        Timestamp = source.Timestamp
    };
}
