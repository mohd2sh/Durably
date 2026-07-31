namespace Durably.Execution;
public interface IExecutionWorkSignal
{
    void Notify();

    Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);
}
