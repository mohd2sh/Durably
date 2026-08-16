# Durably.Extensions.DependencyInjection

Dependency injection and hosting for Durably. Adds AddDurably, flow registration with AddFlow and AddFlowsFromAssembly, and a hosted worker that claims due executions under a lease.

## Install

```bash
dotnet add package Durably.Extensions.DependencyInjection
```

## Usage

```csharp
builder.Services
    .AddDurably()
    .UseInMemoryStore()
    .AddFlow(flow);
```

AddDurably registers the engine and a background worker. Call a persistence extension such as UseInMemoryStore before you start the host. Tune the worker with ConfigureWorker when needed.

## Companion packages

* Durably.Core for Flow.For and the engine types
* Durably.Persistence.InMemory, Durably.Persistence.EntityFrameworkCore, or Durably.Persistence.Dapper for storage
* Durably.Traceability for AddTraceability

## Documentation

Full documentation is in the [Durably repository](https://github.com/mohd2sh/Durably).

## License

MIT
