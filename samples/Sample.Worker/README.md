# Sample.Worker

.NET 10 Generic Host sample showing **fluent** Durably registration (`Flow.For` + `AddFlow`) with `IStep` classes. Requires the .NET 10 SDK.

For a pure fluent + **lambda** (no `IStep`) example in the API, see [`../Sample.AspNetCore.Api/Workflows/Fluent`](../Sample.AspNetCore.Api/Workflows/Fluent). OOP `IFlow` demos live under [`../Sample.AspNetCore.Api/Workflows/Oop`](../Sample.AspNetCore.Api/Workflows/Oop).

## What it demonstrates

- Fluent flow definition with per-step retry
- Enqueue via `IFlowEngine.StartAsync` from a background service
- Library-hosted `DurablyWorkerService` claims Pending work with leases
- Resume after a simulated one-shot email failure on `order-2`
- `IStepContext.IdempotencyKey` on the email step

## Persistence

| How you run | Store |
|-------------|--------|
| `dotnet run --project samples/Sample.AspNetCore.AppHost` | EF PostgreSQL **container** (Aspire injects `ConnectionStrings:worker`) |
| `dotnet run --project samples/Sample.Worker` | **InMemory** by default (`Durably:Store=InMemory`) |

Optional: set `Durably:Store=Postgres` and `ConnectionStrings:Durable` (or `worker`) to point at your own Postgres.

## Run

```bash
# Recommended: with API + SQL Server + Postgres via Aspire
dotnet run --project samples/Sample.AspNetCore.AppHost

# Zero deps
dotnet run --project samples/Sample.Worker
```

## Behaviour

1. Every few seconds the sample enqueues `order-1` and `order-2` (idempotent while a run is open).
2. `order-1` completes: generate-report → send-email → finalize.
3. `order-2` fails once at send-email, then resumes on the next worker claim; generate-report is not re-executed.
