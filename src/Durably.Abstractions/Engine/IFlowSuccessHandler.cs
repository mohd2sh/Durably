namespace Durably.Engine;
public interface IFlowSuccessHandler<in TState>
{
    Task HandleAsync(TState state, CancellationToken cancellationToken);
}
