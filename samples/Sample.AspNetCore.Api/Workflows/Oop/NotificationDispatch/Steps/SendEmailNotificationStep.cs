using Sample.AspNetCore.Api.Services;

namespace Sample.AspNetCore.Api.Workflows.Oop.NotificationDispatch;

public sealed class SendEmailNotificationStep : IStep<NotificationDispatchState>
{
    private readonly INotificationService _notifications;

    public SendEmailNotificationStep(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task ExecuteAsync(NotificationDispatchState state, IStepContext context, CancellationToken cancellationToken)
    {
        await _notifications.SendEmailAsync(state.Recipient, state.Message, cancellationToken);
        state.DispatchedVia = "email";
    }
}
