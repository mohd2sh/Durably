using Durably;
using Sample.AspNetCore.Api.Services;

namespace Sample.AspNetCore.Api.Workflows.Oop.PaymentCapture;

public sealed class PaymentCaptureFlow : IFlow<PaymentCaptureState>
{
    public void Build(IFlowBuilder<PaymentCaptureState> builder) => builder
        .Step<CapturePaymentStep>(configure: o => o
            .Timeout(TimeSpan.FromSeconds(2))
            .Retry(RetryPolicy.Fixed(4, TimeSpan.FromMilliseconds(200))
                .RetryOn(typeof(TransientPaymentException))
                .DoNotRetryOn(typeof(PermanentPaymentException))))
        .Step<SettlePaymentStep>()
        .OnSuccess(s => { /* settlement complete */ })
        .OnFailure((s, ex) => s.CaptureAttempts = Math.Max(s.CaptureAttempts, 1));
}
