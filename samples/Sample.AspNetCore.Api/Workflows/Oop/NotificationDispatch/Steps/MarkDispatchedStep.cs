namespace Sample.AspNetCore.Api.Workflows.Oop.NotificationDispatch;

public sealed class MarkDispatchedStep : IStep<NotificationDispatchState>
{
    public Task ExecuteAsync(NotificationDispatchState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Completed = true;
        return Task.CompletedTask;
    }
}
