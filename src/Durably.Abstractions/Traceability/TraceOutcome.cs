namespace Durably.Traceability;
/// <summary>Terminal outcome recorded for a single step attempt.</summary>
public enum TraceOutcome
{
    /// <summary>The step completed successfully.</summary>
    Succeeded,

    /// <summary>The step attempt failed (may be retried).</summary>
    Failed,

    /// <summary>The step was skipped because its guard evaluated false.</summary>
    Skipped
}
