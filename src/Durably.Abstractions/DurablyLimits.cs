namespace Durably;

/// <summary>
/// Shared schema and runtime limits used across Durably packages. Prefer these over magic numbers.
/// </summary>
public static class DurablyLimits
{
    /// <summary>Max length for FlowName, InstanceId, RunId, FailedStep, LockedBy, StepKey columns.</summary>
    public const int IdentifierMaxLength = 200;

    /// <summary>Max length for worker runner identifiers persisted on leases.</summary>
    public const int RunnerIdMaxLength = 64;

    /// <summary>Default cap for exponential retry delay when none is supplied.</summary>
    public static readonly TimeSpan DefaultRetryMaxDelay = TimeSpan.FromMinutes(5);

    /// <summary>Jitter factor applied as <c>jitterFactor + Random()</c> (range [jitterFactor, 1+jitterFactor)).</summary>
    public const double RetryJitterFactor = 0.5;

    /// <summary>Max attempts when waiting for EF database readiness during migrate.</summary>
    public const int DatabaseMigrateMaxAttempts = 10;

    /// <summary>Initial backoff (ms) between EF migrate readiness attempts.</summary>
    public const int DatabaseMigrateInitialBackoffMilliseconds = 5000;

    /// <summary>Backoff multiplier between EF migrate readiness attempts.</summary>
    public const double DatabaseMigrateBackoffMultiplier = 1.5;

    /// <summary>PostgreSQL unique-violation SQLSTATE.</summary>
    public const string PostgresUniqueViolationSqlState = "23505";

    /// <summary>SQL fragment used as a no-op WHERE starter when composing dynamic filters.</summary>
    public const string SqlAlwaysTruePredicate = "1 = 1";

    /// <summary>In-memory store composite key separator (U+0000).</summary>
    public const char InMemoryKeySeparator = '\0';
}
