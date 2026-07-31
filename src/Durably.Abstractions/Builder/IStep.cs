namespace Durably.Builder;
/// <summary>
/// A unit of work in a flow. A step is the atomic unit of retry and resume: once it completes
/// successfully it is checkpointed and never re-run.
/// </summary>
/// <typeparam name="TState">The flow's typed context object, shared and mutated across steps.</typeparam>
public interface IStep<TState>
{
    Task ExecuteAsync(TState state, IStepContext context, CancellationToken cancellationToken);
}
