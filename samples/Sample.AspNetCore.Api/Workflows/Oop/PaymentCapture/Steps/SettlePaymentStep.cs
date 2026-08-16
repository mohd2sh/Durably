using Sample.AspNetCore.Api.Services;

namespace Sample.AspNetCore.Api.Workflows.Oop.PaymentCapture;

public sealed class SettlePaymentStep : IStep<PaymentCaptureState>
{
    private readonly IPaymentGateway _payments;

    public SettlePaymentStep(IPaymentGateway payments)
    {
        _payments = payments;
    }

    public async Task ExecuteAsync(PaymentCaptureState state, IStepContext context, CancellationToken cancellationToken)
    {
        await _payments.SettleAsync(state.PaymentId, context.IdempotencyKey, cancellationToken);
        state.Settled = true;
    }
}
