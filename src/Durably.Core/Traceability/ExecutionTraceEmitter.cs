namespace Durably.Traceability;

/// <summary>Emits per-step trace records through an <see cref="ITraceSink"/>.</summary>
internal sealed class ExecutionTraceEmitter
{
    private readonly ITraceSink _trace;

    public ExecutionTraceEmitter(ITraceSink trace)
    {
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
    }

    public void EmitSkipped(string flowName, string instanceId, string runId, string stepKey)
        => Emit(flowName, instanceId, runId, stepKey, 0, TraceOutcome.Skipped, null, null, 0, null);

    public void EmitSuccess(
        string flowName,
        string instanceId,
        string runId,
        string stepKey,
        int attempt,
        string inputJson,
        string outputJson,
        int durationMs)
        => Emit(flowName, instanceId, runId, stepKey, attempt, TraceOutcome.Succeeded, inputJson, outputJson, durationMs, null);

    public void EmitFailure(
        string flowName,
        string instanceId,
        string runId,
        string stepKey,
        int attempt,
        string inputJson,
        int durationMs,
        string exceptionMessage)
        => Emit(flowName, instanceId, runId, stepKey, attempt, TraceOutcome.Failed, inputJson, null, durationMs, exceptionMessage);

    private void Emit(
        string flowName,
        string instanceId,
        string runId,
        string stepKey,
        int attempt,
        TraceOutcome outcome,
        string? inputJson,
        string? outputJson,
        int durationMs,
        string? exceptionMessage)
    {
        _trace.Emit(new TraceRecord
        {
            FlowName = flowName,
            InstanceId = instanceId,
            RunId = runId,
            StepKey = stepKey,
            Attempt = attempt,
            Outcome = outcome,
            InputJson = inputJson,
            OutputJson = outputJson,
            DurationMs = durationMs,
            ExceptionMessage = exceptionMessage,
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}
