namespace Sample.AspNetCore.Api.Services;

public interface IPaymentGateway
{
    Task CaptureAsync(string paymentId, decimal amount, string idempotencyKey, CancellationToken cancellationToken);

    Task SettleAsync(string paymentId, string idempotencyKey, CancellationToken cancellationToken);
}
