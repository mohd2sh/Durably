# Durably.Core

The Durably workflow engine for .NET. Define a flow as ordinary C# steps with Flow.For, checkpoint state after every step, and resume from the last completed step after a crash or a restart.

## Install

```bash
dotnet add package Durably.Core
```

## Usage

```csharp
var flow = Flow.For<OrderFinalizeState>()
    .Step<GenerateReportStep>()
    .Step<SendEmailStep>()
    .Step<FinalizeOrderStep>();
```

Pair Core with Durably.Extensions.DependencyInjection and a store. Start instances with IFlowEngine.StartAsync. The hosted worker claims due work and checkpoints after each success.

## Companion packages

* Durably.Extensions.DependencyInjection for AddDurably, AddFlow, and the worker
* Durably.Persistence.InMemory or Durably.Persistence.EntityFrameworkCore for storage
* Durably.Traceability and Durably.UI when you want step traces and a dashboard

## Documentation

Full documentation is in the [Durably repository](https://github.com/mohd2sh/Durably).

## License

MIT
