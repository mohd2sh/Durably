using Xunit;

namespace Durably.E2E.Tests.TestScenarios;

public abstract class StoreScenarios<TFixture> : ScenarioTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected StoreScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Store_round_trips_execution_and_respawn_resets_provider_database()
    {
        await ResetAsync();
        var store = NewExecutionStore();
        var record = new ExecutionRecord
        {
            FlowName = "round-trip",
            InstanceId = "one",
            Status = ExecutionStatus.Running,
            CurrentStep = 0,
            ContextJson = "{\"value\":1}",
            Attempts = 0,
            Version = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await store.CreateAsync(record, default);

        var leaseUntil = DateTimeOffset.UtcNow.Add(TestLimits.DefaultLeaseDuration);
        const string runner = "e2e-runner";
        Assert.True(await store.TryAcquireLeaseAsync("round-trip", "one", runner, leaseUntil, default));

        var loaded = await store.LoadAsync("round-trip", "one", default);
        Assert.NotNull(loaded);
        Assert.Equal("{\"value\":1}", loaded!.ContextJson);
        Assert.Equal(0, loaded.Version);

        loaded.CurrentStep = 1;
        loaded.Status = ExecutionStatus.Completed;
        await store.SaveCheckpointAsync(loaded, runner, leaseUntil, default);

        var reloaded = await store.LoadAsync("round-trip", "one", default);
        Assert.Equal(1, reloaded!.CurrentStep);
        Assert.Equal(1, reloaded.Version);
        Assert.Equal(ExecutionStatus.Completed, reloaded.Status);

        await Database.ResetAsync();
        var afterReset = await store.LoadAsync("round-trip", "one", default);
        Assert.Null(afterReset);
    }

    [Fact]
    public async Task Checkpoint_without_valid_lease_throws_LeaseLost()
    {
        await ResetAsync();
        var store = NewExecutionStore();
        var now = DateTimeOffset.UtcNow;
        await store.CreateAsync(new ExecutionRecord
        {
            FlowName = "lease-lost",
            InstanceId = "one",
            Status = ExecutionStatus.Running,
            CurrentStep = 0,
            ContextJson = "{}",
            Attempts = 0,
            Version = 0,
            CreatedAt = now,
            UpdatedAt = now
        }, default);

        var leaseUntil = DateTimeOffset.UtcNow.Add(TestLimits.DefaultLeaseDuration);
        Assert.True(await store.TryAcquireLeaseAsync("lease-lost", "one", "owner", leaseUntil, default));

        var record = await store.LoadAsync("lease-lost", "one", default);
        await store.ReleaseLeaseAsync("lease-lost", "one", "owner", default);

        await Assert.ThrowsAsync<LeaseLostException>(() =>
            store.SaveCheckpointAsync(record!, "other-runner", leaseUntil, default));
    }
}

[Collection(SqlServerE2ECollection.Name)]
public sealed class SqlServerStoreScenarios : StoreScenarios<SqlServerDatabaseFixture>
{
    public SqlServerStoreScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresE2ECollection.Name)]
public sealed class PostgresStoreScenarios : StoreScenarios<PostgresDatabaseFixture>
{
    public PostgresStoreScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
