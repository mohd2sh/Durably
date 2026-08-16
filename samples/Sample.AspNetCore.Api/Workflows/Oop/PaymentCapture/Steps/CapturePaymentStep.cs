using Sample.AspNetCore.Api.Services;

namespace Sample.AspNetCore.Api.Workflows.Oop.PaymentCapture;

public sealed class CapturePaymentStep : IStep<PaymentCaptureState>
{
    private readonly IPaymentGateway _payments;
    private readonly ILogger<CapturePaymentStep> _logger;

    public CapturePaymentStep(IPaymentGateway payments, ILogger<CapturePaymentStep> logger)
    {
        _payments = payments;
        _logger = logger;
    }

    public async Task ExecuteAsync(PaymentCaptureState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.CaptureAttempts = context.Attempt;
        _logger.LogInformation(
            "Capturing payment {PaymentId} attempt {Attempt} key {IdempotencyKey}",
            state.PaymentId,
            context.Attempt,
            context.IdempotencyKey);

        await _payments.CaptureAsync(state.PaymentId, state.Amount, context.IdempotencyKey, cancellationToken);
        state.Captured = true;
    }
}
