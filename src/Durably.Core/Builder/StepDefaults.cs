namespace Durably.Builder;
/// <summary>Global step defaults from <c>AddDurably</c>; per-step builder options override these.</summary>
internal sealed class StepDefaults
{
    public static StepDefaults None { get; } = new();

    public RetryPolicy DefaultRetry { get; }

    public TimeSpan? DefaultStepTimeout { get; }

    public StepDefaults()
        : this(RetryPolicy.None, null)
    {
    }

    public StepDefaults(RetryPolicy defaultRetry, TimeSpan? defaultStepTimeout)
    {
        DefaultRetry = defaultRetry ?? RetryPolicy.None;
        DefaultStepTimeout = defaultStepTimeout;
    }
}
