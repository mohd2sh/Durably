namespace Sample.AspNetCore.Api.Services;

public interface INotificationService
{
    Task SendEmailAsync(string to, string body, CancellationToken cancellationToken);

    Task SendSmsAsync(string phone, string body, CancellationToken cancellationToken);

    Task SendPushAsync(string deviceId, string body, CancellationToken cancellationToken);
}
