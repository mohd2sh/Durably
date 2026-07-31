using Xunit;

namespace Durably.Load.Tests;

[CollectionDefinition(Name)]
public sealed class SqlServerLoadCollection : ICollectionFixture<SqlServerDatabaseFixture>
{
    public const string Name = "sql-server-load";
}

[CollectionDefinition(Name)]
public sealed class PostgresLoadCollection : ICollectionFixture<PostgresDatabaseFixture>
{
    public const string Name = "postgres-load";
}
