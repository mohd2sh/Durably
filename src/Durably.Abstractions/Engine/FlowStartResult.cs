namespace Durably.Engine;
public sealed class FlowStartResult
{
    private FlowStartResult(FlowStartOutcome outcome, string flowName, string instanceId, ExecutionStatus status)
    {
        Outcome = outcome;
        FlowName = flowName;
        InstanceId = instanceId;
        Status = status;
    }

    public FlowStartOutcome Outcome { get; }

    public string FlowName { get; }

    public string InstanceId { get; }

    public ExecutionStatus Status { get; }

    public bool WasCreated => Outcome == FlowStartOutcome.Created;

    public static FlowStartResult Created(string flowName, string instanceId)
        => new(FlowStartOutcome.Created, flowName, instanceId, ExecutionStatus.Pending);

    public static FlowStartResult AlreadyExists(string flowName, string instanceId, ExecutionStatus status)
        => new(FlowStartOutcome.AlreadyExists, flowName, instanceId, status);
}
