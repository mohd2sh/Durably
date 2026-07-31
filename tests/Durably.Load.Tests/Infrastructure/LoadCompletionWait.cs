namespace Durably.Load.Tests;

/// <summary>Waits for completed-row counts via <see cref="IExecutionQuery"/> (O(1) count, not per-instance polls).</summary>
internal static class LoadCompletionWait
{
    public static async Task WaitUntilCompletedCountAsync(
        IExecutionQuery query,
        string flowName,
        int expected,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        var lastCount = 0;
        while (DateTime.UtcNow < deadline)
        {
            var page = await query.SearchAsync(
                new ExecutionSearchCriteria
                {
                    FlowName = flowName,
                    Status = ExecutionStatus.Completed,
                    Skip = 0,
                    Take = 1
                },
                CancellationToken.None);

            lastCount = page.TotalCount;
            if (lastCount >= expected)
            {
                return;
            }

            await Task.Delay(TestLimits.BriefDelay);
        }

        throw new TimeoutException(
            $"Timed out waiting for {expected} completed executions of '{flowName}'. Last count: {lastCount}.");
    }
}
