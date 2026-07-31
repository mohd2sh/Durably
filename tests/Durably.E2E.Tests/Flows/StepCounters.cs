namespace Durably.E2E.Tests.Flows;

public sealed class StepCounters
{
    public int Generate;
    public int Email;
    public int Finalize;
    public int Optional;
    public int BranchA;
    public int BranchB;
    public int Otherwise;
    public int Flaky;
    public int Blocking;
    public bool FailOnce = true;
    public int FailUntilAttempt = 3;
}
