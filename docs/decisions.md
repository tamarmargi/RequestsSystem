# Technical Decisions

## 1. SQL Server

- **Decision:** Use SQL Server 2022 as the database.
- **Reason:** Built-in `rowversion` provides optimistic concurrency out of the box, and execution plans make performance analysis straightforward.
- **Alternatives:** MongoDB, PostgreSQL.

## 2. EF Core

- **Decision:** Use EF Core as the data access technology.
- **Reason:** LINQ is translated to SQL, and EF Core has built-in concurrency support (concurrency tokens / rowversion).
- **Alternatives:** Dapper.

## 3. Three layers (Api / BL / DAL)

- **Decision:** Split the solution into three projects: Api, BL and DAL.
- **Reason:** Clear separation of responsibilities without over-engineering. No CQRS, MediatR or microservices.
- **Alternatives:** Clean Architecture with 4+ projects.

## 4. Enums stored as tinyint

- **Decision:** Store `RequestStatus` and `RequestPriority` as `tinyint`.
- **Reason:** Smaller rows and smaller indexes.
- **Alternatives:** Store enums as strings.

## 5. All dates in UTC

- **Decision:** All `DateTime` values (`CreatedAt`, `UpdatedAt`, `ChangedAt`) are stored and handled in UTC.
- **Reason:** Avoids time-zone and daylight-saving ambiguity; conversion to local time is a presentation concern.
- **Alternatives:** Local server time, `DateTimeOffset`.

## 6. Seeding with SqlBulkCopy and a fixed random seed

- **Decision:** Generate test data in memory with a fixed random seed (42) and a fixed reference date, and insert it with `SqlBulkCopy` (from `DataTable`s, inside one transaction) via `dotnet run -- --seed`.
- **Reason:** Fast (seconds instead of minutes with EF Core) and deterministic: every run on every machine produces exactly the same data, so performance measurements before and after adding indexes are comparable.
- **Alternatives:** EF Core `AddRange` + `SaveChanges`, the Bogus library, a SQL script.

## 7. Angular Material for behavior, custom design tokens for appearance

- **Decision:** Use Angular Material components for behavior, and our own CSS custom properties (`frontend/src/styles/_tokens.scss`) for appearance. The Material theme is mapped onto these variables (primary color, typography, shape).
- **Reason:** Ready-made, accessible table, paginator, sort and dialogs that work with server-side paging and sorting, while the visual design stays under our control through our own CSS variables (to be defined in a dedicated design step).
- **Alternatives:** PrimeNG, Angular CDK with fully custom components.

## 8. API contract defined before implementation

- **Decision:** Write [api-contract.md](api-contract.md) (endpoints, parameters, response shapes, status codes) before implementing the endpoints; the TypeScript models mirror it exactly.
- **Reason:** Lets the frontend be built in parallel with the backend and avoids mismatches between them.
- **Alternatives:** Generating the client from Swagger after the backend is done.

## 9. Dev proxy instead of CORS

- **Decision:** In development the Angular dev server proxies `/api` to `http://localhost:5000` (`frontend/proxy.conf.json`); the app only calls relative URLs.
- **Reason:** The browser calls the same origin, so no CORS configuration is needed in development.
- **Note:** In production the frontend would be served behind the same host as the API, or CORS would be configured explicitly.

## 10. Specific repository in the DAL (no generic repository)

- **Decision:** One specific repository, `IRequestRepository` / `RequestRepository`, with methods for the queries the application needs (`SearchAsync`, `GetByIdAsync`, `GetStatsAsync`). `IQueryable` never leaves the DAL.
- **Reason:** Keeps EF Core and SQL inside the DAL; all request queries live in one place, which is where the performance work (indexes, query shape) happens; avoids the over-engineering of a generic repository on top of EF Core, which already is a repository / unit of work.
- **Alternatives:** The BL uses `DbContext` directly; a generic `IRepository<T>`.

