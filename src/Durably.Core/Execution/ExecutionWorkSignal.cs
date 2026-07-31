namespace Durably.Execution;
internal sealed class ExecutionWorkSignal : IExecutionWorkSignal
{
    private readonly SemaphoreSlim _signal = new(0, 1);

    public void Notify()
    {
        try
        {
            _signal.Release();
        }
        catch (SemaphoreFullException)
        {
            // Already signaled; concurrent Notify calls are idempotent.
        }
    }

    public Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        => _signal.WaitAsync(timeout, cancellationToken);
}
