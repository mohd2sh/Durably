using Xunit;

namespace Durably.Persistence.EntityFrameworkCore.IntegrationTests;

public abstract class TraceStoreScenarios<TFixture> : ProviderTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    private const string StepKey = "generate";
    private const int Attempt = 1;
    private const int DurationMs = 12;
    private const string InputJson = "{\"in\":true}";
    private const string OutputJson = "{\"out\":true}";

    protected TraceStoreScenarios(TFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Append_and_Load_round_trip()
    {
        // Arrange
        await ResetAsync();
        var traceStore = NewTraceStore();
        var timestamp = DateTimeOffset.UtcNow;
        var records = new[]
        {
            new TraceRecord
            {
                FlowName = TestConstants.FlowName,
                RunId = TestConstants.RunId,
                InstanceId = TestConstants.InstanceId,
                StepKey = StepKey,
                Attempt = Attempt,
                Outcome = TraceOutcome.Succeeded,
                InputJson = InputJson,
                OutputJson = OutputJson,
                DurationMs = DurationMs,
                Timestamp = timestamp
            }
        };

        // Act
        await traceStore.AppendAsync(records, CancellationToken.None);
        var loaded = await traceStore.LoadAsync(
            TestConstants.FlowName, TestConstants.RunId, CancellationToken.None);

        // Assert
        Assert.Single(loaded);
        Assert.Equal(StepKey, loaded[0].StepKey);
        Assert.Equal(TraceOutcome.Succeeded, loaded[0].Outcome);
        Assert.Equal(InputJson, loaded[0].InputJson);
    }

    [Fact]
    public async Task Load_empty_returns_empty_list()
    {
        // Arrange
        await ResetAsync();
        var traceStore = NewTraceStore();
        const string missingRunId = "missing-run";

        // Act
        var loaded = await traceStore.LoadAsync(
            TestConstants.FlowName, missingRunId, CancellationToken.None);

        // Assert
        Assert.Empty(loaded);
    }
}

[Collection(SqlServerEfIntegrationCollection.Name)]
public sealed class SqlServerTraceStoreScenarios : TraceStoreScenarios<SqlServerDatabaseFixture>
{
    public SqlServerTraceStoreScenarios(SqlServerDatabaseFixture database)
        : base(database)
    {
    }
}

[Collection(PostgresEfIntegrationCollection.Name)]
public sealed class PostgresTraceStoreScenarios : TraceStoreScenarios<PostgresDatabaseFixture>
{
    public PostgresTraceStoreScenarios(PostgresDatabaseFixture database)
        : base(database)
    {
    }
}
