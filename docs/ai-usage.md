# AI Usage

This document records, per development step, which parts were generated with Claude Code and which were reviewed or decided manually.

## Step B – Test data (T1.4)

### Generated with Claude Code

- Seeder: `DatabaseSeeder` (delete + `SqlBulkCopy` insert in one transaction, identity reseed) and the `--seed [--count N]` command in `Program.cs`.
- Generator: `RequestGenerator` (deterministic data with status, priority and organization distributions and consistent status history) and the `SeedData` source lists.
- Tests: `RequestGeneratorTests` (determinism, status distribution, history consistency, `New` requests).

### Reviewed / decided manually

- הגדרתי את דרישות ה-Seed: התפלגויות לא שוות (סטטוס, עדיפות, ארגונים), זרע אקראי קבוע ותאריך ייחוס קבוע, ומסלולי היסטוריה הגיוניים לכל סטטוס.
- אישרתי סטייה מההוראה המקורית: גם לשורות ההיסטוריה ניתנים מזהים קבועים (1 עד M), ולא מזהים ש-SQL Server מייצר. בלי זה, כל הרצה חוזרת הייתה ממשיכה את המספור, והנתונים לא היו זהים לגמרי בין הרצות.
- בדקתי ידנית מול בסיס הנתונים: 100,000 פניות, ההתפלגות קרובה ליעד, הרצה חוזרת משאירה בדיוק אותן כמויות, ולפנייה לדוגמה יש היסטוריה שמתחילה ב-New ומסתיימת בסטטוס הנוכחי, עם תאריכים עולים.
- וידאתי שזמן ה-Seed סביר (כ-4 עד 8 שניות), ושטבלת הפניות עדיין ללא אינדקסים מלבד המפתח הראשי, לקראת מדידת הביצועים.

## Step C1 – Angular skeleton and API contract (T7.1)

### Generated with Claude Code

- API contract: `docs/api-contract.md`.
- Angular 21 skeleton in `frontend/` (standalone, routing, SCSS, no SSR, Angular Material, RTL/Hebrew, dev proxy).
- Models (`models/`) matching the contract, `RequestsApiService` and `HealthService`.
- `ApiError` and the functional error interceptor.
- Design tokens (`styles/_tokens.scss`) and the Material theme mapping in `styles.scss`; the `requests-page` with the API connection indicator.
- Tests: `requests-api.service.spec.ts`, `error.interceptor.spec.ts` (Vitest, single headless run).

### Reviewed / decided manually

- TODO: fill in by the developer

## Step C – Read API (T2.1–T2.6, T8.1, T8.3)

### Generated with Claude Code

- DAL: `RequestFilter`, `RequestStats`, `IRequestRepository` / `RequestRepository` (filtering, search, stable sorting, paging, single-query stats).
- BL: DTOs (`RequestDto`, `PagedResult<T>`, `RequestQueryParameters`, `RequestStatsDto`), `RequestMapping`, `RequestQueryParametersValidator`, `ValidationException`, `IRequestQueryService` / `RequestQueryService`.
- API: `RequestsController`, `GlobalExceptionHandler`, JSON enum-as-string and ProblemDetails setup in `Program.cs`, `Requests.http` examples.
- Tests: `RequestQueryParametersValidatorTests`, `GlobalExceptionHandlerTests`, the Testcontainers fixture (`ApiFixture`) and `RequestsApiTests`.
- Documentation: README sections (API, Running tests, Avoiding loading all data into memory) and decisions 10–14.

### Reviewed / decided manually

- TODO: fill in by the developer

## Step D – Status update, concurrency and audit (T3.1–T4.2, T8.2, T8.4)

### Generated with Claude Code

- BL: `StatusTransitions`, `UpdateStatusRequest`, `RequestHistoryItemDto`, `UpdateStatusRequestValidator`, `UpdateStatusResult`, `IRequestCommandService` / `RequestCommandService`, history in `RequestQueryService`.
- DAL: `UpdatedAt` in `AppDbContext.SaveChanges`, `GetForUpdateAsync`, `SaveStatusChangeAsync` (original RowVersion + history row in one transaction), `GetHistoryAsync`, `ExistsAsync`.
- API: `CurrentUserAccessor` (`X-User-Name`), `PATCH …/status` and `GET …/history` in `RequestsController`, cleaner binding errors, `Requests.http` examples.
- Tests: `StatusTransitionsTests` (all 16 pairs), `UpdateStatusRequestValidatorTests`, `StatusUpdateTests` (success, anonymous, stale version, parallel `Task.WhenAll`, invalid transition, 404, history order).
- Documentation: Concurrency and Audit history in the README, the PATCH and history sections of the API contract, decisions 15–19.

### Reviewed / decided manually

- TODO: fill in by the developer
