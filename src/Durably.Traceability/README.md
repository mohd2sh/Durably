# Durably.Traceability

Step traces for Durably. Records outcome, duration, and optional input and output for every step through a bounded channel and a background writer, so tracing never blocks a checkpoint.

## Install

```bash
dotnet add package Durably.Traceability
```

## Usage

```csharp
builder.Services
    .AddDurably()
    .UseSqlServer(connectionString, o => o.AutoMigrate = true)
    .AddTraceability(o =>
    {
        o.CaptureInputOutput = true;
        o.CaptureExceptions = true;
        o.FlushInterval = TimeSpan.FromSeconds(1);
    })
    .AddFlow(flow);
```

Register a persistence provider that supplies ITraceStore before AddTraceability.

## Companion packages

* Durably.Extensions.DependencyInjection for AddDurably
* A persistence package that registers ITraceStore
* Durably.UI to browse traces in the dashboard

## Documentation

Full documentation is in the [Durably repository](https://github.com/mohd2sh/Durably).

## License

MIT
