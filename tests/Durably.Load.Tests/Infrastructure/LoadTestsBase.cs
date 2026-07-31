namespace Durably.Load.Tests;

public abstract class LoadTestsBase<TFixture> : ScenarioTestsBase<TFixture>
    where TFixture : IDatabaseFixture
{
    protected LoadTestsBase(TFixture database)
        : base(database)
    {
    }
}
