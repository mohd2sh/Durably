using System.Text.Json;
using Xunit;

namespace Durably.Core.UnitTests.Builder;
public sealed class RetryPolicyTests
{
    private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan TwoSeconds = TimeSpan.FromSeconds(2);
    private const int ThreeAttempts = 3;
    private const int ThreeSeconds = 3;

    [Fact]
    public void None_has_single_attempt_and_never_retries()
    {
        // Arrange
        var policy = RetryPolicy.None;
        var exception = new InvalidOperationException("transient");

        // Act / Assert
        Assert.Equal(1, policy.MaxAttempts);
        Assert.False(policy.ShouldRetry(exception));
        Assert.Equal(TimeSpan.Zero, policy.DelayBefore(1));
    }

    [Fact]
    public void Fixed_uses_constant_delay_and_retries_any_exception()
    {
        // Arrange
        var policy = RetryPolicy.Fixed(ThreeAttempts, OneSecond);
        var exception = new Exception("any");

        // Act / Assert
        Assert.Equal(ThreeAttempts, policy.MaxAttempts);
        Assert.True(policy.ShouldRetry(exception));
        Assert.Equal(OneSecond, policy.DelayBefore(1));
        Assert.Equal(OneSecond, policy.DelayBefore(2));
    }

    [Fact]
    public void Exponential_doubles_delay_until_cap()
    {
        // Arrange
        var baseDelay = OneSecond;
        var maxDelay = TimeSpan.FromSeconds(ThreeSeconds);
        var policy = RetryPolicy.Exponential(ThreeAttempts, baseDelay, maxDelay, jitter: false);

        // Act
        var first = policy.DelayBefore(1);
        var second = policy.DelayBefore(2);
        var third = policy.DelayBefore(3);

        // Assert
        Assert.Equal(OneSecond, first);
        Assert.Equal(TwoSeconds, second);
        Assert.Equal(maxDelay, third);
    }

    [Fact]
    public void RetryOn_only_retries_listed_exception_types()
    {
        // Arrange
        var policy = RetryPolicy.Fixed(ThreeAttempts, TimeSpan.Zero)
            .RetryOn(typeof(InvalidOperationException));

        // Act / Assert
        Assert.True(policy.ShouldRetry(new InvalidOperationException("expected")));
        Assert.False(policy.ShouldRetry(new ArgumentException("expected")));
    }

    [Fact]
    public void DoNotRetryOn_blocks_listed_exception_types()
    {
        // Arrange
        var policy = RetryPolicy.Fixed(ThreeAttempts, TimeSpan.Zero)
            .DoNotRetryOn(typeof(ArgumentException));

        // Act / Assert
        Assert.True(policy.ShouldRetry(new InvalidOperationException("expected")));
        Assert.False(policy.ShouldRetry(new ArgumentException("expected")));
    }

    [Fact]
    public void Constructor_rejects_maxAttempts_less_than_one()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicy.Fixed(0, TimeSpan.Zero));
    }
}
