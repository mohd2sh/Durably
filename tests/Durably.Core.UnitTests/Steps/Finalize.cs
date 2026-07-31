namespace Durably.Core.UnitTests;

public sealed class Finalize : IStep<OrderState>
{
    public Task ExecuteAsync(OrderState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Finalized = true;
        return Task.CompletedTask;
    }
}
