namespace Durably.TestSupport;

/// <summary>
/// Shared helpers to claim/lease an execution and drive <see cref="ExecutionProcessor"/>.
/// </summary>
internal static class ExecutionResume
{
    private const string DefaultRunnerId = "test-resume-runner";

    /// <summary>
    /// Claims a due execution or acquires a lease, then processes it (Pending/Running/Failed).
    /// </summary>
    public static async Task<FlowRunResult> ProcessAsync(
        IExecutionStore store,
        ExecutionProcessor processor,
        string flowName,
        string instanceId,
        string? runnerId = null)
    {
        var resolvedRunnerId = runnerId ?? DefaultRunnerId;
        var leaseDuration = TestLimits.DefaultLeaseDuration;
        var leaseUntil = DateTimeOffset.UtcNow.Add(leaseDuration);
        var claimed = await store.ClaimDueAsync(
            resolvedRunnerId,
            leaseUntil,
            TestLimits.ClaimBatchSize,
            CancellationToken.None);
        var match = claimed.FirstOrDefault(record =>
            record.FlowName == flowName && record.InstanceId == instanceId);
        if (match is not null)
        {
            return await processor.ProcessAsync(match, resolvedRunnerId, leaseDuration);
        }

        var current = await store.LoadLatestAsync(flowName, instanceId, CancellationToken.None)
            ?? throw new InvalidOperationException($"No execution found for '{flowName}/{instanceId}'.");

        if (!await store.TryAcquireLeaseAsync(
                flowName,
                current.RunId,
                resolvedRunnerId,
                leaseUntil,
                CancellationToken.None))
        {
            return FlowRunResult.AlreadyRunning();
        }

        var leased = await store.LoadAsync(flowName, current.RunId, CancellationToken.None)
            ?? throw new InvalidOperationException("Execution disappeared after lease acquisition.");
        return await processor.ProcessAsync(leased, resolvedRunnerId, leaseDuration);
    }

    /// <summary>
    /// Explicit resume for terminal Failed instances (worker only claims Pending/Running).
    /// </summary>
    public static async Task<FlowRunResult> ResumeFailedAsync(
        IExecutionStore store,
        ExecutionProcessor processor,
        string flowName,
        string instanceId,
        string? runnerId = null)
    {
        var resolvedRunnerId = runnerId ?? DefaultRunnerId;
        var leaseDuration = TestLimits.DefaultLeaseDuration;
        var leaseUntil = DateTimeOffset.UtcNow.Add(leaseDuration);
        var record = await store.LoadLatestAsync(flowName, instanceId, CancellationToken.None)
            ?? throw new InvalidOperationException($"No execution found for flow '{flowName}' instance '{instanceId}'.");

        if (record.Status != ExecutionStatus.Failed)
        {
            throw new InvalidOperationException(
                $"Expected Failed status to resume, found {record.Status} for '{flowName}/{instanceId}'.");
        }

        if (!await store.TryAcquireLeaseAsync(
                flowName,
                record.RunId,
                resolvedRunnerId,
                leaseUntil,
                CancellationToken.None))
        {
            return FlowRunResult.AlreadyRunning();
        }

        var leased = await store.LoadAsync(flowName, record.RunId, CancellationToken.None)
            ?? throw new InvalidOperationException("Execution disappeared after lease acquisition.");
        return await processor.ProcessAsync(leased, resolvedRunnerId, leaseDuration);
    }
}
