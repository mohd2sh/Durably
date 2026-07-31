namespace Durably.Core.UnitTests;

/// <summary>Core unit-test timing limits (keep aligned with <c>Durably.TestSupport.TestLimits</c>).</summary>
internal static class TestLimits
{
    public const int ClaimBatchSize = 50;

    public static readonly TimeSpan DefaultLeaseDuration = TimeSpan.FromMinutes(1);

    public static readonly TimeSpan ContentionDelay = TimeSpan.FromMilliseconds(50);

    public static readonly TimeSpan LongStepDelay = TimeSpan.FromMilliseconds(500);

    public static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(50);

    public static readonly TimeSpan LongDelay = TimeSpan.FromSeconds(30);

    public static readonly TimeSpan NotifyDelay = TimeSpan.FromMilliseconds(50);

    public static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);
}
