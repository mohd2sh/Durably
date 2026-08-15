# Sample.AspNetCore.Api

.NET 10 Web API showcase: business workflows organized by **authoring design**, Durably UI, Traceability, and store selection (Aspire SQL Server or InMemory). Requires the .NET 10 SDK.

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
| `dotnet run --project samples/Sample.AspNetCore.AppHost` | EF SQL Server **container** (Aspire injects `ConnectionStrings:durable`) |
| `dotnet run --project samples/Sample.AspNetCore.Api` | **InMemory** by default (`Durably:Store=InMemory`) |

Optional SQL without Aspire: set `Durably:Store=SqlServer` and `ConnectionStrings:Durable`.

## Run

```bash
dotnet run --project samples/Sample.AspNetCore.AppHost
# or
dotnet run --project samples/Sample.AspNetCore.Api
```

- Swagger: Development UI from the API launch URL  
- Durably dashboard: `/durable`

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
