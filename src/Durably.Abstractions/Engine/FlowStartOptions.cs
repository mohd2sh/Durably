namespace Durably.Engine;

public sealed class FlowStartOptions
{
    /// <summary>
    /// Behavior when an open (Pending/Running) run already exists for the instance id.
    /// Default is <see cref="OpenConflictPolicy.Fail"/>.
    /// </summary>
    public OpenConflictPolicy OpenConflict { get; set; } = OpenConflictPolicy.Fail;

    public IReadOnlyDictionary<string, string>? Metadata { get; set; }
}
