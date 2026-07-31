using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Durably.Traceability.IntegrationTests;

public sealed class HostedFlushScenarios
{
    [Fact]
    public async Task StopAsync_drains_pending_traces_into_store()
    {
        // Arrange
        const string flow = "flush-flow";
        const string instance = "flush-1";
        const int expectedCount = 3;
        const string stepA = "a";
        const string stepB = "b";
        const string stepC = "c";
        using var host = TraceHostFactory.Create(o =>
        {
            o.FlushInterval = TimeSpan.FromHours(1);
            o.BatchSize = 50;
        });
        await host.StartAsync();
        var sink = host.Services.GetRequiredService<ITraceSink>();
        var store = host.Services.GetRequiredService<ITraceStore>();
        var recordA = TraceHostFactory.Sample(flow, instance, stepA);
        var recordB = TraceHostFactory.Sample(flow, instance, stepB);
        var recordC = TraceHostFactory.Sample(flow, instance, stepC);

        // Act
        sink.Emit(recordA);
        sink.Emit(recordB);
        sink.Emit(recordC);
        await host.StopAsync();
        var traces = await store.LoadAsync(flow, instance, CancellationToken.None);

        // Assert
        Assert.Equal(expectedCount, traces.Count);
        Assert.Contains(traces, t => t.StepKey == stepA);
        Assert.Contains(traces, t => t.StepKey == stepB);
        Assert.Contains(traces, t => t.StepKey == stepC);
    }

    [Fact]
    public async Task Writer_flushes_within_FlushInterval_when_batch_fills()
    {
        // Arrange
        const string flow = "batch-flow";
        const string instance = "batch-1";
        const int emitCount = 5;
        const int batchSize = 2;
        using var host = TraceHostFactory.Create(o =>
        {
            o.FlushInterval = TestLimits.ShortFlush;
            o.BatchSize = batchSize;
        });
        await host.StartAsync();
        var sink = host.Services.GetRequiredService<ITraceSink>();
        var store = host.Services.GetRequiredService<ITraceStore>();
        var records = Enumerable.Range(0, emitCount)
            .Select(i => TraceHostFactory.Sample(flow, instance, $"s{i}"))
            .ToList();

        try
        {
            // Act
            foreach (var record in records)
            {
                sink.Emit(record);
            }

            await TraceHostFactory.WaitUntilAsync(
                async () => (await store.LoadAsync(flow, instance, CancellationToken.None)).Count >= emitCount,
                TestLimits.DefaultWait);
            var traces = await store.LoadAsync(flow, instance, CancellationToken.None);

            // Assert
            Assert.Equal(emitCount, traces.Count);
        }
        finally
        {
            await host.StopAsync();
        }
    }
}
