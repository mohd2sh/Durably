# Durably.Persistence.Dapper

Dapper persistence for Durably on SQL Server, PostgreSQL, and SQLite. Runs plain SQL against a connection you supply and keeps Entity Framework Core out of the dependency graph.

## Install

```bash
dotnet add package Durably.Persistence.Dapper
```

## Usage

```csharp
builder.Services
    .AddDurably()
    .UseSqlServer(connectionString)
    .AddFlow(flow);
```

Also available: UsePostgreSql (alias UsePostgres) and UseSqlite. Pass a connection string or a connection factory.

## Companion packages

* Durably.Extensions.DependencyInjection for AddDurably and the worker
* Durably.Traceability for AddTraceability
* Durably.UI for the dashboard when query services are registered

## Documentation

Full documentation is in the [Durably repository](https://github.com/mohd2sh/Durably).

## License

MIT
