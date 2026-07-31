using Xunit;

namespace Durably.Core.UnitTests.Execution;
public sealed class InMemoryExecutionStoreTests
{
    private const string FlowName = "orders";
    private const string InstanceId = "instance-1";
    private const string RunnerId = "runner-a";
    private const string OtherRunnerId = "runner-b";
    private const string EmptyContextJson = "{}";
    private static readonly TimeSpan LeaseDuration = TestLimits.DefaultLeaseDuration;

    [Fact]
    public async Task CreateAsync_duplicate_throws_ExecutionAlreadyExistsException()
    {
        // Arrange
        var store = new InMemoryExecutionStore();
        var record = CreatePendingRecord(FlowName, InstanceId);
        await store.CreateAsync(record, CancellationToken.None);

        // Act / Assert
        await Assert.ThrowsAsync<ExecutionAlreadyExistsException>(() =>
            store.CreateAsync(CreatePendingRecord(FlowName, InstanceId), CancellationToken.None));
    }

    [Fact]
    public async Task SaveCheckpointAsync_increments_version()
    {
        // Arrange
        var store = new InMemoryExecutionStore();
        await store.CreateAsync(CreatePendingRecord(FlowName, InstanceId), CancellationToken.None);
        var leaseUntil = DateTimeOffset.UtcNow.Add(LeaseDuration);
        Assert.True(await store.TryAcquireLeaseAsync(FlowName, InstanceId, RunnerId, leaseUntil, CancellationToken.None));

        var loaded = await store.LoadAsync(FlowName, InstanceId, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(0, loaded!.Version);
        loaded.CurrentStep = 1;
        loaded.Status = ExecutionStatus.Completed;

        // Act
        await store.SaveCheckpointAsync(loaded, RunnerId, leaseUntil, CancellationToken.None);

        // Assert
        Assert.Equal(1, loaded.Version);
        var reloaded = await store.LoadAsync(FlowName, InstanceId, CancellationToken.None);
        Assert.Equal(1, reloaded!.Version);
        Assert.Equal(ExecutionStatus.Completed, reloaded.Status);
    }

    [Fact]
    public async Task SaveCheckpointAsync_without_lease_throws_LeaseLostException()
    {
        // Arrange
        var store = new InMemoryExecutionStore();
        await store.CreateAsync(CreatePendingRecord(FlowName, InstanceId), CancellationToken.None);
        var leaseUntil = DateTimeOffset.UtcNow.Add(LeaseDuration);
        Assert.True(await store.TryAcquireLeaseAsync(FlowName, InstanceId, RunnerId, leaseUntil, CancellationToken.None));
        var record = await store.LoadAsync(FlowName, InstanceId, CancellationToken.None);
        await store.ReleaseLeaseAsync(FlowName, InstanceId, RunnerId, CancellationToken.None);

        // Act / Assert
        await Assert.ThrowsAsync<LeaseLostException>(() =>
            store.SaveCheckpointAsync(record!, OtherRunnerId, leaseUntil, CancellationToken.None));
    }

    [Fact]
    public async Task ClaimDueAsync_includes_Pending_and_expired_Running_excludes_Failed_and_Completed()
    {
        // Arrange
        var store = new InMemoryExecutionStore();
        var now = DateTimeOffset.UtcNow;
        const string pendingFlow = "pending-flow";
        const string runningExpiredFlow = "running-expired";
        const string failedFlow = "failed-flow";
        const string completedFlow = "completed-flow";

        await store.CreateAsync(CreateRecord(pendingFlow, "p1", ExecutionStatus.Pending, now), CancellationToken.None);
        await store.CreateAsync(CreateRecord(runningExpiredFlow, "r1", ExecutionStatus.Running, now), CancellationToken.None);
        await store.CreateAsync(CreateRecord(failedFlow, "f1", ExecutionStatus.Failed, now), CancellationToken.None);
        await store.CreateAsync(CreateRecord(completedFlow, "c1", ExecutionStatus.Completed, now), CancellationToken.None);

        var expiredLease = now.AddMinutes(-5);
        Assert.True(await store.TryAcquireLeaseAsync(
            runningExpiredFlow, "r1", RunnerId, expiredLease, CancellationToken.None));

        var claimLeaseUntil = now.Add(LeaseDuration);
        const int batchSize = 10;

        // Act
        var claimed = await store.ClaimDueAsync(OtherRunnerId, claimLeaseUntil, batchSize, CancellationToken.None);

        // Assert
        Assert.Contains(claimed, r => r.FlowName == pendingFlow && r.InstanceId == "p1");
        Assert.Contains(claimed, r => r.FlowName == runningExpiredFlow && r.InstanceId == "r1");
        Assert.DoesNotContain(claimed, r => r.Status == ExecutionStatus.Failed);
        Assert.DoesNotContain(claimed, r => r.Status == ExecutionStatus.Completed);
    }

    [Fact]
    public async Task ClaimDueAsync_concurrent_runners_get_disjoint_sets()
    {
        // Arrange
        var store = new InMemoryExecutionStore();
        const int total = 40;
        const int batchSize = 10;
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < total; i++)
        {
            await store.CreateAsync(
                CreateRecord(FlowName, $"i-{i:D3}", ExecutionStatus.Pending, now.AddMilliseconds(i)),
                CancellationToken.None);
        }

        var leaseUntil = now.Add(LeaseDuration);

        // Act
        var a = await store.ClaimDueAsync(RunnerId, leaseUntil, batchSize, CancellationToken.None);
        var b = await store.ClaimDueAsync(OtherRunnerId, leaseUntil, batchSize, CancellationToken.None);

        // Assert
        Assert.Equal(batchSize, a.Count);
        Assert.Equal(batchSize, b.Count);
        var keys = a.Concat(b).Select(r => r.InstanceId).ToList();
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task ReleaseLeaseAsync_clears_lock()
    {
        // Arrange
        var store = new InMemoryExecutionStore();
        await store.CreateAsync(CreatePendingRecord(FlowName, InstanceId), CancellationToken.None);
        var leaseUntil = DateTimeOffset.UtcNow.Add(LeaseDuration);
        Assert.True(await store.TryAcquireLeaseAsync(FlowName, InstanceId, RunnerId, leaseUntil, CancellationToken.None));

        // Act
        await store.ReleaseLeaseAsync(FlowName, InstanceId, RunnerId, CancellationToken.None);

        // Assert
        var loaded = await store.LoadAsync(FlowName, InstanceId, CancellationToken.None);
        Assert.Null(loaded!.LockedBy);
        Assert.Null(loaded.LockedUntil);
        Assert.True(await store.TryAcquireLeaseAsync(FlowName, InstanceId, OtherRunnerId, leaseUntil, CancellationToken.None));
    }

    private static ExecutionRecord CreatePendingRecord(string flowName, string instanceId)
        => CreateRecord(flowName, instanceId, ExecutionStatus.Pending, DateTimeOffset.UtcNow);

    private static ExecutionRecord CreateRecord(
        string flowName,
        string instanceId,
        ExecutionStatus status,
        DateTimeOffset timestamp)
        => new()
        {
            FlowName = flowName,
            InstanceId = instanceId,
            Status = status,
            CurrentStep = 0,
            ContextJson = EmptyContextJson,
            Attempts = 0,
            Version = 0,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
}
