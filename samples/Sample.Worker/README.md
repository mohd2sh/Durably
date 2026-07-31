# Sample.Worker

.NET 8 Worker Service sample showing durable execution in a background polling loop.

This sample uses PostgreSQL with a plain connection string from `appsettings.json`:

```csharp
.UsePostgres(connectionString, o => o.AutoMigrate = true)
```

No `NpgsqlConnection` is created in the sample app.

## Prerequisites

- PostgreSQL running locally, or update `ConnectionStrings:Durable` in `appsettings.json`.

## Run

```bash
dotnet run --project samples/Sample.Worker
```

Watch the console. The worker polls every 5 seconds and processes `order-1` and `order-2` from config.

## Expected behavior

- `order-1` completes on the first cycle.
- `order-2` fails once at send-email (simulated SMTP failure), then completes on the next poll cycle without re-running generate-report.

Stop with Ctrl+C.

## What this demonstrates

- Generic Host + `AddDurably()` + PostgreSQL + traceability
- Caller re-invokes `ExecuteAsync` on each poll; the engine resumes from the last checkpoint
- Background jobs do not need HTTP — the same durable flow API applies
