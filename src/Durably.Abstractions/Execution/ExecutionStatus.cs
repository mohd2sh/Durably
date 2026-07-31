namespace Durably.Execution;
/// <summary>
/// Persisted lifecycle status of a flow instance in <see cref="ExecutionRecord"/>.
/// See also <see cref="FlowStatus"/> for the terminal result of one processor invocation.
/// </summary>
public enum ExecutionStatus
{
    Running = 0,
    Completed = 1,
    Failed = 2,
    Pending = 3
}
