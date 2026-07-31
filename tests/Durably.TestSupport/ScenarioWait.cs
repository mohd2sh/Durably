namespace Durably.TestSupport;

public static class ScenarioWait
{
    public static async Task<ExecutionStatusInfo> WaitForStatusAsync(
        IFlowEngine engine,
        string flowName,
        string instanceId,
        ExecutionStatus status,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TestLimits.DefaultWaitTimeout);
        var delay = pollInterval ?? TestLimits.DefaultPollInterval;
        ExecutionStatusInfo? last = null;

        while (DateTime.UtcNow < deadline)
        {
            last = await engine.GetStatusAsync(flowName, instanceId, CancellationToken.None);
            if (last is not null && last.Status == status)
            {
                return last;
            }

            await Task.Delay(delay);
        }

        var actual = last?.Status.ToString() ?? "<null>";
        throw new TimeoutException(
            $"Timed out waiting for '{flowName}/{instanceId}' to become {status}. Last status: {actual}.");
    }

    public static async Task<IReadOnlyList<TraceRecord>> WaitForTracesAsync(
        ITraceStore traceStore,
        string flowName,
        string instanceId,
        int minCount,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TestLimits.TraceWaitTimeout);
        var delay = pollInterval ?? TestLimits.DefaultPollInterval;

        while (DateTime.UtcNow < deadline)
        {
            var traces = await traceStore.LoadAsync(flowName, instanceId, CancellationToken.None);
            if (traces.Count >= minCount)
            {
                return traces;
            }

            await Task.Delay(delay);
        }

        return await traceStore.LoadAsync(flowName, instanceId, CancellationToken.None);
    }

    public static async Task<TState> LoadStateAsync<TState>(
        IExecutionStore store,
        IStateSerializer serializer,
        string flowName,
        string instanceId)
        where TState : class, new()
    {
        var record = await store.LoadAsync(flowName, instanceId, CancellationToken.None)
            ?? throw new InvalidOperationException($"No execution for '{flowName}/{instanceId}'.");
        return (TState)serializer.Deserialize(record.ContextJson, typeof(TState))!;
    }

    public static async Task WaitForCompletedCountAsync(
        IFlowEngine engine,
        string flowName,
        int expected,
        string instancePrefix,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var completed = 0;
            for (var i = 0; i < expected; i++)
            {
                var status = await engine.GetStatusAsync(flowName, $"{instancePrefix}{i}");
                if (status?.Status == ExecutionStatus.Completed)
                {
                    completed++;
                }
            }

            if (completed == expected)
            {
                return;
            }

            await Task.Delay(TestLimits.BriefDelay);
        }

        throw new TimeoutException(
            $"Timed out waiting for {expected} completed instances with prefix '{instancePrefix}'.");
    }
}
