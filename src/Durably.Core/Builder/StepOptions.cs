namespace Durably.Builder;
internal sealed class StepOptions : IStepOptions
{
    public RetryPolicy RetryPolicy { get; private set; } = RetryPolicy.None;

    public TimeSpan? TimeoutValue { get; private set; }

    public IStepOptions Retry(RetryPolicy policy)
    {
        RetryPolicy = policy ?? RetryPolicy.None;
        return this;
    }

    public IStepOptions Timeout(TimeSpan timeout)
    {
        TimeoutValue = timeout;
        return this;
    }

    internal void ApplyDefaults(StepDefaults? defaults)
    {
        if (defaults is null)
        {
            return;
        }

        RetryPolicy = defaults.DefaultRetry;
        TimeoutValue = defaults.DefaultStepTimeout;
    }

    public static StepOptions Resolve(Action<IStepOptions>? configure, StepDefaults? defaults = null)
    {
        var options = new StepOptions();
        options.ApplyDefaults(defaults);
        configure?.Invoke(options);
        return options;
    }
}
