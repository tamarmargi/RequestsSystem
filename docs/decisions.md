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
