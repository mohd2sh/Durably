using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Durably.Traceability.IntegrationTests;

public sealed class HostedConcurrencyScenarios
{
    [Fact]
    public async Task Parallel_emits_do_not_throw_and_persist_up_to_capacity()
    {
        // Arrange
        const int emitCount = 200;
        const int channelCapacity = 10_000;
        const int batchSize = 25;
        const string flow = "parallel-flow";
        const string instance = "parallel-1";
        using var host = TraceHostFactory.Create(o =>
        {
            o.ChannelCapacity = channelCapacity;
            o.FlushInterval = TestLimits.ShortFlush;
            o.BatchSize = batchSize;
        });
        await host.StartAsync();
        var sink = host.Services.GetRequiredService<ITraceSink>();
        var store = host.Services.GetRequiredService<ITraceStore>();

        try
        {
            // Act
            await Task.WhenAll(Enumerable.Range(0, emitCount).Select(i => Task.Run(() =>
                sink.Emit(TraceHostFactory.Sample(flow, instance, $"p{i}")))));
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

    [Fact]
    public async Task Flood_with_tiny_capacity_drops_without_blocking_emit()
    {
        // Arrange
        const int emitCount = 200;
        const int batchSize = 2;
        const string flow = "drop-flow";
        const string instance = "drop-1";
        var flushInterval = TimeSpan.FromMilliseconds(200);
        var maxEmitDuration = TimeSpan.FromSeconds(1);
        using var host = TraceHostFactory.Create(o =>
        {
            o.ChannelCapacity = TestLimits.TinyCapacity;
            o.FullMode = BoundedChannelFullMode.DropWrite;
            o.FlushInterval = flushInterval;
            o.BatchSize = batchSize;
        });
        await host.StartAsync();
        var sink = host.Services.GetRequiredService<ITraceSink>();
        var store = host.Services.GetRequiredService<ITraceStore>();

        // Act
        var emitWatch = Stopwatch.StartNew();
        for (var i = 0; i < emitCount; i++)
        {
            sink.Emit(TraceHostFactory.Sample(flow, instance, $"d{i}"));
        }

        emitWatch.Stop();
        await host.StopAsync();
        var traces = await store.LoadAsync(flow, instance, CancellationToken.None);

        // Assert
        Assert.True(
            emitWatch.Elapsed < maxEmitDuration,
            $"Emit path blocked under DropWrite: {emitWatch.Elapsed}");
        Assert.True(traces.Count < emitCount, $"Expected drops; persisted {traces.Count} of {emitCount}");
        Assert.True(traces.Count > 0, "Expected some traces to persist.");
    }
}
