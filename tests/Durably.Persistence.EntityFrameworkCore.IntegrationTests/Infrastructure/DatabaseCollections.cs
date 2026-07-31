using Xunit;

namespace Durably.Persistence.EntityFrameworkCore.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class SqlServerEfIntegrationCollection : ICollectionFixture<SqlServerDatabaseFixture>
{
    public const string Name = "sql-server-ef-integration";
}

[CollectionDefinition(Name)]
public sealed class PostgresEfIntegrationCollection : ICollectionFixture<PostgresDatabaseFixture>
{
    public const string Name = "postgres-ef-integration";
}
