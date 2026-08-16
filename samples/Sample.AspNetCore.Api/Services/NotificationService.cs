namespace Sample.AspNetCore.Api.Services;

public sealed class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string body, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email to {To}: {Body}", to, body);
        return Task.CompletedTask;
    }

    public Task SendSmsAsync(string phone, string body, CancellationToken cancellationToken)
    {
        _logger.LogInformation("SMS to {Phone}: {Body}", phone, body);
        return Task.CompletedTask;
    }

    public Task SendPushAsync(string deviceId, string body, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Push to {DeviceId}: {Body}", deviceId, body);
        return Task.CompletedTask;
    }
}
