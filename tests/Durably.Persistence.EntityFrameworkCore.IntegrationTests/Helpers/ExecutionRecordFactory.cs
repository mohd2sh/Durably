namespace Durably.Persistence.EntityFrameworkCore.IntegrationTests;

internal static class ExecutionRecordFactory
{
    private const string DefaultContextJson = "{}";

    public static ExecutionRecord Create(
        string flowName,
        string instanceId,
        ExecutionStatus status,
        string contextJson = DefaultContextJson,
        int currentStep = 0,
        string? metadataJson = null,
        DateTimeOffset? timestamp = null)
    {
        var now = timestamp ?? DateTimeOffset.UtcNow;
        return new ExecutionRecord
        {
            FlowName = flowName,
            RunId = Guid.NewGuid().ToString("N"),
            InstanceId = instanceId,
            Status = status,
            CurrentStep = currentStep,
            ContextJson = contextJson,
            MetadataJson = metadataJson,
            Attempts = 0,
            Version = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static ExecutionRecord Running(
        string flowName,
        string instanceId,
        string contextJson = DefaultContextJson,
        int currentStep = 0)
        => Create(flowName, instanceId, ExecutionStatus.Running, contextJson, currentStep);
}
