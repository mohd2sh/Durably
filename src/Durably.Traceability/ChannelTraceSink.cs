using System.Threading.Channels;

namespace Durably;

/// <summary>
/// Non-blocking <see cref="ITraceSink"/> backed by a bounded <see cref="Channel{T}"/>. The background
/// writer drains this channel and persists via <see cref="ITraceStore"/>.
/// </summary>
internal sealed class ChannelTraceSink : ITraceSink
{
    private readonly Channel<TraceRecord> _channel;
    private readonly TraceabilityOptions _options;
    private readonly ITraceRedactor? _redactor;

    public ChannelTraceSink(Channel<TraceRecord> channel, TraceabilityOptions options, ITraceRedactor? redactor = null)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _redactor = redactor;
    }

    internal ChannelReader<TraceRecord> Reader => _channel.Reader;

    public void Emit(TraceRecord record)
    {
        if (record is null)
        {
            return;
        }

        if (!_options.CaptureInputOutput)
        {
            record.InputJson = null;
            record.OutputJson = null;
        }

        if (!_options.CaptureExceptions)
        {
            record.ExceptionMessage = null;
        }

        _redactor?.Redact(record);

        _channel.Writer.TryWrite(record);
    }
}
