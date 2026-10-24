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
