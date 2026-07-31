using Xunit;

namespace Durably.Persistence.EntityFrameworkCore.IntegrationTests;

public abstract class ExecutionStoreScenarios<TFixture> : ProviderTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected ExecutionStoreScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Create_Load_Checkpoint_increments_version()
    {
        // Arrange
        await ResetAsync();
        var store = NewStore();
        var record = ExecutionRecordFactory.Running(
            TestConstants.FlowName,
            TestConstants.InstanceId,
            TestConstants.ContextWithValue);
        var leaseUntil = DateTimeOffset.UtcNow.Add(TestConstants.LeaseDuration);
        const int expectedStepAfterCheckpoint = 1;
        const long expectedVersionAfterCheckpoint = 1;

        // Act
        await store.CreateAsync(record, CancellationToken.None);
        Assert.True(await store.TryAcquireLeaseAsync(
            TestConstants.FlowName, TestConstants.InstanceId, TestConstants.RunnerId, leaseUntil, CancellationToken.None));

        var loaded = await store.LoadAsync(TestConstants.FlowName, TestConstants.InstanceId, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(0, loaded!.Version);
        Assert.Equal(TestConstants.ContextWithValue, loaded.ContextJson);

        loaded.CurrentStep = expectedStepAfterCheckpoint;
        loaded.Status = ExecutionStatus.Completed;
        await store.SaveCheckpointAsync(loaded, TestConstants.RunnerId, leaseUntil, CancellationToken.None);

        var reloaded = await store.LoadAsync(TestConstants.FlowName, TestConstants.InstanceId, CancellationToken.None);

        // Assert
        Assert.Equal(expectedStepAfterCheckpoint, reloaded!.CurrentStep);
        Assert.Equal(expectedVersionAfterCheckpoint, reloaded.Version);
        Assert.Equal(ExecutionStatus.Completed, reloaded.Status);
    }

    [Fact]
    public async Task CreateAsync_duplicate_throws_ExecutionAlreadyExistsException()
    {
        // Arrange
        await ResetAsync();
        var store = NewStore();
        var record = ExecutionRecordFactory.Running(TestConstants.FlowName, TestConstants.InstanceId);
        await store.CreateAsync(record, CancellationToken.None);

        // Act / Assert
        await Assert.ThrowsAsync<ExecutionAlreadyExistsException>(() =>
            store.CreateAsync(
                ExecutionRecordFactory.Running(TestConstants.FlowName, TestConstants.InstanceId),
                CancellationToken.None));
    }

    [Fact]
    public async Task SaveCheckpointAsync_detects_optimistic_concurrency_conflict()
    {
        // Arrange
        await ResetAsync();
        var store = NewStore();
        var leaseUntil = DateTimeOffset.UtcNow.Add(TestConstants.LeaseDuration);
        await store.CreateAsync(
            ExecutionRecordFactory.Running(TestConstants.FlowName, TestConstants.InstanceId),
            CancellationToken.None);
        Assert.True(await store.TryAcquireLeaseAsync(
            TestConstants.FlowName, TestConstants.InstanceId, TestConstants.RunnerId, leaseUntil, CancellationToken.None));

        var first = await store.LoadAsync(TestConstants.FlowName, TestConstants.InstanceId, CancellationToken.None);
        var second = await store.LoadAsync(TestConstants.FlowName, TestConstants.InstanceId, CancellationToken.None);
        first!.CurrentStep = 1;
        await store.SaveCheckpointAsync(first, TestConstants.RunnerId, leaseUntil, CancellationToken.None);

        // Act / Assert
        second!.CurrentStep = 2;
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            store.SaveCheckpointAsync(second, TestConstants.RunnerId, leaseUntil, CancellationToken.None));
    }

    [Fact]
    public async Task SaveCheckpointAsync_without_lease_throws_LeaseLostException()
    {
        // Arrange
        await ResetAsync();
        var store = NewStore();
        var leaseUntil = DateTimeOffset.UtcNow.Add(TestConstants.LeaseDuration);
        await store.CreateAsync(
            ExecutionRecordFactory.Running(TestConstants.FlowName, TestConstants.InstanceId),
            CancellationToken.None);
        Assert.True(await store.TryAcquireLeaseAsync(
            TestConstants.FlowName, TestConstants.InstanceId, TestConstants.RunnerId, leaseUntil, CancellationToken.None));
        var record = await store.LoadAsync(TestConstants.FlowName, TestConstants.InstanceId, CancellationToken.None);
        await store.ReleaseLeaseAsync(
            TestConstants.FlowName, TestConstants.InstanceId, TestConstants.RunnerId, CancellationToken.None);

        // Act / Assert
        await Assert.ThrowsAsync<LeaseLostException>(() =>
            store.SaveCheckpointAsync(record!, TestConstants.OtherRunnerId, leaseUntil, CancellationToken.None));
    }

    [Fact]
    public async Task TryAcquireLease_blocks_other_runner_until_expired_or_released()
    {
        // Arrange
        await ResetAsync();
        var store = NewStore();
        var leaseUntil = DateTimeOffset.UtcNow.Add(TestConstants.LeaseDuration);
        await store.CreateAsync(
            ExecutionRecordFactory.Running(TestConstants.FlowName, TestConstants.InstanceId),
            CancellationToken.None);

        // Act
        var firstAcquired = await store.TryAcquireLeaseAsync(
            TestConstants.FlowName, TestConstants.InstanceId, TestConstants.RunnerId, leaseUntil, CancellationToken.None);
        var secondBlocked = await store.TryAcquireLeaseAsync(
            TestConstants.FlowName, TestConstants.InstanceId, TestConstants.OtherRunnerId, leaseUntil, CancellationToken.None);

        await store.ReleaseLeaseAsync(
            TestConstants.FlowName, TestConstants.InstanceId, TestConstants.RunnerId, CancellationToken.None);
        var afterRelease = await store.TryAcquireLeaseAsync(
            TestConstants.FlowName, TestConstants.InstanceId, TestConstants.OtherRunnerId, leaseUntil, CancellationToken.None);

        // Assert
        Assert.True(firstAcquired);
        Assert.False(secondBlocked);
        Assert.True(afterRelease);
    }

    [Fact]
    public async Task TryAcquireLease_reclaims_expired_lease()
    {
        // Arrange
        await ResetAsync();
        var store = NewStore();
        var expiredLease = DateTimeOffset.UtcNow.AddMinutes(-5);
        var freshLease = DateTimeOffset.UtcNow.Add(TestConstants.LeaseDuration);
        await store.CreateAsync(
            ExecutionRecordFactory.Running(TestConstants.FlowName, TestConstants.InstanceId),
            CancellationToken.None);
        Assert.True(await store.TryAcquireLeaseAsync(
            TestConstants.FlowName, TestConstants.InstanceId, TestConstants.RunnerId, expiredLease, CancellationToken.None));

        // Act
        var reclaimed = await store.TryAcquireLeaseAsync(
            TestConstants.FlowName, TestConstants.InstanceId, TestConstants.OtherRunnerId, freshLease, CancellationToken.None);

        // Assert
        Assert.True(reclaimed);
        var loaded = await store.LoadAsync(TestConstants.FlowName, TestConstants.InstanceId, CancellationToken.None);
        Assert.Equal(TestConstants.OtherRunnerId, loaded!.LockedBy);
    }

    [Fact]
    public async Task ClaimDueAsync_picks_Pending_and_expired_Running_excludes_Failed_Completed()
    {
        // Arrange
        await ResetAsync();
        var store = NewStore();
        var now = DateTimeOffset.UtcNow;
        const string pendingFlow = "pending-flow";
        const string runningFlow = "running-flow";
        const string failedFlow = "failed-flow";
        const string completedFlow = "completed-flow";
        const int batchSize = 10;

        await store.CreateAsync(ExecutionRecordFactory.Create(pendingFlow, "p1", ExecutionStatus.Pending, timestamp: now), CancellationToken.None);
        await store.CreateAsync(ExecutionRecordFactory.Create(runningFlow, "r1", ExecutionStatus.Running, timestamp: now), CancellationToken.None);
        await store.CreateAsync(ExecutionRecordFactory.Create(failedFlow, "f1", ExecutionStatus.Failed, timestamp: now), CancellationToken.None);
        await store.CreateAsync(ExecutionRecordFactory.Create(completedFlow, "c1", ExecutionStatus.Completed, timestamp: now), CancellationToken.None);

        var expiredLease = now.AddMinutes(-2);
        Assert.True(await store.TryAcquireLeaseAsync(runningFlow, "r1", TestConstants.RunnerId, expiredLease, CancellationToken.None));

        var claimUntil = now.Add(TestConstants.LeaseDuration);

        // Act
        var claimed = await store.ClaimDueAsync(TestConstants.OtherRunnerId, claimUntil, batchSize, CancellationToken.None);

        // Assert
        Assert.Contains(claimed, r => r.FlowName == pendingFlow);
        Assert.Contains(claimed, r => r.FlowName == runningFlow);
        Assert.DoesNotContain(claimed, r => r.FlowName == failedFlow);
        Assert.DoesNotContain(claimed, r => r.FlowName == completedFlow);
    }

    [Fact]
    public async Task ClaimDueAsync_respects_batch_size()
    {
        // Arrange
        await ResetAsync();
        var store = NewStore();
        const int totalPending = 5;
        const int batchSize = 2;
        for (var i = 0; i < totalPending; i++)
        {
            await store.CreateAsync(
                ExecutionRecordFactory.Create(TestConstants.FlowName, $"batch-{i}", ExecutionStatus.Pending),
                CancellationToken.None);
        }

        var leaseUntil = DateTimeOffset.UtcNow.Add(TestConstants.LeaseDuration);

        // Act
        var claimed = await store.ClaimDueAsync(TestConstants.RunnerId, leaseUntil, batchSize, CancellationToken.None);

        // Assert
        Assert.Equal(batchSize, claimed.Count);
    }

    [Fact]
    public async Task ClaimDueAsync_concurrent_runners_get_disjoint_sets()
    {
        await ResetAsync();
        var store = NewStore();
        const int total = 40;
        const int batchSize = 10;
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < total; i++)
        {
            await store.CreateAsync(
                ExecutionRecordFactory.Create(TestConstants.FlowName, $"claim-{i:D3}", ExecutionStatus.Pending, timestamp: now.AddMilliseconds(i)),
                CancellationToken.None);
        }

        var leaseUntil = DateTimeOffset.UtcNow.Add(TestConstants.LeaseDuration);
        var first = store.ClaimDueAsync(TestConstants.RunnerId, leaseUntil, batchSize, CancellationToken.None);
        var second = store.ClaimDueAsync(TestConstants.OtherRunnerId, leaseUntil, batchSize, CancellationToken.None);
        await Task.WhenAll(first, second);

        var a = await first;
        var b = await second;
        Assert.Equal(batchSize, a.Count);
        Assert.Equal(batchSize, b.Count);
        var keys = a.Concat(b).Select(r => r.InstanceId).ToList();
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Respawn_reset_clears_executions()
    {
        // Arrange
        await ResetAsync();
        var store = NewStore();
        await store.CreateAsync(
            ExecutionRecordFactory.Running(TestConstants.FlowName, TestConstants.InstanceId),
            CancellationToken.None);

        // Act
        await Database.ResetAsync();
        var afterReset = await store.LoadAsync(TestConstants.FlowName, TestConstants.InstanceId, CancellationToken.None);

        // Assert
        Assert.Null(afterReset);
    }
}

[Collection(SqlServerEfIntegrationCollection.Name)]
public sealed class SqlServerExecutionStoreScenarios : ExecutionStoreScenarios<SqlServerDatabaseFixture>
{
    public SqlServerExecutionStoreScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresEfIntegrationCollection.Name)]
public sealed class PostgresExecutionStoreScenarios : ExecutionStoreScenarios<PostgresDatabaseFixture>
{
    public PostgresExecutionStoreScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
