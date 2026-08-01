namespace Durably.Execution;

/// <summary>
/// The persisted state of one flow <em>run</em>. This is the unit the engine checkpoints after every
/// step and rehydrates on resume. Persistence providers map this to a row (or document).
/// </summary>
/// <remarks>
/// Identity is <c>(FlowName, RunId)</c>. <see cref="InstanceId"/> is the business key and may have
/// many runs over time; at most one run may be open (Pending/Running) per instance.
/// Mutability is intentional: the engine and stores update this DTO in place during checkpoints.
/// Application code should treat loaded records as read-only snapshots.
/// </remarks>
public sealed class ExecutionRecord
{
    /// <summary>Flow definition name. Part of the run's primary key.</summary>
    public string FlowName { get; set; } = string.Empty;

    /// <summary>System-generated run identity. Part of the run's primary key.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>Business/instance key (e.g. product id). Correlation only — not unique across runs.</summary>
    public string InstanceId { get; set; } = string.Empty;

    public ExecutionStatus Status { get; set; }

    /// <summary>Index of the next step to execute. Everything before this index is durably done.</summary>
    public int CurrentStep { get; set; }

    /// <summary>Serialized typed context (<c>TState</c>) as of the last checkpoint.</summary>
    public string ContextJson { get; set; } = string.Empty;

    /// <summary>
    /// Hash chain of step keys already passed (executed or skipped). Null on new/legacy rows
    /// until the first advance or legacy stamp. Detects definition shape changes under a cursor.
    /// </summary>
    public string? StepPathHash { get; set; }

    /// <summary>Attempts spent on the most recent step (for diagnostics on failure).</summary>
    public int Attempts { get; set; }

    /// <summary>Key of the step that failed, when <see cref="Status"/> is <see cref="ExecutionStatus.Failed"/>.</summary>
    public string? FailedStep { get; set; }

    /// <summary>Message of the failure, when applicable.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Optimistic concurrency token; incremented on each successful checkpoint write.</summary>
    public long Version { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Runner id that currently owns this run, or <c>null</c> when unleased.</summary>
    public string? LockedBy { get; set; }

    /// <summary>When the current lease expires (UTC), or <c>null</c> when unleased.</summary>
    public DateTimeOffset? LockedUntil { get; set; }

    /// <summary>Optional JSON metadata bag for search and UI display (set at run creation).</summary>
    public string? MetadataJson { get; set; }
}
