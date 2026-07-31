namespace Durably.TestSupport;

/// <summary>Shared timing and batch limits for Durably tests.</summary>
public static class TestLimits
{
    public const int ClaimBatchSize = 50;

    public static readonly TimeSpan DefaultLeaseDuration = TimeSpan.FromMinutes(1);

    public static readonly TimeSpan ShortLeaseDuration = TimeSpan.FromSeconds(5);

    public static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(30);

    public static readonly TimeSpan MediumWaitTimeout = TimeSpan.FromSeconds(20);

    public static readonly TimeSpan ShortWaitTimeout = TimeSpan.FromSeconds(15);

    public static readonly TimeSpan TraceWaitTimeout = TimeSpan.FromSeconds(10);

    public static readonly TimeSpan SignalWakeTimeout = TimeSpan.FromSeconds(10);

    public static readonly TimeSpan SignalWakeMaxElapsed = TimeSpan.FromSeconds(8);

    public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(50);

    public static readonly TimeSpan SlowPollInterval = TimeSpan.FromSeconds(30);

    public static readonly TimeSpan BriefDelay = TimeSpan.FromMilliseconds(100);

    public static readonly TimeSpan MediumDelay = TimeSpan.FromMilliseconds(300);

    public static readonly TimeSpan TinyDelay = TimeSpan.FromMilliseconds(25);

    public static readonly TimeSpan ContentionDelay = TimeSpan.FromMilliseconds(50);

    public static readonly TimeSpan StepTimeout = TimeSpan.FromMilliseconds(100);

    public const string DurableSchema = "durable";

    public const string BootstrapFlowName = "__bootstrap__";

    public const string BootstrapInstanceId = "__bootstrap__";

    public const string SqlServerContainerPassword = "Durably_Test_123!";

    public const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-latest";

    public const string PostgresImage = "postgres:16-alpine";

    public const string PostgresDatabaseName = "durably";

    public const string PostgresUsername = "durably";

    public const string PostgresPassword = "durably";
}
