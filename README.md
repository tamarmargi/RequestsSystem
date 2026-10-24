# RequestsSystem

A system for managing requests from organizations.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or newer SDK with the .NET 8 runtime)
- [Docker](https://www.docker.com/) (Docker Desktop on Windows/macOS)

## Project structure

```
RequestsSystem/
├── backend/
│   ├── RequestsSystem.sln
│   ├── src/
│   │   ├── Requests.Api/      # HTTP entry point: controllers, Program.cs, configuration
│   │   ├── Requests.BL/       # Business logic: services, DTOs, validation
│   │   └── Requests.DAL/      # Data access with EF Core: DbContext, entities, migrations
│   └── tests/
│       └── Requests.Tests/    # xUnit unit and integration tests
├── frontend/                  # Angular client (to be added)
├── docs/                      # Technical decisions and work plan
├── .config/                   # Local .NET tools manifest (dotnet-ef)
└── docker-compose.yml         # Local SQL Server 2022
```

## How to run

All commands are run from the `RequestsSystem` folder (the repository root).

```bash
# 1. Local secrets (first time only)
cp .env.example .env

# 2. Start SQL Server 2022 and wait until it is healthy
docker compose up -d

# 3. Restore local tools (dotnet-ef) and apply migrations
dotnet tool restore
dotnet ef database update --project backend/src/Requests.DAL --startup-project backend/src/Requests.Api

# 4. Generate test data (100,000 requests; see "Test Data" below)
dotnet run --project backend/src/Requests.Api -- --seed

# 5. Run the API
dotnet run --project backend/src/Requests.Api
```

- API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- Health check: `GET http://localhost:5000/api/health` → `200 { "status": "ok" }`

> SQL Server from Docker is exposed on host port **1435** (not 1433) so it does not collide with a
> locally installed SQL Server instance, which typically uses TCP 1433 and its DAC on 1434.

Run tests:

```bash
dotnet build backend/RequestsSystem.sln
dotnet test backend/RequestsSystem.sln
```

## Test Data

Generate test data (SQL Server must be running and migrations applied):

```bash
dotnet run --project backend/src/Requests.Api -- --seed               # 100,000 requests (default)
dotnet run --project backend/src/Requests.Api -- --seed --count 5000  # any count from 1 to 1,000,000
```

The command fills the database and exits without starting the web server. It **deletes all existing
requests and history first**, so running it again recreates exactly the same data: generation uses a
fixed random seed (42) and a fixed reference date (2026-10-01 UTC), never the current time.

What is generated (code in `backend/src/Requests.DAL/Seeding/`):

- **100,000 requests** with Ids 1..100,000, Hebrew titles, 300 organizations (municipalities, regional
  councils, hospitals, NGOs, companies; Hebrew and English) and 50 handlers.
- **~178,000 status history rows**: every non-`New` request has a consistent path from `New` to its
  current status, with strictly increasing `ChangedAt` between `CreatedAt` and the reference date.
  `UpdatedAt` equals the last change.

| Field | Distribution |
|---|---|
| `Status` | Completed 50%, InProgress 25%, New 15%, Waiting 10% |
| `Priority` | Low 50%, Medium 35%, High 15% |
| `OrganizationName` | Skewed: 20% of organizations (60) receive ~60% of requests |
| `AssignedTo` | `null` for `New`; otherwise one of 50 handlers |
| `CreatedAt` | Uniform within the 2 years before the reference date |
| History path | InProgress: New→InProgress · Waiting: New→InProgress→Waiting · Completed: New→InProgress→Completed, or (1 in 3) New→InProgress→Waiting→InProgress→Completed |

**Why skewed distributions?** Real data is never uniform. Index selectivity depends on the actual value
distribution: filtering by `Status = Waiting` (10%) behaves differently from `Status = Completed` (50%),
and a "big" organization returns many more rows than a small one. Realistic skew makes the index and
performance measurements in later steps meaningful instead of optimistic.

**Why SqlBulkCopy?** Inserting ~280,000 rows through EF Core (`AddRange` + `SaveChanges`) means change
tracking and batched `INSERT` statements and takes minutes. `SqlBulkCopy` streams the rows with the
bulk-load protocol inside one transaction. Measured on the local Docker SQL Server: generation ~0.4 s,
insert ~2.8 s on an empty database (~7.4 s when existing data is deleted first); the whole command
finishes in under 10 seconds.

## Architecture

The backend solution (`backend/RequestsSystem.sln`) has three layers plus a test project:

| Project | Responsibility |
|---|---|
| `backend/src/Requests.Api` | Entry point only: ASP.NET Core Web API controllers, `Program.cs`, configuration, Swagger. No business logic. |
| `backend/src/Requests.BL` | Business logic: services, DTOs, validation (FluentValidation). Exposes `AddBl()` for DI registration. |
| `backend/src/Requests.DAL` | Data access: EF Core `AppDbContext`, entities, entity configurations, migrations. Exposes `AddDal()` for DI registration. |
| `backend/tests/Requests.Tests` | xUnit tests (unit and integration via `WebApplicationFactory` + Testcontainers). |

Dependency direction: `Api → BL → DAL`, and `Api → DAL` for DI wiring. DAL references neither BL nor Api; BL does not reference Api.

### Data model

- **Requests**: `Id`, `Title`, `OrganizationName`, `Status` (tinyint), `Priority` (tinyint), `AssignedTo`, `CreatedAt`/`UpdatedAt` (UTC), `RowVersion` (rowversion concurrency token).
- **RequestStatusHistory**: `Id`, `RequestId` (FK, cascade delete), `PreviousStatus`, `NewStatus`, `ChangedAt` (UTC), `ChangedBy`. Indexed on `(RequestId, ChangedAt)`.

See [docs/decisions.md](docs/decisions.md) for technical decisions.

## Database & Indexes

TODO

## Concurrency

TODO

## Bulk Update

TODO

## Cache

TODO

## Performance

TODO

## Technical Decisions

TODO

## Known Limitations

TODO

## AI Usage

TODO
