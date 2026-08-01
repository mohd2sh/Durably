using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Durably.Execution;
/// <summary>
/// Claims an execution lease and runs pending steps until completion, failure, or lease loss.
/// </summary>
internal sealed class ExecutionProcessor
{
    private const string QuarantineStepKey = "_quarantine";
    private const string DefinitionMismatchStepKey = "_definition-mismatch";

    private readonly IExecutionStore _store;
    private readonly IFlowRegistry _registry;
    private readonly IStateSerializer _serializer;
    private readonly IServiceProvider? _services;
    private readonly StepDefaults _stepDefaults;
    private readonly ExecutionTraceEmitter _traces;
    private readonly FlowHookInvoker _hooks;
    private readonly StepExecutionRunner _stepRunner;
    private readonly ILogger _logger;

    public ExecutionProcessor(
        IExecutionStore store,
        IFlowRegistry registry,
        IStateSerializer? serializer = null,
        IServiceProvider? services = null,
        ITraceSink? trace = null,
        StepDefaults? stepDefaults = null,
        ILogger? logger = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _serializer = serializer ?? new JsonStateSerializer();
        _services = services;
        _stepDefaults = stepDefaults ?? StepDefaults.None;
        _logger = logger ?? NullLogger.Instance;
        var sink = trace ?? NoOpTraceSink.Instance;
        _traces = new ExecutionTraceEmitter(sink);
        _hooks = new FlowHookInvoker(services, _logger);
        _stepRunner = new StepExecutionRunner(_store, _serializer, services, _traces, _logger);
    }

    public Task<FlowRunResult> ProcessAsync(
        ExecutionRecord record,
        string runnerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        if (string.IsNullOrWhiteSpace(runnerId))
        {
            throw new ArgumentException("Runner id is required.", nameof(runnerId));
        }

        if (!_registry.TryGet(record.FlowName, out var registration))
        {
            return QuarantineAsync(
                record,
                runnerId,
                leaseDuration,
                $"Flow '{record.FlowName}' is not registered. Register it with AddFlow / AddFlowsFromAssembly.",
                cancellationToken);
        }

        if (registration is not IProcessableFlowRegistration processable)
        {
            return QuarantineAsync(
                record,
                runnerId,
                leaseDuration,
                $"Flow '{record.FlowName}' registration does not support processing.",
                cancellationToken);
        }

        return processable.ProcessAsync(this, record, runnerId, leaseDuration, cancellationToken);
    }

