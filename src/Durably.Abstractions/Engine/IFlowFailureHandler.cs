namespace Durably.Engine;
public interface IFlowFailureHandler<in TState>
{
    Task HandleAsync(TState state, string? failedStep, Exception? error, CancellationToken cancellationToken);
}
