namespace Sample.AspNetCore.Api.Workflows.Oop.NotificationDispatch;

public sealed class EscalateUrgentStep : IStep<NotificationDispatchState>
{
    public Task ExecuteAsync(NotificationDispatchState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Escalated = true;
        return Task.CompletedTask;
    }
}
