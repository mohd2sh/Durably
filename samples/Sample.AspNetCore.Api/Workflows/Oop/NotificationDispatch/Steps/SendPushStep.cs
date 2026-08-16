using Sample.AspNetCore.Api.Services;

namespace Sample.AspNetCore.Api.Workflows.Oop.NotificationDispatch;

public sealed class SendPushStep : IStep<NotificationDispatchState>
{
    private readonly INotificationService _notifications;

    public SendPushStep(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task ExecuteAsync(NotificationDispatchState state, IStepContext context, CancellationToken cancellationToken)
    {
        await _notifications.SendPushAsync(state.Recipient, state.Message, cancellationToken);
        state.DispatchedVia = "push";
    }
}
