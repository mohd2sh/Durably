using Sample.AspNetCore.Api.Services;

namespace Sample.AspNetCore.Api.Workflows.Oop.OrderFinalize;

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
            "Sending finalize email for {OrderId} (attempt {Attempt}, key {IdempotencyKey})",
            state.Order.Id,
            context.Attempt,
            context.IdempotencyKey);

        await _email.SendAsync(
            state.Order.Id,
            state.Order.CustomerEmail,
            $"Order {state.Order.Id} finalized",
            state.Report ?? string.Empty,
            context.IdempotencyKey,
            cancellationToken);

        state.EmailSent = true;
    }
}
