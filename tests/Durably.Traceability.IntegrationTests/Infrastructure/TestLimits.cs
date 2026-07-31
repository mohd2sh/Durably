namespace Durably.Traceability.IntegrationTests.Infrastructure;

internal static class TestLimits
{
    public static readonly TimeSpan ShortFlush = TimeSpan.FromMilliseconds(30);

    public static readonly TimeSpan DefaultWait = TimeSpan.FromSeconds(3);

    public const int DefaultBatchSize = 5;

    public const int TinyCapacity = 8;
}
