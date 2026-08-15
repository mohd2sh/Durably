using Sample.AspNetCore.Api.Services;

namespace Sample.AspNetCore.Api.Workflows.Oop.NotificationDispatch;

public sealed class SendSmsStep : IStep<NotificationDispatchState>
{
    private readonly INotificationService _notifications;

    public SendSmsStep(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task ExecuteAsync(NotificationDispatchState state, IStepContext context, CancellationToken cancellationToken)
    {
        await _notifications.SendSmsAsync(state.Recipient, state.Message, cancellationToken);
        state.DispatchedVia = "sms";
    }
}
