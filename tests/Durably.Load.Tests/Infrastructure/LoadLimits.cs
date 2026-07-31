namespace Durably.Load.Tests;

internal static class LoadLimits
{
    /// <summary>Primary single-host drain backlog.</summary>
    public const int DrainBacklog = 10_000;

    /// <summary>Multi-host load backlog (same order of magnitude as drain; dual-worker scenario).</summary>
    public const int MultiWorkerBacklog = 10_000;

    public const int SeedChunkSize = 500;

    public const int SeedParallelism = 8;

    public const int WorkerBatchSize = 32;

    public const int WorkerMaxDegreeOfParallelism = 16;

    /// <summary>Artificial poll interval used only to compute the old throughput floor in asserts.</summary>
    public static readonly TimeSpan ReferencePollInterval = TimeSpan.FromSeconds(2);

    public static readonly TimeSpan WorkerPollInterval = TestLimits.DefaultPollInterval;

    public static readonly TimeSpan DrainTimeout = TimeSpan.FromMinutes(15);

    public static readonly TimeSpan MultiWorkerTimeout = TimeSpan.FromMinutes(20);

    public static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
}
