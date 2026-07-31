namespace Durably;

/// <summary>Options for the EF Core execution store.</summary>
public sealed class EfStoreOptions
{
    /// <summary>Apply pending EF migrations on first store use. Off by default in production.</summary>
    public bool AutoMigrate { get; set; }

    /// <summary>Database schema for Durably tables. Default: <c>durable</c>.</summary>
    public string Schema { get; set; } = "durable";

    /// <summary>Per-command timeout in seconds.</summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Connection string used as the AutoMigrate cache key. Set by provider registration so
    /// <c>EnsureReadyAsync</c> can skip creating a probe <c>DbContext</c> after the first migrate.
    /// </summary>
    internal string? ConnectionString { get; set; }
}
