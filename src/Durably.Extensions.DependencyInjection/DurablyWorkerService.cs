using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Durably;

internal sealed class DurablyWorkerService : BackgroundService
{
    private const double PollJitterFraction = 0.2;
    private static readonly Random PollJitter = new();

    private readonly IExecutionStore _store;
    private readonly ExecutionProcessor _processor;
    private readonly IExecutionWorkSignal _workSignal;
    private readonly DurablyWorkerOptions _options;
    private readonly ILogger _logger;
    private readonly string _runnerId;

    public DurablyWorkerService(
        IExecutionStore store,
        ExecutionProcessor processor,
        IExecutionWorkSignal workSignal,
        DurablyWorkerOptions options,
        ILogger<DurablyWorkerService>? logger = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _workSignal = workSignal ?? throw new ArgumentNullException(nameof(workSignal));
        _options = options ?? new DurablyWorkerOptions();
        _logger = logger ?? NullLogger<DurablyWorkerService>.Instance;
        _runnerId = _options.RunnerId ?? CreateRunnerId();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var claimedCount = 0;
            try
            {
                claimedCount = await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Durably worker batch failed; will retry after poll interval.");
                DurablyWorkerMetrics.RecordProcessFailure();
            }

            if (claimedCount >= _options.BatchSize)
            {
                continue;
            }

            try
            {
                await _workSignal.WaitAsync(JitteredPollInterval(), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var leaseUntil = DateTimeOffset.UtcNow.Add(_options.LeaseDuration);
        var claimWatch = Stopwatch.StartNew();
        var claimed = await _store.ClaimDueAsync(_runnerId, leaseUntil, _options.BatchSize, cancellationToken)
            .ConfigureAwait(false);
        claimWatch.Stop();
        DurablyWorkerMetrics.RecordClaim(claimWatch.Elapsed, claimed.Count, _options.BatchSize);

        if (claimed.Count == 0)
        {
            return 0;
        }

        var parallelism = Math.Max(1, _options.MaxDegreeOfParallelism);
        using var gate = new SemaphoreSlim(parallelism, parallelism);
        var tasks = new List<Task>(claimed.Count);

        try
        {
            foreach (var record in claimed)
            {
                await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                tasks.Add(ProcessOneAsync(record, gate, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        finally
        {
            if (tasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch
                {
                    // Drain in-flight work before disposing the gate (shutdown / cancel).
                }
            }
        }

        return claimed.Count;
    }

    private async Task ProcessOneAsync(ExecutionRecord record, SemaphoreSlim gate, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _processor.ProcessAsync(record, _runnerId, _options.LeaseDuration, cancellationToken)
                .ConfigureAwait(false);
            if (result.Outcome == FlowRunOutcome.LeaseLost)
            {
                DurablyWorkerMetrics.RecordLeaseLoss();
            }
        }
        catch (Exception ex)
        {
            DurablyWorkerMetrics.RecordProcessFailure();
            _logger.LogWarning(
                ex,
                "Durably worker failed processing flow {FlowName} instance {InstanceId}.",
                record.FlowName,
                record.InstanceId);
        }
        finally
        {
            gate.Release();
        }
    }

    private TimeSpan JitteredPollInterval()
    {
        var baseMs = Math.Max(1, _options.PollInterval.TotalMilliseconds);
        var jitterRange = baseMs * PollJitterFraction;
        double offset;
        lock (PollJitter)
        {
            offset = (PollJitter.NextDouble() * 2 - 1) * jitterRange;
        }

        return TimeSpan.FromMilliseconds(Math.Max(1, baseMs + offset));
    }

    private static string CreateRunnerId()
    {
        var id = $"{Environment.MachineName}:{Process.GetCurrentProcess().Id}:{Guid.NewGuid():N}";
        return id.Length <= DurablyLimits.RunnerIdMaxLength
            ? id
            : id.Substring(0, DurablyLimits.RunnerIdMaxLength);
    }
}
