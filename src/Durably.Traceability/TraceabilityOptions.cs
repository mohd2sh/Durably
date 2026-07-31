using System.Threading.Channels;

namespace Durably;

/// <summary>Configuration for async trace capture and the background writer.</summary>
public sealed class TraceabilityOptions
{
    /// <summary>Capture serialized context before/after each step.</summary>
    public bool CaptureInputOutput { get; set; } = true;

    /// <summary>Capture exception messages on failed attempts.</summary>
    public bool CaptureExceptions { get; set; } = true;

    /// <summary>Bounded channel capacity before events are dropped (when <see cref="FullMode"/> is <c>DropWrite</c>).</summary>
    public int ChannelCapacity { get; set; } = 10_000;

    /// <summary>Behaviour when the channel is full. Only <c>DropWrite</c> is safe for the engine's synchronous <c>Emit</c>.</summary>
    public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.DropWrite;

    /// <summary>Maximum records written per database round-trip.</summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>How long the writer waits when the channel is idle before checking again.</summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(1);
}
