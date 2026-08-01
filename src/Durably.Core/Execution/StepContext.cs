namespace Durably.Execution;

internal sealed class StepContext : IStepContext
{
    public StepContext(string flowName, string instanceId, string runId, string stepKey, int attempt)
    {
        FlowName = flowName;
        InstanceId = instanceId;
        RunId = runId;
        StepKey = stepKey;
        Attempt = attempt;
        IdempotencyKey = $"{flowName}\u0000{runId}\u0000{stepKey}";
    }

    public string FlowName { get; }

    public string InstanceId { get; }

    public string RunId { get; }

    public string StepKey { get; }

    public int Attempt { get; }

    public string IdempotencyKey { get; }
}
