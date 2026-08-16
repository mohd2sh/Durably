namespace Durably.Engine;

public sealed class FlowStartResult
{
    private FlowStartResult(FlowStartOutcome outcome, string flowName, string instanceId, string runId, ExecutionStatus status)
    {
        Outcome = outcome;
        FlowName = flowName;
        InstanceId = instanceId;
        RunId = runId;
        Status = status;
    }

    public FlowStartOutcome Outcome { get; }

    public string FlowName { get; }

    public string InstanceId { get; }

    /// <summary>Execution identity for this start result (new or existing open run).</summary>
    public string RunId { get; }

    public ExecutionStatus Status { get; }

    public bool WasCreated => Outcome == FlowStartOutcome.Created;

    public static FlowStartResult Created(string flowName, string instanceId, string runId)
        => new(FlowStartOutcome.Created, flowName, instanceId, runId, ExecutionStatus.Pending);

    public static FlowStartResult Conflict(string flowName, string instanceId, string runId, ExecutionStatus status)
        => new(FlowStartOutcome.Conflict, flowName, instanceId, runId, status);

    public static FlowStartResult Skipped(string flowName, string instanceId, string runId, ExecutionStatus status)
        => new(FlowStartOutcome.Skipped, flowName, instanceId, runId, status);
}
