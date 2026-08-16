# Durably.Persistence.InMemory

In memory Durably store for tests, samples, and local development. Enable it with UseInMemoryStore. State lives in the process, so use a database store in production.

## Install

```bash
dotnet add package Durably.Persistence.InMemory
```

## Usage

```csharp
builder.Services
    .AddDurably()
    .UseInMemoryStore()
    .AddFlow(flow);
```

UseInMemoryStore registers IExecutionStore, IExecutionQuery, and ITraceStore in process. Data is lost when the process exits.

## Companion packages

* Durably.Extensions.DependencyInjection for AddDurably
* Durably.Persistence.EntityFrameworkCore or Durably.Persistence.Dapper for production storage

## Documentation

Full documentation is in the [Durably repository](https://github.com/mohd2sh/Durably).

## License

MIT
