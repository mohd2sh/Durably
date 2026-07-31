using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Durably;

/// <summary>
/// Background worker that drains the trace channel and batch-writes to <see cref="ITraceStore"/> on a
/// dedicated connection. Trace write failures are logged and swallowed so observability never affects durability.
/// </summary>
internal sealed class TraceWriterService : BackgroundService
{
    private readonly ChannelTraceSink _sink;
    private readonly ITraceStore _store;
    private readonly TraceabilityOptions _options;
    private readonly ILogger _logger;

    public TraceWriterService(
        ChannelTraceSink sink,
        ITraceStore store,
        TraceabilityOptions options,
        ILogger<TraceWriterService>? logger = null)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<TraceWriterService>.Instance;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<TraceRecord>(_options.BatchSize);
        var reader = _sink.Reader;

        while (!stoppingToken.IsCancellationRequested)
        {
            batch.Clear();
            await ReadBatchAsync(reader, batch, _options.BatchSize, stoppingToken).ConfigureAwait(false);

            if (batch.Count > 0)
            {
                await WriteBatchSafeAsync(batch, stoppingToken).ConfigureAwait(false);
                continue;
            }

            if (!await WaitWhenIdleAsync(stoppingToken).ConfigureAwait(false))
            {
                break;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await DrainRemainingAsync(cancellationToken).ConfigureAwait(false);
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ReadBatchAsync(
        ChannelReader<TraceRecord> reader,
        List<TraceRecord> batch,
        int batchSize,
        CancellationToken stoppingToken)
    {
        try
        {
            if (!await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                return;
            }

            while (batch.Count < batchSize && reader.TryRead(out var item))
            {
                batch.Add(item);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown requested while waiting for channel data.
        }
    }

    private async Task<bool> WaitWhenIdleAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(_options.FlushInterval, stoppingToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private async Task DrainRemainingAsync(CancellationToken cancellationToken)
    {
        var batch = new List<TraceRecord>();
        var reader = _sink.Reader;
        while (reader.TryRead(out var item))
        {
            batch.Add(item);
        }

        if (batch.Count > 0)
        {
            await WriteBatchSafeAsync(batch, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WriteBatchSafeAsync(IReadOnlyList<TraceRecord> batch, CancellationToken cancellationToken)
    {
        try
        {
            await _store.AppendAsync(batch, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Durably trace batch write failed; traces are best-effort and were discarded.");
        }
    }
}
