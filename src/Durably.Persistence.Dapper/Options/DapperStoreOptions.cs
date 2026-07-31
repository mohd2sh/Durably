namespace Durably;

/// <summary>Options for the Dapper execution store.</summary>
public sealed class DapperStoreOptions
{
    /// <summary>Run the idempotent schema bootstrap on first use. Off by default; ship the script in production.</summary>
    public bool EnsureSchema { get; set; }

    /// <summary>Per-command timeout in seconds.</summary>
    public int CommandTimeoutSeconds { get; set; } = 30;
}
