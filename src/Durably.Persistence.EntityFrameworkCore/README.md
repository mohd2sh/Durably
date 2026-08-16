# Durably.Persistence.EntityFrameworkCore

Entity Framework Core persistence for Durably on SQL Server, PostgreSQL, and SQLite. Provides the execution store, the trace store, and the query services used by the worker and the dashboard, with optional schema migration on startup.

## Install

```bash
dotnet add package Durably.Persistence.EntityFrameworkCore
```

## Usage

```csharp
builder.Services
    .AddDurably()
    .UseSqlServer(connectionString, o => o.AutoMigrate = true)
    .AddFlow(flow);
```

Also available: UsePostgres and UseSqlite. Targets .NET 6 through .NET 10.

## Companion packages

* Durably.Extensions.DependencyInjection for AddDurably and the worker
* Durably.Traceability for AddTraceability
* Durably.UI for the dashboard (needs IExecutionQuery and ITraceQuery from this package)

## Documentation

Full documentation is in the [Durably repository](https://github.com/mohd2sh/Durably).

## License

MIT