## 11. Stable sorting with Id as a tie-breaker

- **Decision:** Every sort field is followed by `Id` as a secondary sort in the same direction (e.g. `ORDER BY Priority DESC, Id DESC`).
- **Reason:** Without a unique tie-breaker, the order of rows with equal sort values is undefined, so with `OFFSET/FETCH` the same row can appear on two pages or be skipped.

## 12. Date range semantics

- **Decision:** `createdFrom` and `createdTo` are dates (`yyyy-MM-dd`), and the range includes both days. It is implemented as `CreatedAt >= createdFrom 00:00 UTC` and `CreatedAt < (createdTo + 1 day) 00:00 UTC`.
- **Reason:** "Up to and including 31 January" is what users mean; an exclusive upper bound on the next day covers the whole last day without precision issues such as `23:59:59.999`.

## 13. Stats computed over all requests, in one query

- **Decision:** `GET /api/requests/stats` counts over the whole table, independent of list filters, in a single SQL query with conditional counts.
- **Reason:** The result is identical for all users, so it can be cached later; one pass over the table instead of one query per group.
- **Alternatives:** Stats per current filter.

## 14. Validation in the BL, mapped to 400 by a global handler

- **Decision:** Query parameters are validated with FluentValidation in the BL service, which throws an HTTP-agnostic `ValidationException`; `GlobalExceptionHandler` in the API maps it to `400 ValidationProblemDetails`. Model-binding errors (e.g. `status=Banana`) use the same format and camelCase field names.
- **Reason:** The rules live with the business logic and stay independent of HTTP; all error responses are uniform ProblemDetails.

## 15. Status transition rules

- **Decision:** `New → InProgress`; `InProgress → Waiting | Completed`; `Waiting → InProgress | Completed`; `Completed` is final; changing a status to itself is not allowed. Implemented as a static, pure class (`StatusTransitions`) in the BL.
- **Reason:** A simple workflow that is easy to explain and test. `Completed` is final so finished work cannot be reopened silently. The rules are consistent with the history paths of the seeded data (every seeded transition is allowed; `Waiting → Completed` is allowed but does not occur in the seed).

## 16. Version in the request body rather than If-Match

- **Decision:** The client sends the concurrency token as `version` in the JSON body of `PATCH …/status`; a mismatch returns `409 Conflict`.
- **Reason:** Simpler for the client and for tests, explicit in the contract, and the same shape works per item in the bulk endpoint.
- **Alternatives:** ETag / `If-Match` header with `412 Precondition Failed`.

## 17. Conflict is checked before the transition rule

- **Decision:** The version is compared before the transition rule is checked.
- **Reason:** When the client's data is stale, a transition error would be misleading (the transition may be valid from the real current status, or the request may already be in the target status). `409` with the current state is the actionable answer.

## 18. Result objects instead of exceptions for expected outcomes

- **Decision:** `UpdateStatusAsync` returns a closed set of results (`Success`, `NotFound`, `Conflict`, `InvalidTransition`); the controller maps each one to an HTTP response explicitly. `DbUpdateConcurrencyException` is caught in the DAL and turned into `false`.
- **Reason:** Not found, conflict and invalid transition are normal business outcomes, not errors; returning them keeps the flow visible and the HTTP mapping in one place. Exceptions are kept for invalid input (`ValidationException`) and unexpected errors.

## 19. UpdatedAt set centrally in SaveChanges

- **Decision:** `AppDbContext` overrides `SaveChanges` / `SaveChangesAsync` and sets `UpdatedAt = DateTime.UtcNow` on every modified `Request`.
- **Reason:** No code path can forget it. (The seeder uses `SqlBulkCopy`, bypasses `SaveChanges`, and is not affected.)

## 20. Bulk update: partial success

- **Decision:** `POST /api/requests/bulk-status` processes every item independently; a failed item never rolls back or blocks the others, and the response reports the outcome of each item.
- **Reason:** The items are independent requests with their own versions. One conflict should not block 99 valid updates, and the client sees exactly what failed and can retry only those items.
- **Alternatives:** All-or-nothing (one transaction for the whole batch).

