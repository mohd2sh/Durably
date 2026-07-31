namespace Durably.Builder;
/// <summary>Executes a step's body, resolving any class-based step from the (optional) service provider.</summary>
internal delegate Task StepExecutor<TState>(IServiceProvider? services, TState state, IStepContext context, CancellationToken cancellationToken);
