using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Durably.Execution;
/// <summary>Runs a single step with retry, timeout, and tracing.</summary>
internal sealed class StepExecutionRunner
{
    private readonly IExecutionStore _store;
    private readonly IStateSerializer _serializer;
    private readonly IServiceProvider? _services;
    private readonly ExecutionTraceEmitter _traces;
    private readonly ILogger _logger;

    public StepExecutionRunner(
        IExecutionStore store,
        IStateSerializer serializer,
        IServiceProvider? services,
        ExecutionTraceEmitter traces,
        ILogger? logger = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _services = services;
        _traces = traces ?? throw new ArgumentNullException(nameof(traces));
        _logger = logger ?? NullLogger.Instance;
    }

    public async Task<FlowRunResult?> ExecuteWithRetryAsync<TState>(
        string flowName,
        string instanceId,
        StepNode<TState> node,
        TState state,
        ExecutionRecord record,
        bool wasCreated,
        string runnerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            var inputJson = _serializer.Serialize(state!);
            var stopwatch = Stopwatch.StartNew();
            var context = new StepContext(flowName, instanceId, node.Key, attempt);
            try
            {
                await RunNodeAsync(node, state, context, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();
                _traces.EmitSuccess(
                    flowName,
                    instanceId,
                    node.Key,
                    attempt,
                    inputJson,
                    _serializer.Serialize(state!),
                    (int)stopwatch.ElapsedMilliseconds);
                record.Attempts = attempt;
                record.FailedStep = null;
                record.ErrorMessage = null;
                return null;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                stopwatch.Stop();
                var durationMs = (int)stopwatch.ElapsedMilliseconds;
                if (attempt < node.Retry.MaxAttempts && node.Retry.ShouldRetry(ex))
                {
                    if (!await RetryOrLoseLeaseAsync(
                            flowName,
                            instanceId,
                            node,
                            attempt,
                            inputJson,
                            durationMs,
                            ex,
                            runnerId,
                            leaseDuration,
                            cancellationToken)
                        .ConfigureAwait(false))
                    {
                        return FlowRunResult.LeaseLost();
                    }

                    continue;
                }

                _traces.EmitFailure(
                    flowName,
                    instanceId,
                    node.Key,
                    attempt,
                    inputJson,
                    durationMs,
                    ex.Message);
                return await HandleTerminalFailureAsync(
                    record,
                    node.Key,
                    state,
                    ex,
                    attempt,
                    wasCreated,
                    runnerId,
                    leaseDuration,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Emits retry trace, renews lease around backoff. Returns <c>true</c> to continue the retry loop,
    /// <c>false</c> when the lease was lost.
    /// </summary>
    private async Task<bool> RetryOrLoseLeaseAsync<TState>(
        string flowName,
        string instanceId,
        StepNode<TState> node,
        int attempt,
        string inputJson,
        int durationMs,
        Exception ex,
        string runnerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        _traces.EmitFailure(flowName, instanceId, node.Key, attempt, inputJson, durationMs, ex.Message);
        _logger.LogDebug(ex, "Step {StepKey} attempt {Attempt} failed; retrying.", node.Key, attempt);

        if (!await RenewLeaseAsync(flowName, instanceId, runnerId, leaseDuration, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        var delay = node.Retry.DelayBefore(attempt);
        if (delay <= TimeSpan.Zero)
        {
            return true;
        }

        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        return await RenewLeaseAsync(flowName, instanceId, runnerId, leaseDuration, cancellationToken).ConfigureAwait(false);
    }

    private async Task<FlowRunResult> HandleTerminalFailureAsync<TState>(
        ExecutionRecord record,
        string failedStep,
        TState state,
        Exception ex,
        int attempt,
        bool wasCreated,
        string runnerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        record.Status = ExecutionStatus.Failed;
        record.FailedStep = failedStep;
        record.ErrorMessage = ex.Message;
        record.Attempts = attempt;
        record.ContextJson = _serializer.Serialize(state!);
        record.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveCheckpointAsync(record, runnerId, leaseDuration, cancellationToken).ConfigureAwait(false);
        return FlowRunResult.StepFailed(wasCreated, failedStep, ex, attempt);
    }

    private async Task RunNodeAsync<TState>(
        StepNode<TState> node,
        TState state,
        IStepContext context,
        CancellationToken cancellationToken)
    {
        if (node.Timeout is null)
        {
            await node.Execute(_services, state, context, cancellationToken).ConfigureAwait(false);
            return;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(node.Timeout.Value);
        await node.Execute(_services, state, context, linked.Token).ConfigureAwait(false);
    }

    private async Task<bool> RenewLeaseAsync(
        string flowName,
        string instanceId,
        string runnerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var leaseUntil = DateTimeOffset.UtcNow.Add(leaseDuration);
        return await _store.TryAcquireLeaseAsync(flowName, instanceId, runnerId, leaseUntil, cancellationToken)
            .ConfigureAwait(false);
    }

    private Task SaveCheckpointAsync(
        ExecutionRecord record,
        string runnerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var leaseUntil = DateTimeOffset.UtcNow.Add(leaseDuration);
        return _store.SaveCheckpointAsync(record, runnerId, leaseUntil, cancellationToken);
    }
}
