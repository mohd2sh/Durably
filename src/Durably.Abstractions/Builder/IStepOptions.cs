namespace Durably.Builder;
/// <summary>Per-step configuration applied via the <c>configure</c> callback on the builder methods.</summary>
public interface IStepOptions
{
    /// <summary>Set the retry policy for this step. Defaults to <see cref="RetryPolicy.None"/>.</summary>
    IStepOptions Retry(RetryPolicy policy);

    /// <summary>Cancel the step's attempt if it runs longer than the given timeout.</summary>
    IStepOptions Timeout(TimeSpan timeout);
}
