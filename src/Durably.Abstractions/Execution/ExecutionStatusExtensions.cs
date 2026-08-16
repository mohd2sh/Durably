namespace Durably.Execution;

/// <summary>Shared status helpers so stores and the engine agree on “open” runs.</summary>
public static class ExecutionStatusExtensions
{
    /// <summary>True when the run is still claimable / in flight (Pending or Running).</summary>
    public static bool IsOpen(this ExecutionStatus status)
        => status is ExecutionStatus.Pending or ExecutionStatus.Running;
}