    internal async Task<FlowRunResult> ProcessTypedAsync<TState>(
        FlowRegistration<TState> registration,
        ExecutionRecord record,
        string runnerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
        where TState : class, new()
    {
        if (record.Status == ExecutionStatus.Completed)
        {
            return FlowRunResult.AlreadyCompleted();
        }

        var builder = registration.Materialize(_services, _stepDefaults);
        var wasCreated = record.Status == ExecutionStatus.Pending && record.CurrentStep == 0 && record.Version == 0;

        var pathError = TryValidateStepPath(record, builder.Nodes);
        if (pathError is not null)
        {
            return await QuarantineAsync(
                    record,
                    runnerId,
                    leaseDuration,
                    pathError,
                    cancellationToken,
                    DefinitionMismatchStepKey)
                .ConfigureAwait(false);
        }

        TState state;
        try
        {
            state = (TState)_serializer.Deserialize(record.ContextJson, typeof(TState))!;
        }
        catch (Exception ex)
        {
            return await QuarantineAsync(
                    record,
                    runnerId,
                    leaseDuration,
                    $"Failed to deserialize execution state: {ex.Message}",
                    cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            if (record.Status == ExecutionStatus.Pending)
            {
                record.Status = ExecutionStatus.Running;
                record.UpdatedAt = DateTimeOffset.UtcNow;
                await SaveCheckpointAsync(record, runnerId, leaseDuration, cancellationToken).ConfigureAwait(false);
            }

            return await RunPendingStepsAsync(
                record.FlowName,
                builder,
                record.InstanceId,
                record,
                state,
                wasCreated,
                runnerId,
                leaseDuration,
                cancellationToken).ConfigureAwait(false);
        }
        catch (LeaseLostException)
        {
            return FlowRunResult.LeaseLost();
        }
        finally
        {
            await _store.ReleaseLeaseAsync(record.FlowName, record.RunId, runnerId, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Returns a quarantine message on mismatch / truncation; stamps legacy null hashes; otherwise null.
    /// </summary>
    private static string? TryValidateStepPath<TState>(ExecutionRecord record, IReadOnlyList<StepNode<TState>> nodes)
    {
        if (record.CurrentStep < 0)
        {
            return $"Execution '{record.FlowName}/{record.InstanceId}' has invalid CurrentStep {record.CurrentStep}. "
                + "Redeploy a compatible definition or resume/close this instance manually.";
        }

        if (record.CurrentStep > nodes.Count)
        {
            return $"Execution '{record.FlowName}/{record.InstanceId}' CurrentStep {record.CurrentStep} exceeds "
                + $"definition length {nodes.Count} (steps were removed or the definition changed). "
                + "Redeploy a compatible definition or resume/close this instance manually.";
        }

        if (record.CurrentStep == 0)
        {
            return null;
        }

        var keys = new string[record.CurrentStep];
        for (var i = 0; i < record.CurrentStep; i++)
        {
            keys[i] = nodes[i].Key;
        }

        var recomputed = StepPathHasher.ComputePrefix(keys, record.CurrentStep);
        if (record.StepPathHash is null)
        {
            // Legacy row: stamp once and continue (nothing recorded to verify against).
            record.StepPathHash = recomputed;
            return null;
        }

        if (string.Equals(record.StepPathHash, recomputed, StringComparison.Ordinal))
        {
            return null;
        }

        var divergentKey = FindFirstDivergentKey(keys, record.StepPathHash);
        return $"Execution '{record.FlowName}/{record.InstanceId}' definition mismatch at CurrentStep {record.CurrentStep}"
            + (divergentKey is null ? "." : $" (first divergent key '{divergentKey}').")
            + " The flow shape changed under an in-flight instance. "
            + "Redeploy a compatible definition or resume/close this instance manually.";
    }

    private static string? FindFirstDivergentKey(IReadOnlyList<string> keys, string storedHash)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            var prefixMatches = string.Equals(
                StepPathHasher.ComputePrefix(keys, i),
                storedHash,
                StringComparison.Ordinal);
            var nextMatches = string.Equals(
                StepPathHasher.ComputePrefix(keys, i + 1),
                storedHash,
                StringComparison.Ordinal);
            if (prefixMatches && !nextMatches)
            {
                return keys[i];
            }
        }

        return keys.Count > 0 ? keys[0] : null;
    }

    private async Task<FlowRunResult> QuarantineAsync(
        ExecutionRecord record,
        string runnerId,
        TimeSpan leaseDuration,
        string errorMessage,
        CancellationToken cancellationToken,
        string failedStepKey = QuarantineStepKey)
    {
        _logger.LogWarning(
            "Quarantining flow {FlowName} instance {InstanceId}: {ErrorMessage}",
            record.FlowName,
            record.InstanceId,
            errorMessage);

        try
        {
            record.Status = ExecutionStatus.Failed;
            record.FailedStep = failedStepKey;
            record.ErrorMessage = errorMessage;
            record.UpdatedAt = DateTimeOffset.UtcNow;
            await SaveCheckpointAsync(record, runnerId, leaseDuration, cancellationToken).ConfigureAwait(false);
            return FlowRunResult.StepFailed(
                wasCreated: false,
                failedStepKey,
                new InvalidOperationException(errorMessage),
                record.Attempts);
        }
        catch (LeaseLostException)
        {
            return FlowRunResult.LeaseLost();
        }
        catch (ConcurrencyConflictException ex)
        {
            _logger.LogWarning(
                ex,
                "Could not quarantine flow {FlowName} instance {InstanceId}; lease or version conflict.",
                record.FlowName,
                record.InstanceId);
            return FlowRunResult.LeaseLost();
        }
        finally
        {
            await _store.ReleaseLeaseAsync(record.FlowName, record.RunId, runnerId, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<FlowRunResult> RunPendingStepsAsync<TState>(
        string flowName,
        FlowBuilder<TState> builder,
        string instanceId,
        ExecutionRecord record,
        TState state,
        bool wasCreated,
        string runnerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var nodes = builder.Nodes;

        for (var index = record.CurrentStep; index < nodes.Count; index++)
        {
            var node = nodes[index];

            if (ShouldSkipStep(node, state))
            {
                _traces.EmitSkipped(flowName, instanceId, record.RunId, node.Key);

                await AdvanceAsync(record, index + 1, node.Key, state, runnerId, leaseDuration, cancellationToken)
                    .ConfigureAwait(false);

                continue;
            }

            var failure = await _stepRunner.ExecuteWithRetryAsync(
                flowName,
                instanceId,
                record.RunId,
                node,
                state,
                record,
                wasCreated,
                runnerId,
                leaseDuration,
                cancellationToken).ConfigureAwait(false);

            if (failure is not null)
            {
                await _hooks.InvokeFailureHooksAsync(builder, state, failure.FailedStep, failure.Error, cancellationToken)
                    .ConfigureAwait(false);
                return failure;
            }

            await AdvanceAsync(record, index + 1, node.Key, state, runnerId, leaseDuration, cancellationToken)
                .ConfigureAwait(false);
        }

        record.Status = ExecutionStatus.Completed;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveCheckpointAsync(record, runnerId, leaseDuration, cancellationToken).ConfigureAwait(false);
        await _hooks.InvokeSuccessHooksAsync(builder, state, cancellationToken).ConfigureAwait(false);
        return FlowRunResult.Succeeded(wasCreated);
    }

    private static bool ShouldSkipStep<TState>(StepNode<TState> node, TState state)
        => node.Guard is not null && !node.Guard(state);

    private async Task AdvanceAsync<TState>(
        ExecutionRecord record,
        int nextStep,
        string passedStepKey,
        TState state,
        string runnerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        record.Status = ExecutionStatus.Running;
        record.CurrentStep = nextStep;
        record.StepPathHash = StepPathHasher.Append(record.StepPathHash ?? StepPathHasher.Seed(), passedStepKey);
        record.ContextJson = _serializer.Serialize(state!);
        record.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveCheckpointAsync(record, runnerId, leaseDuration, cancellationToken).ConfigureAwait(false);
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
