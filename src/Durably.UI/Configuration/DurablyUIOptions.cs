namespace Durably;

/// <summary>Configuration for the embeddable Durably observability UI.</summary>
public sealed class DurablyUIOptions
{
    /// <summary>Route prefix for the SPA and JSON API. Default: <see cref="DurablyUIDefaults.RoutePrefix"/>.</summary>
    public string RoutePrefix { get; set; } = DurablyUIDefaults.RoutePrefix;

    /// <summary>When true, action endpoints (e.g. manual resume) may be exposed. Reserved for a future release.</summary>
    public bool AllowActions { get; set; }
}
