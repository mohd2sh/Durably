using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Durably;

/// <summary>Library-friendly worker metrics via <see cref="System.Diagnostics.Metrics"/>.</summary>
internal static class DurablyWorkerMetrics
{
    private static readonly Meter Meter = new("Durably.Worker", "1.0.0");

    private static readonly Counter<long> ClaimsTotal = Meter.CreateCounter<long>("durably.worker.claims");
    private static readonly Counter<long> ClaimedRecords = Meter.CreateCounter<long>("durably.worker.claimed_records");
    private static readonly Counter<long> EmptyPolls = Meter.CreateCounter<long>("durably.worker.empty_polls");
    private static readonly Counter<long> FullBatches = Meter.CreateCounter<long>("durably.worker.full_batches");
    private static readonly Counter<long> ProcessFailures = Meter.CreateCounter<long>("durably.worker.process_failures");
    private static readonly Counter<long> LeaseLosses = Meter.CreateCounter<long>("durably.worker.lease_losses");
    private static readonly Histogram<double> ClaimDurationMs = Meter.CreateHistogram<double>("durably.worker.claim_duration_ms");

    public static void RecordClaim(TimeSpan duration, int claimedCount, int batchSize)
    {
        ClaimsTotal.Add(1);
        ClaimDurationMs.Record(duration.TotalMilliseconds);
        if (claimedCount == 0)
        {
            EmptyPolls.Add(1);
            return;
        }

        ClaimedRecords.Add(claimedCount);
        if (claimedCount >= batchSize)
        {
            FullBatches.Add(1);
        }
    }

    public static void RecordProcessFailure() => ProcessFailures.Add(1);

    public static void RecordLeaseLoss() => LeaseLosses.Add(1);
}
