using System.Diagnostics;
using System.Threading.Channels;
using Xunit;

namespace Durably.Traceability.UnitTests;

public sealed class TraceWriterServiceTests
{
    [Fact]
    public async Task Writer_flushes_in_batches_of_BatchSize()
    {
        // Arrange
        const int batchSize = 2;
        const int expectedCount = 3;
        var flushInterval = TimeSpan.FromMilliseconds(20);
        var waitTimeout = TimeSpan.FromSeconds(2);
        var store = new RecordingTraceStore();
        var (sink, writer) = CreateWriter(store, batchSize, flushInterval);
        var first = Sample("a");
        var second = Sample("b");
        var third = Sample("c");

        await writer.StartAsync(CancellationToken.None);
        try
        {
            // Act
            sink.Emit(first);
            sink.Emit(second);
            sink.Emit(third);
            await WaitUntilAsync(() => store.All.Count >= expectedCount, waitTimeout);
        }
        finally
        {
            await writer.StopAsync(CancellationToken.None);
        }

        // Assert
        Assert.Contains(store.Batches, b => b.Count == batchSize);
        Assert.Equal(expectedCount, store.All.Count);
    }

    [Fact]
    public async Task StopAsync_drains_remaining_without_waiting_full_flush_interval()
    {
        // Arrange
        const int batchSize = 50;
        const int expectedCount = 2;
        const string firstStep = "drain-1";
        const string secondStep = "drain-2";
        var flushInterval = TimeSpan.FromHours(1);
        var maxStopDuration = TimeSpan.FromSeconds(2);
        var store = new RecordingTraceStore();
        var (sink, writer) = CreateWriter(store, batchSize, flushInterval);
        var first = Sample(firstStep);
        var second = Sample(secondStep);

        await writer.StartAsync(CancellationToken.None);
        sink.Emit(first);
        sink.Emit(second);

        // Act
        var stopWatch = Stopwatch.StartNew();
        await writer.StopAsync(CancellationToken.None);
        stopWatch.Stop();

        // Assert
        Assert.True(stopWatch.Elapsed < maxStopDuration, $"Stop took too long: {stopWatch.Elapsed}");
        Assert.Equal(expectedCount, store.All.Count);
        Assert.Contains(store.All, r => r.StepKey == firstStep);
        Assert.Contains(store.All, r => r.StepKey == secondStep);
    }

    [Fact]
    public async Task Store_throw_discards_batch_and_writer_continues()
    {
        // Arrange
        const int batchSize = 10;
        const int failNextAppends = 1;
        const string failedStep = "fail-batch";
        const string okStep = "ok-batch";
        var flushInterval = TimeSpan.FromMilliseconds(20);
        var waitTimeout = TimeSpan.FromSeconds(2);
        var store = new RecordingTraceStore();
        store.FailNextAppends(failNextAppends);
        var (sink, writer) = CreateWriter(store, batchSize, flushInterval);
        var failedRecord = Sample(failedStep);
        var okRecord = Sample(okStep);

        await writer.StartAsync(CancellationToken.None);
        try
        {
            // Act
            sink.Emit(failedRecord);
            await WaitUntilAsync(() => store.AppendCallCount >= 1, waitTimeout);
            sink.Emit(okRecord);
            await WaitUntilAsync(() => store.All.Count >= 1, waitTimeout);
        }
        finally
        {
            await writer.StopAsync(CancellationToken.None);
        }

        // Assert
        Assert.DoesNotContain(store.All, r => r.StepKey == failedStep);
        Assert.Contains(store.All, r => r.StepKey == okStep);
    }

    [Fact]
    public async Task Cancel_during_idle_delay_exits_cleanly()
    {
        // Arrange
        const int batchSize = 10;
        var flushInterval = TimeSpan.FromSeconds(30);
        var idleEnterDelay = TimeSpan.FromMilliseconds(50);
        var store = new RecordingTraceStore();
        var (sink, writer) = CreateWriter(store, batchSize, flushInterval);

        await writer.StartAsync(CancellationToken.None);

        // Act — allow ExecuteAsync to enter WaitWhenIdleAsync, then stop
        await Task.Delay(idleEnterDelay);
        await writer.StopAsync(CancellationToken.None);

        // Assert
        Assert.Empty(store.All);
        Assert.NotNull(sink);
    }

    private static (ChannelTraceSink Sink, TraceWriterService Writer) CreateWriter(
        RecordingTraceStore store,
        int batchSize,
        TimeSpan flushInterval)
    {
        var options = new TraceabilityOptions
        {
            BatchSize = batchSize,
            FlushInterval = flushInterval,
            ChannelCapacity = 1_000,
            FullMode = BoundedChannelFullMode.DropWrite
        };
        var channel = Channel.CreateBounded<TraceRecord>(new BoundedChannelOptions(options.ChannelCapacity)
        {
            FullMode = options.FullMode,
            SingleReader = true,
            SingleWriter = false
        });
        var sink = new ChannelTraceSink(channel, options);
        var writer = new TraceWriterService(sink, store, options);
        return (sink, writer);
    }

    private static TraceRecord Sample(string step) => new()
    {
        FlowName = "flow",
        InstanceId = "i1",
        StepKey = step,
        Attempt = 1,
        Outcome = TraceOutcome.Succeeded,
        Timestamp = DateTimeOffset.UtcNow
    };

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.Fail($"Condition not met within {timeout}.");
    }
}
