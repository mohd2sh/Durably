namespace Durably.Execution;
/// <summary>Thrown when <see cref="IExecutionStore.CreateAsync"/> is called for an instance that already exists.</summary>
public sealed class ExecutionAlreadyExistsException : Exception
{
    public ExecutionAlreadyExistsException(string flowName, string instanceId)
        : base($"Flow '{flowName}' instance '{instanceId}' already exists.")
    {
        FlowName = flowName;
        InstanceId = instanceId;
    }

    public string FlowName { get; }

    public string InstanceId { get; }
}
