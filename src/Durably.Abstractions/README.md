# Durably.Abstractions

Contracts for Durably durable workflow execution, including IFlow, IStep, IFlowEngine, and IExecutionStore. Reference this package when you build a store or an integration that should not depend on the engine.

## Install

```bash
dotnet add package Durably.Abstractions
```

## Usage

Most applications get these contracts transitively through Durably.Core. Reference Abstractions directly when you implement persistence or an integration without taking a dependency on the engine.

```csharp
using Durably.Abstractions;

public sealed class MyStore : IExecutionStore
{
    // Your IExecutionStore implementation
}
```

## Companion packages

* Durably.Core for the engine and Flow.For
* Durably.Extensions.DependencyInjection for AddDurably and the hosted worker
* A persistence package for IExecutionStore

## Documentation

Full documentation is in the [Durably repository](https://github.com/mohd2sh/Durably).

## License

MIT
