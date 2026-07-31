namespace Durably.Builder;
/// <summary>
/// Per-step retry behaviour: how many times to attempt a step, how long to wait between attempts,
/// and which exceptions are eligible for retry. Immutable; build with the static factories.
/// </summary>
public sealed class RetryPolicy
{
    private readonly Func<int, TimeSpan> _delay;
    private readonly Func<Exception, bool> _shouldRetry;

    private RetryPolicy(int maxAttempts, Func<int, TimeSpan> delay, Func<Exception, bool> shouldRetry)
    {
        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "A step must be attempted at least once.");
        }

        MaxAttempts = maxAttempts;
        _delay = delay;
        _shouldRetry = shouldRetry;
    }

    /// <summary>Total number of attempts (1 = no retry).</summary>
    public int MaxAttempts { get; }

    /// <summary>No retry: the step is attempted exactly once.</summary>
    public static readonly RetryPolicy None = new(1, _ => TimeSpan.Zero, _ => false);

    /// <summary>Retry up to <paramref name="maxAttempts"/> times with a constant delay between attempts.</summary>
    public static RetryPolicy Fixed(int maxAttempts, TimeSpan delay)
        => new(maxAttempts, _ => delay, _ => true);

    /// <summary>
    /// Retry up to <paramref name="maxAttempts"/> times with exponential backoff
    /// (<paramref name="baseDelay"/> * 2^(attempt-1)), optionally capped and jittered.
    /// </summary>
    public static RetryPolicy Exponential(int maxAttempts, TimeSpan baseDelay, TimeSpan? maxDelay = null, bool jitter = false)
    {
        var cap = maxDelay ?? DurablyLimits.DefaultRetryMaxDelay;
        var rng = jitter ? new Random() : null;
        return new RetryPolicy(maxAttempts, attempt =>
        {
            var exp = baseDelay.TotalMilliseconds * Math.Pow(2, Math.Max(0, attempt - 1));
            var capped = Math.Min(exp, cap.TotalMilliseconds);
            if (rng is not null)
            {
                capped *= DurablyLimits.RetryJitterFactor + rng.NextDouble();
                capped = Math.Min(capped, cap.TotalMilliseconds);
            }

            return TimeSpan.FromMilliseconds(capped);
        }, _ => true);
    }

    /// <summary>Restrict retries to a specific set of exception types (an allow-list).</summary>
    public RetryPolicy RetryOn(params Type[] exceptionTypes)
        => new(MaxAttempts, _delay, ex => Array.Exists(exceptionTypes, t => t.IsInstanceOfType(ex)));

    /// <summary>Never retry the given exception types (a deny-list) even if otherwise eligible.</summary>
    public RetryPolicy DoNotRetryOn(params Type[] exceptionTypes)
    {
        var inner = _shouldRetry;
        return new RetryPolicy(MaxAttempts, _delay,
            ex => !Array.Exists(exceptionTypes, t => t.IsInstanceOfType(ex)) && inner(ex));
    }

    /// <summary>The delay to wait before the given (1-based) attempt.</summary>
    public TimeSpan DelayBefore(int attempt) => _delay(attempt);

    /// <summary>Whether the engine should retry after the given exception.</summary>
    public bool ShouldRetry(Exception exception) => _shouldRetry(exception);
}
