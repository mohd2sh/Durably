using Xunit;

namespace Durably.E2E.Tests;

[CollectionDefinition(Name)]
public sealed class SqlServerE2ECollection : ICollectionFixture<SqlServerDatabaseFixture>
{
    public const string Name = "sql-server-e2e";
}

[CollectionDefinition(Name)]
public sealed class PostgresE2ECollection : ICollectionFixture<PostgresDatabaseFixture>
{
    public const string Name = "postgres-e2e";
}
