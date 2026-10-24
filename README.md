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

# 4. Run the API
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
