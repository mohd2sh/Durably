namespace Sample.AspNetCore.Api.Services;

public interface IEmailService
{
    Task SendAsync(
        string orderId,
        string recipient,
        string subject,
        string body,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
