namespace Durably.Engine;

/// <summary>
/// What <see cref="IFlowEngine"/> start does when an open run (Pending/Running)
/// already exists for the same flow + instance id.
/// </summary>
public enum OpenConflictPolicy
{
    /// <summary>Return <see cref="FlowStartOutcome.Conflict"/>; do not create a new run.</summary>
    Fail = 0,

    /// <summary>
    /// Return <see cref="FlowStartOutcome.Skipped"/> with the existing run id; do not create a new run.
    /// The in-flight worker continues.
    /// </summary>
    Skip = 1
}
