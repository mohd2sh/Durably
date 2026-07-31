namespace Sample.Worker.Services;

public interface IEmailService
{
    Task SendAsync(string orderId, CancellationToken cancellationToken);
}
