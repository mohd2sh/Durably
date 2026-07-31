namespace Durably.Engine;
/// <summary>
/// Terminal outcome of a single processor run, reported on <see cref="FlowRunResult"/>.
/// Distinct from <see cref="ExecutionStatus"/>, which is the persisted lifecycle of an instance
/// (Pending / Running / Completed / Failed).
/// </summary>
public enum FlowStatus
{
    Completed,
    Failed
}
