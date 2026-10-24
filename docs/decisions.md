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
