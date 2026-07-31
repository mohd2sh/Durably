using System.Text.Json;

namespace Durably;

/// <summary>Global options for the Durably engine, configured via <c>AddDurably</c>.</summary>
public sealed class DurablyOptions
{
    /// <summary>Options passed to the default JSON state serializer. <c>null</c> uses general defaults.</summary>
    public JsonSerializerOptions? SerializerOptions { get; set; }

    /// <summary>Default retry policy for steps that do not call <c>.Retry(...)</c> on the flow builder.</summary>
    public RetryPolicy DefaultRetry { get; set; } = RetryPolicy.None;

    /// <summary>Default per-step timeout when a step does not call <c>.Timeout(...)</c>.</summary>
    public TimeSpan? DefaultStepTimeout { get; set; }
}
