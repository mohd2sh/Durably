using System.Collections.Concurrent;

namespace Durably.Core.UnitTests;

public sealed class CollectingTraceSink : ITraceSink
{
    public ConcurrentBag<TraceRecord> Records { get; } = new();

    public void Emit(TraceRecord record) => Records.Add(record);
}
