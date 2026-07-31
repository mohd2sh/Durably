namespace Durably.Load.Tests;

/// <summary>
/// Seeds Pending executions in parallel chunks so load tests measure claim/process, not enqueue API cost.
/// </summary>
internal static class LoadBacklogSeeder
{
    public static async Task SeedPendingAsync(
        IExecutionStore store,
        string flowName,
        string instancePrefix,
        int count,
        string contextJson = "{}",
        CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var chunkSize = LoadLimits.SeedChunkSize;
        using var gate = new SemaphoreSlim(LoadLimits.SeedParallelism, LoadLimits.SeedParallelism);
        var tasks = new List<Task>((count + chunkSize - 1) / chunkSize);

        for (var start = 0; start < count; start += chunkSize)
        {
            var offset = start;
            var length = Math.Min(chunkSize, count - start);
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            tasks.Add(SeedChunkAsync(store, flowName, instancePrefix, offset, length, now, contextJson, gate, cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static async Task SeedChunkAsync(
        IExecutionStore store,
        string flowName,
        string instancePrefix,
        int offset,
        int length,
        DateTimeOffset now,
        string contextJson,
        SemaphoreSlim gate,
        CancellationToken cancellationToken)
    {
        try
        {
            for (var i = 0; i < length; i++)
            {
                var index = offset + i;
                await store.CreateAsync(
                        new ExecutionRecord
                        {
                            FlowName = flowName,
                            InstanceId = $"{instancePrefix}{index}",
                            Status = ExecutionStatus.Pending,
                            CurrentStep = 0,
                            ContextJson = contextJson,
                            Attempts = 0,
                            Version = 0,
                            CreatedAt = now.AddTicks(index),
                            UpdatedAt = now.AddTicks(index)
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            gate.Release();
        }
    }
}
