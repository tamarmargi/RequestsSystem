# RequestsSystem

A system for managing requests from organizations.

- Work plan: [docs/work-plan.md](docs/work-plan.md)
- API contract: [docs/api-contract.md](docs/api-contract.md)

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or newer SDK with the .NET 8 runtime)
- [Docker](https://www.docker.com/) (Docker Desktop on Windows/macOS)
- [Node.js](https://nodejs.org/) LTS – developed with Node 24.12.0 and npm 11.6.2 (Angular 21 requires Node `^20.19`, `^22.12` or `>=24`)

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
├── frontend/                  # Angular 21 client (see frontend/README.md)
│   ├── proxy.conf.json        # dev proxy: /api → http://localhost:5000
│   └── src/
│       ├── styles/            # design tokens (_tokens.scss) used by all component styles
│       └── app/
│           ├── models/        # TypeScript types matching docs/api-contract.md
│           ├── services/      # HTTP clients (RequestsApiService, HealthService)
│           ├── core/          # cross-cutting code: ApiError, error interceptor
│           └── features/      # UI by feature (requests/requests-page)
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

Run the frontend (in a second terminal, while the API is running):

```bash
cd frontend
npm install
npm start
```

Then open http://localhost:4200. The dev server proxies `/api` to the API on port 5000; the page
shows "השרת זמין" when the API is reachable.

To run the tests, see [Running tests](#running-tests).

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

## API

Endpoints implemented so far (full specification in [docs/api-contract.md](docs/api-contract.md)):

| Endpoint | Description |
|---|---|
| `GET /api/requests` | Paged list with filters (`status`, `priority`, `organizationName`, `assignedTo`, `createdFrom`/`createdTo`), free-text `search` on title or organization, sorting (`sortBy`, `sortDirection`) and paging (`page`, `pageSize` ≤ 100). `400` on invalid parameters. |
| `GET /api/requests/{id}` | A single request, or `404`. |
| `GET /api/requests/stats` | Counts over all requests: total, per status and per priority. |
| `PATCH /api/requests/{id}/status` | Changes the status, with optimistic concurrency on `version` (see [Concurrency](#concurrency)). `200` updated request, `400` invalid body or transition, `404`, `409` with the current state. Optional `X-User-Name` header for the audit row. |
| `GET /api/requests/{id}/history` | Status change history, newest first, or `404`. |
| `POST /api/requests/bulk-status` | Changes the status of 1–100 requests with partial success (see [Bulk Update](#bulk-update)). `200` with a result per item whenever the request is valid; `400` for an invalid request (nothing changed). |
| `GET /api/health` | Liveness check. |

Enums are strings, dates are UTC ISO 8601, `version` is the Base64 `rowversion`, and every error is an
RFC 7807 ProblemDetails (`400` responses include an `errors` object keyed by the camelCase parameter name).

[backend/src/Requests.Api/Requests.http](backend/src/Requests.Api/Requests.http) contains runnable
examples for every endpoint, including invalid input (open it in VS Code with the REST Client extension,
or in Visual Studio / Rider).

## Running tests

```bash
dotnet test backend/RequestsSystem.sln

cd frontend && npm test   # Angular tests, single headless run
```

The backend tests include unit tests (no dependencies) and **integration tests that need Docker running**:
they start a throwaway SQL Server 2022 container with Testcontainers (once for all integration tests),
apply the migrations, seed 500 deterministic requests and call the API over HTTP through
`WebApplicationFactory`. The first run also pulls the SQL Server image if it is not present.

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

### Avoiding loading all data into memory

- Filtering, search, sorting and paging are composed on one `IQueryable` in `RequestRepository` and
  executed by SQL Server: one query with `ORDER BY … OFFSET … FETCH NEXT` for the page, plus one
  `COUNT(*)` query with the same `WHERE` for `totalCount`. Nothing is materialized before paging.
- Queries use `AsNoTracking`, so EF Core does not build change-tracking state for read-only data.
- Only the current page (at most 100 rows, enforced by validation) leaves the database.
- Stats are computed in SQL by a single aggregate query (`COUNT(*)` plus conditional
  `COUNT(CASE WHEN …)` per status and priority), one pass over the table instead of one query per group.
- The `Requests` table has no indexes besides the primary key yet; indexes will be added and measured in
  the performance step.

## Concurrency

Status updates use **optimistic concurrency** with the SQL Server `rowversion` column (`RowVersion`), which
SQL Server changes automatically on every update of the row. Every `RequestDto` carries it as `version`
(Base64), and the client sends back the version it saw in `PATCH /api/requests/{id}/status`.

**The database enforces the check.** The repository sets the version from the client as the *original*
value of `RowVersion`, so EF Core generates:

```sql
UPDATE [Requests] SET [Status] = @p5, [UpdatedAt] = @p6
OUTPUT INSERTED.[RowVersion]
WHERE [Id] = @p7 AND [RowVersion] = @p8;
```

If someone else changed the row in the meantime, the `WHERE` matches no row, EF Core raises
`DbUpdateConcurrencyException`, nothing is saved (including the history row, which is in the same
transaction), and the API returns `409`. The check in code that compares versions right after loading
the request is only a shortcut that avoids a pointless `UPDATE`. Correctness does not depend on it, nor
on the UI: two requests sent at exactly the same moment are serialized by SQL Server, and only one
`UPDATE` matches (covered by an integration test that sends two `PATCH` calls in parallel with
`Task.WhenAll`).

**What the client receives on 409.** ProblemDetails with the title "The request was modified by another
user" and the current state under `current` (a full `RequestDto` with the new `version`). The client
should show the user what changed and, if the user still wants the change, send it again with
`current.version`. This prevents **lost updates**: a change based on stale data can never silently
overwrite someone else's change.

`UpdatedAt` and `RowVersion` change on every update: `UpdatedAt` is set centrally in
`AppDbContext.SaveChanges` for every modified request, so no code path can forget it; `RowVersion` is
set by SQL Server. The seeder writes with `SqlBulkCopy`, bypasses `SaveChanges`, and keeps its generated
`UpdatedAt` values.

### Status transitions

| From | Allowed to |
|---|---|
| `New` | `InProgress` |
| `InProgress` | `Waiting`, `Completed` |
| `Waiting` | `InProgress`, `Completed` |
| `Completed` | – (final) |

Changing a status to itself is not allowed. The rules (`StatusTransitions` in the BL) match the history
paths of the seeded data.

### Order of checks in `PATCH …/status`

1. Body is valid (`newStatus` known, `version` is Base64 of 8 bytes), otherwise `400` with `errors`.
2. The request exists, otherwise `404`.
3. `version` equals the current version, otherwise `409` with `current`. This comes before the transition
   rule: with stale data a transition error would be misleading.
4. The transition is allowed, otherwise `400` "Invalid status transition", with a message that names both
   statuses and the allowed targets.
5. Save. If the database rejects the version (someone saved in between), the current state is reloaded and
   the result is `409` with `current`.

### Audit history

Every successful status change adds a `RequestStatusHistory` row: `RequestId`, `PreviousStatus`,
`NewStatus`, `ChangedAt` (UTC) and `ChangedBy` (from the `X-User-Name` header, `"anonymous"` if missing).
The row is added in the same `SaveChanges` call as the status change, so both are written in one
transaction: there is never a change without an audit row, or an audit row without a change. A rejected
update (`409`, `400`) writes nothing.

`GET /api/requests/{id}/history` returns the rows newest first (`ChangedAt` descending, then `Id`
descending). It uses the existing index on `(RequestId, ChangedAt)`.

## Bulk Update

`POST /api/requests/bulk-status` changes the status of up to 100 requests in one call, with **partial
success**: every item succeeds or fails on its own.

**Why partial success?** The items are independent requests, each with its own version. One request that
someone else changed in the meantime should not block 99 valid updates, and with all-or-nothing the user
would have to fix and resend the whole batch without knowing which items were the problem. With partial
success the client sees exactly what failed and why, and can retry only those items.

**Two levels of errors:**

- *Request-level* (`400` ValidationProblemDetails, nothing touched in the database): missing or unknown
  `newStatus`, empty list, more than 100 items, duplicate ids, a `version` that is not Base64 of 8 bytes.
- *Item-level* (inside a `200` response): `NotFound`, `Conflict` (stale version, or someone saved in between)
  and `InvalidTransition` (with a message naming the current status, the requested one and the allowed targets).

**How it runs.** All requests are loaded with **one query** (`WHERE Id IN (…)`). Then each item, in input
order, goes through the same code as the single `PATCH`: exists → version → transition → save. Each
successful item is saved with **its own `SaveChanges`, so its own transaction**, which writes the status
change and its **audit history row** together (`ChangedBy` from `X-User-Name`). A save that fails because of
a concurrency conflict detaches that request and its pending history row, so the next item in the same
`DbContext` is not affected. The stats cache is invalidated once at the end, only if at least one item
was updated.

Example response (one item of each outcome, results in the order of the request):

```json
{
  "succeeded": 1,
  "failed": 3,
  "results": [
    { "id": 77048, "outcome": "Updated", "version": "AAAAAAAGKIo=" },
    { "id": 999999, "outcome": "NotFound", "message": "Request 999999 does not exist." },
    { "id": 4088, "outcome": "Conflict", "message": "The request was modified by another user." },
    { "id": 49769, "outcome": "InvalidTransition", "message": "Cannot change status from Completed to Completed. Allowed from Completed: none (Completed is final)." }
  ]
}
```

**Why `200` and not `207 Multi-Status`?** `207` comes from WebDAV and is poorly supported by HTTP clients
and tooling (many treat it as plain success anyway). The request itself was processed successfully, and the
body already carries the outcome of every item, plus `succeeded` / `failed` counts.

## Cache

**What is cached:** only the stats (`GET /api/requests/stats`). They are an aggregate over the whole table,
which is expensive. They are identical for all users and read on every page load, and a few seconds of
staleness in summary counts is harmless. The list is not cached: with free-text search, filters, sorting
and paging, almost every query is different, so the hit rate would be near zero.

**How:** `IStatsCache` (BL) with the implementation `MemoryStatsCache` over `IMemoryCache`, registered as a
singleton. It never captures scoped services: the factory that queries the database is passed in on every
call. Expiration is **absolute**, 60 seconds by default (`Cache:StatsSeconds` in `appsettings.json`), so the
stats are never older than that. Sliding expiration would keep a frequently read value alive forever.

**Invalidation:** after every successful single status change, and after a bulk update when at least one
item was updated. It happens **only after the save succeeded**, never before: invalidating first would let
a concurrent read re-cache the old values before the change is committed. Hits, misses and invalidations
are logged at Debug (enabled in `appsettings.Development.json`).

**Known race:** a stats read that started *before* an update can finish *after* the invalidation and put the
old values back in the cache. The absolute expiration bounds this: the stale value lives at most 60 seconds.

**Multiple instances:** each instance would have its own memory cache, and an invalidation on one instance
would not reach the others. The fix is a Redis implementation of `IStatsCache` (over `IDistributedCache`),
plus either broadcasting the invalidation to all instances (e.g. Redis pub/sub) or versioned keys (a
counter in Redis that is incremented on every change and is part of the cache key). Thanks to the interface,
no other code changes.

## Performance

TODO

## Technical Decisions

TODO

## Known Limitations

- **`ChangedBy` comes from a client header (`X-User-Name`)**, because there is no authentication. Any
  client can send any name. In a real system it would come from the authenticated user (e.g. a claim of a
  validated JWT), not from a header the client controls. Header values must also be ASCII: browsers refuse
  header values with characters such as Hebrew letters.
- **Bulk update saves each item separately**: up to 100 `SaveChanges` round trips (plus one reload per
  save-time conflict). This keeps partial success simple and correct. A possible improvement is a single
  batched save with per-entry conflict handling (detach the failed entries and retry the rest), or a
  set-based SQL `UPDATE … WHERE Id = … AND RowVersion = …` with `OUTPUT` that returns which rows changed,
  and writes the history rows in the same statement batch.
- **Stats cache is per instance** (see [Cache](#cache)); running several API instances needs the Redis
  implementation of `IStatsCache`.

## AI Usage

TODO
