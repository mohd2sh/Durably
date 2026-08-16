# Durably.UI

Embeddable dashboard and JSON API for Durably. Search executions, open a single run, and read its step traces inside your own ASP.NET Core app with AddDurablyUI and MapDurablyUI.

## Install

```bash
dotnet add package Durably.UI
```

## Usage

```csharp
builder.Services.AddDurablyUI();

var app = builder.Build();
app.MapDurablyUI("/durable");
```

Default route prefix is /durable. Persistence must expose IExecutionQuery and ITraceQuery (EF Core, Dapper, or in memory). Targets .NET 6 through .NET 10.

The package ships the prebuilt dashboard inside the assembly. Your build needs no Node.js and no Angular CLI.

![Durably dashboard executions list](https://raw.githubusercontent.com/mohd2sh/Durably/main/docs/images/Dashboard%20List%20Light%20theme.png)

## Companion packages

* Durably.Extensions.DependencyInjection plus a persistence package for query services
* Durably.Traceability when you want step traces in the detail view

## Documentation

Full documentation is in the [Durably repository](https://github.com/mohd2sh/Durably).

## License

MIT
