# Durably Samples

Runnable examples for the main adoption paths. Prefer **Aspire + Docker** for durable SQL; use **InMemory** for zero-deps demos.

| Sample | Target | Role |
|--------|--------|------|
| [Sample.AspNetCore.AppHost](./Sample.AspNetCore.AppHost/) | .NET 10 Aspire 13 | SQL Server (API) + PostgreSQL (Worker) containers |
| [Sample.AspNetCore.Api](./Sample.AspNetCore.Api/) | .NET 10 Web API | Workflows by design style (`Oop` / `Fluent`), UI, Traceability |
| [Sample.Worker](./Sample.Worker/) | .NET 10 Worker | Fluent `Flow.For` + `IStep`, background enqueue |

## Quick start

```bash
# Recommended: Docker Desktop for Aspire containers
dotnet run --project samples/Sample.AspNetCore.AppHost

# API only, InMemory (no Docker / no LocalDB)
dotnet run --project samples/Sample.AspNetCore.Api

# Worker only, InMemory
dotnet run --project samples/Sample.Worker
```

## Explore by design (API)

API workflows are grouped under folders so you can browse by authoring style:

| Style | Folder | Registration | How steps are written |
|-------|--------|--------------|------------------------|
| **Oop** | [`Workflows/Oop/`](./Sample.AspNetCore.Api/Workflows/Oop/) | `AddFlowsFromAssembly` | `IFlow` + `IStep` classes |
| **Fluent** | [`Workflows/Fluent/`](./Sample.AspNetCore.Api/Workflows/Fluent/) | `AddFlow` | `Flow.For` + lambda steps |

Controllers mirror the same split: `Controllers/Oop/`, `Controllers/Fluent/`.

### Oop catalog

| Workflow | Features |
|----------|----------|
| **OrderFinalize** | Checkpoints/resume, Fixed retry, DI success/failure handlers, idempotency keys, `FlowStartResult` |
| **OrderFulfillment** | `StepIf`, `Choose` with multi-step express arm, builder `OnSuccess`/`OnFailure` |
| **PaymentCapture** | `RetryOn`/`DoNotRetryOn`, per-step `Timeout`, `IStepContext.Attempt` |
| **SubscriptionRenewal** | Lambda + `IStep` mix inside `IFlow`, `OpenConflictPolicy.Skip` |
| **NotificationDispatch** | Nested `Choose`, searchable metadata, `ITraceRedactor` |

### Fluent catalog

| Workflow | Features |
|----------|----------|
| **InvoiceReminder** | Pure `Flow.For` + lambdas, `StepIf`, `Choose`, no `IFlow`/`IStep` |

See [Sample.AspNetCore.Api/README.md](./Sample.AspNetCore.Api/README.md) for routes and curls.

## Shared hosting features (Program.cs)

- `ConfigureWorker` (poll, batch, lease, runner id)
- `AddTraceability` with input/output + exception capture
- `AddDurablyUI` at `/durable`
- `AddFlowsFromAssembly` (Oop) + `AddFlow` (Fluent) in the API; Worker uses fluent + `IStep`

## Persistence notes

- Standalone defaults to **InMemory** (`Durably:Store=InMemory`). State is lost on process exit.
- Aspire injects connection strings and selects EF SQL Server / Postgres automatically.
- Production schema ownership is out of scope here; samples use `AutoMigrate = true` for demos.

## Definition changes

Inserting/reordering/removing steps on a live flow name can quarantine in-flight runs. Prefer a new flow name and drain the old one. See root docs `FLOW-DEFINITION-AND-RESUME.md`.
