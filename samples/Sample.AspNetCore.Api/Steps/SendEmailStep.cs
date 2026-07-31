using Durably;
using Sample.AspNetCore.Api.Models;
using Sample.AspNetCore.Api.Services;

namespace Sample.AspNetCore.Api.Steps;

public sealed class SendEmailStep : IStep<OrderFinalizeState>
{
    private readonly IEmailService _email;

    public SendEmailStep(IEmailService email)
    {
        _email = email;
    }

    public async Task ExecuteAsync(OrderFinalizeState state, IStepContext context, CancellationToken cancellationToken)
    {
        await _email.SendAsync(
            state.Order.Id,
            state.Order.CustomerEmail,
            $"Order {state.Order.Id} finalized",
            state.Report ?? string.Empty,
            cancellationToken);

        state.EmailSent = true;
    }
}
