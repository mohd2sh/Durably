# Sample.AspNetCore.Api

.NET 8 Web API sample showing Durably with DI, SQL Server persistence, traceability, OOP flows/steps, hooks, branching, and the embeddable Durably UI.

## Prerequisites

- **Aspire (recommended):** [Docker Desktop](https://www.docker.com/products/docker-desktop/) for the SQL Server container
- **Standalone:** SQL Server LocalDB (Windows) or update `ConnectionStrings:Durable` in `appsettings.Development.json`

## Run

| Command | Database |
|---------|----------|
| `dotnet run --project samples/Sample.AspNetCore.AppHost` | SQL Server **container** (Aspire dashboard opens automatically) |
| `dotnet run --project samples/Sample.AspNetCore.Api` | **LocalDB** fallback (no Docker required) |

When launched via AppHost, Aspire injects `ConnectionStrings:durable` into the API. When run standalone, the API uses `ConnectionStrings:Durable` from config, then falls back to LocalDB.

### Aspire AppHost

```bash
dotnet run --project samples/Sample.AspNetCore.AppHost
```

Open the Aspire dashboard URL shown in the console, then the API Swagger at the `api` resource endpoint.

### Standalone API

```bash
dotnet run --project samples/Sample.AspNetCore.Api
```

Open Swagger at `https://localhost:7xxx/swagger` (port shown in console).

## Durably UI

Open `/durable` for execution search, step graph, and trace detail. No login required. Durably UI routes are excluded from Swagger.

## What this demonstrates

- `AddDurably()` + `UseSqlServer(..., AutoMigrate = true)` + `AddTraceability()` + `AddFlowsFromAssembly()`
- Thin composition root: `Program.cs` calls `AddSampleApplication()` (handlers/services live in their own files)
- `StartAsync` enqueues (`Pending`); background worker processes; `GetStatusAsync` for polling
- Global retry defaults and per-step `.Retry(...)` on finalize email
- **DI hooks:** `IFlowSuccessHandler` / `IFlowFailureHandler` on `OrderFinalizeFlow`
- **Builder hooks:** `.OnSuccess` / `.OnFailure` on `OrderFulfillmentFlow`
- **Branching:** `StepIf` (fraud check when `Total >= 500`) and `Choose`/`When`/`Otherwise` by `Channel`

## Try it

### Finalize (linear flow + DI handlers)

1. **Happy path** — `POST /api/orders/order-1/finalize`:

```json
{
  "customerEmail": "user@example.com",
  "total": 99.99
}
```

Returns **202 Accepted**. Poll `GET /api/orders/order-1/status`. Watch logs for `OrderFinalizeSuccessHandler`.

2. **Failure** — trigger a fail-once email, then finalize:

```text
POST /api/orders/order-2/simulate-email-failure
POST /api/orders/order-2/finalize
```

Poll status until `Failed`. Logs show `OrderFinalizeFailureHandler`. (Failed is terminal for the worker; re-queue is out of scope for this sample.)

### Fulfill (StepIf + Choose + builder hooks)

`POST /api/orders/{id}/fulfill` — returns **202**. Poll `GET /api/orders/{id}/fulfill/status`.

**High-value express** (runs fraud check + express branch):

```json
{
  "customerEmail": "user@example.com",
  "total": 750,
  "channel": "express"
}
```

**Low-value standard** (skips fraud check, standard branch):

```json
{
  "customerEmail": "user@example.com",
  "total": 49.99,
  "channel": "standard"
}
```

**Digital / other channel** (`Otherwise` branch):

```json
{
  "customerEmail": "user@example.com",
  "total": 19.99,
  "channel": "digital"
}
```

`Channel` values: `express`, `standard`, or anything else (treated as digital via `Otherwise`).
