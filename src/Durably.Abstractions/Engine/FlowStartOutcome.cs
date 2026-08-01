namespace Durably.Engine;

public enum FlowStartOutcome
{
    /// <summary>A new run was inserted and is Pending.</summary>
    Created,

    /// <summary>An open run already exists and <see cref="OpenConflictPolicy.Fail"/> was used.</summary>
    Conflict,

    /// <summary>An open run already exists and <see cref="OpenConflictPolicy.Skip"/> was used.</summary>
    Skipped
}
