# Durably Samples

Runnable examples for the main adoption paths. Prefer **Aspire + Docker** for durable SQL; use **InMemory** for zero-deps demos.

| Sample | Target | Role |
|--------|--------|------|
| [Sample.AspNetCore.AppHost](./Sample.AspNetCore.AppHost/) | .NET 10 Aspire 13 | PostgreSQL containers for API + Worker |
| [Sample.AspNetCore.Api](./Sample.AspNetCore.Api/) | .NET 10 Web API | Workflows by design style (`Oop` / `Fluent`), UI, Traceability |
| [Sample.Worker](./Sample.Worker/) | .NET 10 Worker | Fluent `Flow.For` + `IStep`, background enqueue |

## Quick start

```bash
# Recommended: Docker Desktop must be running (Postgres container)
dotnet run --project samples/Sample.AspNetCore.AppHost

# API only, InMemory (no Docker / no LocalDB)
dotnet run --project samples/Sample.AspNetCore.Api

# Worker only, InMemory
dotnet run --project samples/Sample.Worker
```

### Aspire dashboard tips

- Start AppHost only after **Docker Desktop** is up.
- In the dashboard, wait for `postgres` / `durable` / `worker`, then `api` / `sample-worker` (they use `WaitFor`). States: Waiting → Running, or Failed (open **Console**).
- **Running ≠ listening.** Check the **api Console** for `API Program starting` / `API listening`. AppHost assigns **new HTTP/HTTPS ports each run** — open the URL shown in the dashboard for that session (do not reuse old `58329`/`58330` bookmarks).
- After failed debug sessions: stop AppHost, kill leftover `Sample.AspNetCore.Api` / `Sample.Worker`, then start again.
- Breakpoints in API `Program.cs` require debugging the **AppHost** so the IDE attaches to child projects, or attach to the live `Sample.AspNetCore.Api` PID. Plain `dotnet run` on AppHost will not hit API breakpoints.

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

| Run | Store |
|-----|--------|
| AppHost | API + Worker → Postgres (`Durably__Store=Postgres`; `ConnectionStrings:durable` / `ConnectionStrings:worker`) |
| Standalone `dotnet run` | **InMemory** from `appsettings.json` |

AppHost sets `Durably__Store` and Development hosting env via environment (overrides appsettings). Standalone keeps `Durably:Store=InMemory`. Samples use `AutoMigrate = true` for demos.

No sample runs are auto-imported. The API only executes flows you start. The Worker demo enqueue loop is off unless you set `Worker:PendingOrderIds`. Leftover AppHost executions live in Docker volume `durably-pg-data` — delete it to wipe persisted state. An unused leftover `durably-sql-data` volume from older SQL Server AppHost runs can be deleted too.

## Definition changes

Inserting/reordering/removing steps on a live flow name can quarantine in-flight runs. Prefer a new flow name and drain the old one. See root docs `FLOW-DEFINITION-AND-RESUME.md`.