## 21. Bulk update: one transaction per item

- **Decision:** All items are loaded with one query, then each successful item is saved with its own `SaveChanges` (its own transaction, including its history row). After a concurrency failure the repository detaches the failed request and its history row, so the next item is not affected.
- **Reason:** With a single `SaveChanges` for the batch, one concurrency failure rolls back everything, which contradicts partial success.
- **Alternatives:** A batched save with retry of the remaining entries after removing the failed ones; a set-based SQL update with `OUTPUT`.

## 22. Bulk response: 200 with per-item results, not 207

- **Decision:** A valid bulk request always returns `200` with `{ succeeded, failed, results }`, even if every item failed; `400` only for an invalid request.
- **Reason:** `207 Multi-Status` comes from WebDAV and is poorly supported by clients; the body already carries all the detail.
- **Alternatives:** `207 Multi-Status`.

## 23. Cache: IMemoryCache behind IStatsCache, absolute expiration + explicit invalidation

- **Decision:** Only the global stats are cached, in `IMemoryCache` behind the `IStatsCache` interface, with absolute expiration (60 s, configurable) and explicit invalidation after every successful single or bulk update (only after the save).
- **Reason:** The stats are an expensive aggregate, identical for all users and read on every page load. Invalidation makes changes visible immediately. The absolute expiration bounds any staleness, including the race where a read that started before an update re-caches old values. A single instance needs nothing more.
- **Multi-instance note:** Each instance has its own memory cache, so an invalidation would not reach the others. Switch `IStatsCache` to a Redis implementation (`IDistributedCache`) with pub/sub invalidation or versioned keys; no other code changes.
- **Alternatives:** Redis from the start (unnecessary for a single instance).

## 24. Indexes chosen by measured queries, not per column

- **Decision:** Indexes on `Requests` are added only for queries that were measured with the exact SQL EF Core sends, and kept only if the actual execution plan of a measured query uses them: `IX_Requests_CreatedAt`, `IX_Requests_Status_CreatedAt`, `IX_Requests_AssignedTo`, `IX_Requests_OrganizationName`, `IX_Requests_Status_Priority` (see `docs/performance/README.md`).
- **Reason:** Each index speeds up specific reads but slows every write and takes space (the five indexes take about as much space as the table, and the seeder's bulk insert went from ~8 s to ~13–16 s). An index per column would mostly not match the real `WHERE` + `ORDER BY` combinations.
- **Alternatives:** One index per filterable column; indexes from the missing-index suggestions without measuring.

## 25. No filtered index for open requests

- **Decision:** No filtered index such as `(AssignedTo, CreatedAt DESC, Id DESC) WHERE Status <> 3`.
- **Reason:** Measured with the query EF sends for `?status=InProgress&assignedTo=…`: SQL Server did not use it (2,030 reads, Clustered Index Scan), and the plan reports `UnmatchedIndexes` with reason `Parameterization`. A cached plan for `Status = @p` must be valid for every value, including `3`, so an index that only contains `Status <> 3` cannot be used. Only the same query with literals (which the API never sends) used it (117 reads). It would only add write cost.
- **Alternatives:** Keep it and add `OPTION (RECOMPILE)` or literal SQL for that query (loses plan reuse; not worth it for this workload).

## 26. Composite indexes include Id as the last key

- **Decision:** Composite indexes for the list end with `CreatedAt DESC, Id DESC`, the exact `ORDER BY` of the list queries (decision 11: `Id` is the stable tie-breaker).
- **Reason:** When the index order matches the whole `ORDER BY`, SQL Server reads the top rows of the page directly from the index and stops; there is no Sort operator (Q1-page: 2,134 → 70 reads). With `CreatedAt` alone, rows with equal `CreatedAt` would still need sorting by `Id`.
