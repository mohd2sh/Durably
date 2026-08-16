# Sample.Worker

.NET 10 Generic Host sample showing **fluent** Durably registration (`Flow.For` + `AddFlow`) with `IStep` classes. Requires the .NET 10 SDK.

For a pure fluent + **lambda** (no `IStep`) example in the API, see [`../Sample.AspNetCore.Api/Workflows/Fluent`](../Sample.AspNetCore.Api/Workflows/Fluent). OOP `IFlow` demos live under [`../Sample.AspNetCore.Api/Workflows/Oop`](../Sample.AspNetCore.Api/Workflows/Oop).

## What it demonstrates

- Fluent flow definition with per-step retry
- Library-hosted `DurablyWorkerService` claims Pending work with leases
- Optional demo enqueue loop (`OrderFinalizeWorker`) when you configure order ids
- `IStepContext.IdempotencyKey` on the email step

## Persistence

| How you run | Store |
|-------------|--------|
| `dotnet run --project samples/Sample.AspNetCore.AppHost` | EF PostgreSQL **container** — AppHost sets `Durably__Store=Postgres` and injects `ConnectionStrings:worker` |
| `dotnet run --project samples/Sample.Worker` | **InMemory** from `appsettings.json` (`Durably:Store=InMemory`) |

Optional without Aspire: set `Durably:Store=Postgres` and `ConnectionStrings:Durable` (or `worker`).

## Run

```bash
# Recommended: with API + Postgres via Aspire
dotnet run --project samples/Sample.AspNetCore.AppHost

# Zero deps
dotnet run --project samples/Sample.Worker
```

## No auto-enqueue by default

`Worker:PendingOrderIds` is omitted/empty, so `OrderFinalizeWorker` is **not** registered. Nothing is started until you add ids (or enqueue from your own code).

To opt into the demo loop, set in `appsettings.json`:

```json
"Worker": {
  "PollIntervalSeconds": 5,
  "PendingOrderIds": [ "order-1", "order-2" ]
}
```

Then the background service periodically calls `StartAsync` for those ids.