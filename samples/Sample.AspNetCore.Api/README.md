# Sample.AspNetCore.Api

.NET 10 Web API showcase: business workflows organized by **authoring design**, Durably UI, Traceability, and store selection (Aspire Postgres or InMemory). Requires the .NET 10 SDK.

## Explore by design

```
Workflows/
  Oop/       ← IFlow + IStep  (AddFlowsFromAssembly)
  Fluent/    ← Flow.For + lambdas (AddFlow)
Controllers/
  Oop/
  Fluent/
```

| Style | Folder | Registration | Controllers |
|-------|--------|--------------|-------------|
| OOP | `Workflows/Oop/` | `AddFlowsFromAssembly` | `Controllers/Oop/` |
| Fluent / lambda | `Workflows/Fluent/` | `AddFlow` | `Controllers/Fluent/` |

Shared fakes live in `Services/` (not under a style folder).

## Persistence

| How you run | Store |
|-------------|--------|
| `dotnet run --project samples/Sample.AspNetCore.AppHost` | EF PostgreSQL **container** — AppHost sets `Durably__Store=Postgres` and injects `ConnectionStrings:durable` |
| `dotnet run --project samples/Sample.AspNetCore.Api` | **InMemory** from `appsettings.json` (`Durably:Store=InMemory`) |

Optional without Aspire: set `Durably:Store=Postgres` (or `SqlServer`) and `ConnectionStrings:Durable` (env or config).

The API does **not** import or seed sample runs. The Durably UI and hosted worker only process instances you start (Swagger, curl, or your own clients). If old executions keep showing under AppHost, they are leftover rows in the Aspire Postgres volume `durably-pg-data` — remove that Docker volume (or reset the database) to clear them. An unused leftover SQL volume `durably-sql-data` from older AppHost runs can be deleted too.

## Run

```bash
# Docker Desktop required — api waits for the Postgres durable DB (WaitFor)
dotnet run --project samples/Sample.AspNetCore.AppHost
# or
dotnet run --project samples/Sample.AspNetCore.Api
```

- Swagger: Development UI from the API launch URL (AppHost forces `ASPNETCORE_ENVIRONMENT=Development`)  
- Durably UI: `/durable` on the API  
- Aspire dashboard: confirm `postgres` / `durable` then `api` are **Running**. If `api` stays **Waiting**, the database is not healthy yet; if **Failed**, open the resource **Console**.
- **Running ≠ reachable.** Open the **api Console** and confirm `API Program starting` / `API listening`. Under AppHost, HTTP/HTTPS ports are **allocated by Aspire each run** (launch profile ports are ignored) — always use the **current** dashboard URL, not a bookmark to `58329`/`58330`.
- Before a fresh AppHost session after failed debug runs: stop debugging, then kill leftover `Sample.AspNetCore.Api` / `Sample.Worker` processes so they cannot steal ports or confuse the debugger.
- Breakpoints in this project's `Program.cs`: debug the **AppHost** so the IDE attaches to the child `api` process, or attach to the live `Sample.AspNetCore.Api` PID. Plain `dotnet run` on AppHost alone will not hit those breakpoints.

## Oop workflows

| Route prefix | Folder | Highlights |
|--------------|--------|------------|
| `/api/order-finalize` | `Workflows/Oop/OrderFinalize` | Resume, Fixed retry, DI hooks, idempotency key, `FlowStartResult` |
| `/api/order-fulfillment` | `Workflows/Oop/OrderFulfillment` | `StepIf`, multi-step `Choose` arm, builder hooks |
| `/api/payment-capture` | `Workflows/Oop/PaymentCapture` | `RetryOn` / `DoNotRetryOn`, step `Timeout`, `Attempt` |
| `/api/subscription-renewal` | `Workflows/Oop/SubscriptionRenewal` | Lambda + `IStep` mix inside `IFlow`, `OpenConflictPolicy.Skip` |
| `/api/notification-dispatch` | `Workflows/Oop/NotificationDispatch` | Nested `Choose`, metadata, `ITraceRedactor` |

## Fluent / lambda workflows

| Route prefix | Folder | Highlights |
|--------------|--------|------------|
| `/api/invoice-reminder` | `Workflows/Fluent/InvoiceReminder` | Pure `Flow.For` + lambdas; `StepIf` + `Choose`; no `IFlow` / `IStep` |

Compared to [Sample.Worker](../Sample.Worker/): Worker is fluent + `IStep` classes; this API fluent sample is fluent + **lambda** steps only.

Hosting knobs in `Program.cs`: `ConfigureWorker`, `AddTraceability`, `AddDurablyUI`.

## Quick curls

Replace `BASE` with the API URL (e.g. `https://localhost:7xxx`).

### Fluent — invoice reminder

```bash
curl -X POST "$BASE/api/invoice-reminder/inv-1" -H "Content-Type: application/json" \
  -d '{"customerEmail":"billing@example.com","daysOverdue":45,"channel":"sms","amountDue":250}'
curl "$BASE/api/invoice-reminder/inv-1/status"
```

### Oop — order finalize + resume

```bash
curl -X POST "$BASE/api/order-finalize/ord-1/simulate-email-failure"
curl -X POST "$BASE/api/order-finalize/ord-1" -H "Content-Type: application/json" \
  -d '{"customerEmail":"a@example.com","total":42,"channel":"standard"}'
curl "$BASE/api/order-finalize/ord-1/status"
```

### Oop — fulfillment (express + fraud gate)

```bash
curl -X POST "$BASE/api/order-fulfillment/ord-2" -H "Content-Type: application/json" \
  -d '{"customerEmail":"b@example.com","total":750,"channel":"express"}'
```

### Oop — payment (transient retry vs permanent fail)

```bash
curl -X POST "$BASE/api/payment-capture/pay-1/simulate-transient"
curl -X POST "$BASE/api/payment-capture/pay-1" -H "Content-Type: application/json" -d '{"amount":19.99}'
```

### Oop — subscription (idempotent open conflict)

```bash
curl -X POST "$BASE/api/subscription-renewal/sub-1" -H "Content-Type: application/json" \
  -d '{"customerEmail":"sub@example.com"}'
# second POST while open → outcome Skipped
curl -X POST "$BASE/api/subscription-renewal/sub-1" -H "Content-Type: application/json" \
  -d '{"customerEmail":"sub@example.com"}'
```

### Oop — notification (nested branches)

```bash
curl -X POST "$BASE/api/notification-dispatch/n-1" -H "Content-Type: application/json" \
  -d '{"priority":"urgent","channel":"sms","recipient":"+15551212","message":"Ship now"}'
```

All start endpoints return `outcome` (`Created` / `Conflict` / `Skipped`), `runId`, and `status`.
