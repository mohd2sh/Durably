using Sample.Worker.Models;
using Sample.Worker.Services;

namespace Sample.Worker.Steps;

public sealed class SendEmailStep : IStep<OrderFinalizeState>
{
    private readonly IEmailService _email;
    private readonly ILogger<SendEmailStep> _logger;

    public SendEmailStep(IEmailService email, ILogger<SendEmailStep> logger)
    {
        _email = email;
        _logger = logger;
    }

    public async Task ExecuteAsync(OrderFinalizeState state, IStepContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Worker send-email for {OrderId} attempt {Attempt} key {IdempotencyKey}",
            state.OrderId,
            context.Attempt,
            context.IdempotencyKey);

        await _email.SendAsync(state.OrderId, context.IdempotencyKey, cancellationToken);
        state.EmailSent = true;
    }
}
