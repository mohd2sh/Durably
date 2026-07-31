using Xunit;

namespace Durably.Core.UnitTests.Execution;
public sealed class ExecutionWorkSignalTests
{
    [Fact]
    public async Task Notify_unblocks_WaitAsync_before_timeout()
    {
        // Arrange
        var signal = new ExecutionWorkSignal();
        using var cts = new CancellationTokenSource(TestLimits.WaitTimeout);
        var waitTask = signal.WaitAsync(TestLimits.WaitTimeout, cts.Token);

        // Act
        await Task.Delay(TestLimits.NotifyDelay);
        signal.Notify();
        await waitTask;

        // Assert — completed without cancellation
        Assert.True(waitTask.IsCompletedSuccessfully);
    }
}
