using System.Threading.Channels;
using Xunit;

namespace Durably.Traceability.UnitTests;

public sealed class ChannelTraceSinkTests
{
    [Fact]
    public void Emit_null_is_noop()
    {
        // Arrange
        const int capacity = 8;
        var (sink, reader) = CreateSink(capacity);

        // Act
        sink.Emit(null!);

        // Assert
        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public void CaptureInputOutput_false_clears_payload_fields()
    {
        // Arrange
        const int capacity = 8;
        const string stepKey = "s1";
        const string inputJson = "{}";
        const string outputJson = "{}";
        var (sink, reader) = CreateSink(
            capacity,
            configure: o => o.CaptureInputOutput = false);
        var record = Sample(stepKey, inputJson, outputJson, exception: null);

        // Act
        sink.Emit(record);
        var enqueued = reader.TryRead(out var stored);

        // Assert
        Assert.True(enqueued);
        Assert.Null(stored!.InputJson);
        Assert.Null(stored.OutputJson);
    }

    [Fact]
    public void CaptureExceptions_false_clears_exception_message()
    {
        // Arrange
        const int capacity = 8;
        const string stepKey = "s1";
        const string exceptionMessage = "boom";
        var (sink, reader) = CreateSink(
            capacity,
            configure: o => o.CaptureExceptions = false);
        var record = Sample(stepKey, input: null, output: null, exceptionMessage);

        // Act
        sink.Emit(record);
        var enqueued = reader.TryRead(out var stored);

        // Assert
        Assert.True(enqueued);
        Assert.Null(stored!.ExceptionMessage);
    }

    [Fact]
    public void Redactor_runs_when_registered()
    {
        // Arrange
        const int capacity = 8;
        const string stepKey = "s1";
        const string inputJson = "{}";
        const string outputJson = "{}";
        const string expectedRedacted = "[redacted]";
        var redactor = new RecordingRedactor();
        var (sink, reader) = CreateSink(capacity, redactor: redactor);
        var record = Sample(stepKey, inputJson, outputJson, exception: null);

        // Act
        sink.Emit(record);
        var enqueued = reader.TryRead(out var stored);

        // Assert
        Assert.Equal(1, redactor.CallCount);
        Assert.True(enqueued);
        Assert.Equal(expectedRedacted, stored!.InputJson);
        Assert.Equal(expectedRedacted, stored.OutputJson);
    }

    [Fact]
    public void DropWrite_full_channel_drops_without_throwing()
    {
        // Arrange
        const int capacity = 1;
        const string keepStep = "keep";
        const string dropStep = "drop";
        var (sink, reader) = CreateSink(
            capacity,
            configure: o => o.FullMode = BoundedChannelFullMode.DropWrite);
        var keep = Sample(keepStep, input: null, output: null, exception: null);
        var drop = Sample(dropStep, input: null, output: null, exception: null);

        // Act
        sink.Emit(keep);
        sink.Emit(drop);
        var readKept = reader.TryRead(out var kept);
        var readMore = reader.TryRead(out _);

        // Assert
        Assert.True(readKept);
        Assert.Equal(keepStep, kept!.StepKey);
        Assert.False(readMore);
    }

    [Fact]
    public void FullMode_Wait_still_non_blocking_via_TryWrite()
    {
        // Arrange
        const int capacity = 1;
        const string firstStep = "first";
        const string secondStep = "second";
        var (sink, reader) = CreateSink(
            capacity,
            configure: o => o.FullMode = BoundedChannelFullMode.Wait);
        var first = Sample(firstStep, input: null, output: null, exception: null);
        var second = Sample(secondStep, input: null, output: null, exception: null);

        // Act — TryWrite returns immediately even when FullMode is Wait
        sink.Emit(first);
        sink.Emit(second);
        var readKept = reader.TryRead(out var kept);
        var readMore = reader.TryRead(out _);

        // Assert
        Assert.True(readKept);
        Assert.Equal(firstStep, kept!.StepKey);
        Assert.False(readMore);
    }

    private static (ChannelTraceSink Sink, ChannelReader<TraceRecord> Reader) CreateSink(
        int capacity,
        Action<TraceabilityOptions>? configure = null,
        ITraceRedactor? redactor = null)
    {
        var options = new TraceabilityOptions
        {
            ChannelCapacity = capacity,
            FullMode = BoundedChannelFullMode.DropWrite
        };
        configure?.Invoke(options);

        var channel = Channel.CreateBounded<TraceRecord>(new BoundedChannelOptions(options.ChannelCapacity)
        {
            FullMode = options.FullMode,
            SingleReader = true,
            SingleWriter = false
        });
        var sink = new ChannelTraceSink(channel, options, redactor);
        return (sink, sink.Reader);
    }

    private static TraceRecord Sample(string step, string? input, string? output, string? exception) => new()
    {
        FlowName = "flow",
        InstanceId = "i1",
        StepKey = step,
        Attempt = 1,
        Outcome = TraceOutcome.Succeeded,
        InputJson = input,
        OutputJson = output,
        ExceptionMessage = exception,
        Timestamp = DateTimeOffset.UtcNow
    };
}
