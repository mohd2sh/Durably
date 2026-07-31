namespace Durably.Core.UnitTests;

public sealed class SendEmail : IStep<OrderState>
{
    public Task ExecuteAsync(OrderState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.EmailSent = true;
        return Task.CompletedTask;
    }
}
