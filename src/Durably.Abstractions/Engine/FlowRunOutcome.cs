namespace Durably.Engine;
public enum FlowRunOutcome
{
    Started,
    Resumed,
    Completed,
    AlreadyCompleted,
    Failed,
    AlreadyRunning,
    LeaseLost
}
