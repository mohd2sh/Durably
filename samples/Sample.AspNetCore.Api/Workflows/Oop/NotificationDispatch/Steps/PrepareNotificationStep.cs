namespace Sample.AspNetCore.Api.Workflows.Oop.NotificationDispatch;

public sealed class PrepareNotificationStep : IStep<NotificationDispatchState>
{
    public Task ExecuteAsync(NotificationDispatchState state, IStepContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(state.Message))
        {
            state.Message = $"Notification {state.NotificationId}";
        }

        return Task.CompletedTask;
    }
}
