# Durably Samples

Runnable examples for the main adoption paths (.NET 6+ with EF Core persistence).

| Sample | Target | Scenario |
|--------|--------|----------|
| [Sample.AspNetCore.AppHost](./Sample.AspNetCore.AppHost/) | .NET 8 Aspire | Orchestrates API + SQL Server container (recommended local dev) |
| [Sample.AspNetCore.Api](./Sample.AspNetCore.Api/) | .NET 8 Web API | DI, SQL Server, traceability, OOP flows |
| [Sample.Worker](./Sample.Worker/) | .NET 8 Worker | PostgreSQL, background polling, resume on next cycle |

## Quick start

```bash
# Modern Web API with Aspire (Docker required for SQL Server container)
dotnet run --project samples/Sample.AspNetCore.AppHost

# Modern Web API standalone (LocalDB fallback)
dotnet run --project samples/Sample.AspNetCore.Api

# Background worker
dotnet run --project samples/Sample.Worker
```

All samples cover the **order** story: finalize (report → email → finalize) and the API also demos **fulfill** (`StepIf` / `Choose`) plus success/failure hooks.

Persistence uses **EF Core** (`Durably.Persistence.EntityFrameworkCore`) with `AutoMigrate = true` in development so the database schema is created automatically.

## Shared flow story

1. First run fails at `send-email` (when simulated).
2. Second run resumes from `send-email`; `generate-report` does not re-execute.
3. Flow completes.

See each sample's README for endpoint details and curl examples.
