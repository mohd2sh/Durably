using Durably;
using Sample.Worker.Models;
using Sample.Worker.Services;

namespace Sample.Worker.Steps;

public sealed class SendEmailStep : IStep<OrderFinalizeState>
{
    private readonly IEmailService _email;

    public SendEmailStep(IEmailService email)
    {
        _email = email;
    }

    public async Task ExecuteAsync(OrderFinalizeState state, IStepContext context, CancellationToken cancellationToken)
    {
        await _email.SendAsync(state.OrderId, cancellationToken);
        state.EmailSent = true;
    }
}
