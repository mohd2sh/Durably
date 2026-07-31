namespace Durably.Engine;
public sealed class FlowRunResult
{
    private FlowRunResult(
        FlowRunOutcome outcome,
        FlowStatus? status,
        string? failedStep,
        Exception? error,
        int attempts)
    {
        Outcome = outcome;
        Status = status;
        FailedStep = failedStep;
        Error = error;
        Attempts = attempts;
    }

    public FlowRunOutcome Outcome { get; }

    public FlowStatus? Status { get; }

    public string? FailedStep { get; }

    public Exception? Error { get; }

    public int Attempts { get; }

    public bool IsCompleted =>
        Status == FlowStatus.Completed || Outcome == FlowRunOutcome.AlreadyCompleted;

    public static FlowRunResult AlreadyCompleted()
        => new(FlowRunOutcome.AlreadyCompleted, FlowStatus.Completed, null, null, 0);

    public static FlowRunResult AlreadyRunning()
        => new(FlowRunOutcome.AlreadyRunning, null, null, null, 0);

    public static FlowRunResult LeaseLost()
        => new(FlowRunOutcome.LeaseLost, null, null, null, 0);

    public static FlowRunResult Succeeded(bool wasCreated)
        => new(
            wasCreated ? FlowRunOutcome.Started : FlowRunOutcome.Resumed,
            FlowStatus.Completed,
            null,
            null,
            0);

    public static FlowRunResult StepFailed(bool wasCreated, string failedStep, Exception error, int attempts)
        => new(
            wasCreated ? FlowRunOutcome.Started : FlowRunOutcome.Resumed,
            FlowStatus.Failed,
            failedStep,
            error,
            attempts);
}
