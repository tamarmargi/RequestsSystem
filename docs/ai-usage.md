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

- בחרתי ב-Angular 21 LTS ולא ב-22: תמיכה ארוכת טווח, ודרישת Node נמוכה יותר, כך שקל יותר להריץ את הפרויקט.
- החלטתי להגדיר את חוזה ה-API לפני המימוש, כדי לבנות את ה-Frontend במקביל ל-Backend.
- החלטתי להשקיע בעיצוב, ולכן ביקשתי כבר בשלב הזה בסיס של משתני עיצוב (Design Tokens), כדי שהעיצוב יוחלף במקום אחד.

## Step C – Read API (T2.1–T2.6, T8.1, T8.3)

### Generated with Claude Code

- DAL: `RequestFilter`, `RequestStats`, `IRequestRepository` / `RequestRepository` (filtering, search, stable sorting, paging, single-query stats).
- BL: DTOs (`RequestDto`, `PagedResult<T>`, `RequestQueryParameters`, `RequestStatsDto`), `RequestMapping`, `RequestQueryParametersValidator`, `ValidationException`, `IRequestQueryService` / `RequestQueryService`.
- API: `RequestsController`, `GlobalExceptionHandler`, JSON enum-as-string and ProblemDetails setup in `Program.cs`, `Requests.http` examples.
- Tests: `RequestQueryParametersValidatorTests`, `GlobalExceptionHandlerTests`, the Testcontainers fixture (`ApiFixture`) and `RequestsApiTests`.
- Documentation: README sections (API, Running tests, Avoiding loading all data into memory) and decisions 10–14.

### Reviewed / decided manually

- הגדרתי את המבנה של שלוש שכבות (API, BL, DAL), ובחנתי את ההחלטה ליצור ממשקים רק לשירותים ול-Repository, ולא ל-DTOs ולמחלקות נתונים.
- בחרתי ב-SQL Server בגלל rowversion המובנה ותוכניות הביצוע, אחרי השוואה ל-MongoDB ול-PostgreSQL.
- בדקתי את ה-Endpoints ב-Swagger מול 100,000 הרשומות.
- בדקתי את ניסוח דרישת החיפוש במבדק ("לפחות" בכותרת ובארגון), ווידאתי שהמימוש עומד בה.

## Step D – Status update, concurrency and audit (T3.1–T4.2, T8.2, T8.4)

### Generated with Claude Code

- BL: `StatusTransitions`, `UpdateStatusRequest`, `RequestHistoryItemDto`, `UpdateStatusRequestValidator`, `UpdateStatusResult`, `IRequestCommandService` / `RequestCommandService`, history in `RequestQueryService`.
- DAL: `UpdatedAt` in `AppDbContext.SaveChanges`, `GetForUpdateAsync`, `SaveStatusChangeAsync` (original RowVersion + history row in one transaction), `GetHistoryAsync`, `ExistsAsync`.
- API: `CurrentUserAccessor` (`X-User-Name`), `PATCH …/status` and `GET …/history` in `RequestsController`, cleaner binding errors, `Requests.http` examples.
- Tests: `StatusTransitionsTests` (all 16 pairs), `UpdateStatusRequestValidatorTests`, `StatusUpdateTests` (success, anonymous, stale version, parallel `Task.WhenAll`, invalid transition, 404, history order).
- Documentation: Concurrency and Audit history in the README, the PATCH and history sections of the API contract, decisions 15–19.

### Reviewed / decided manually

- גיליתי בבדיקה ב-UI שעדכון סטטוס מחזיר "הפנייה לא נמצאה". בדקתי ב-Swagger ומצאתי שה-Endpoints של שלב זה לא נוצרו, כי השלב לא הורץ. הרצתי אותו, וה-UI עבד בלי שינוי, כי נבנה לפי החוזה.
- קבעתי את כללי המעבר, כולל מעבר ישיר מ-Waiting ל-Completed, ווידאתי שהם תואמים לנתוני ה-Seed.
- בדקתי התנגשות ידנית: עדכנתי את אותה פנייה משתי לשוניות, וקיבלתי את חלון ה-409.

## Step E – Bulk update and cache (T5.1–T6.2, T8.5)

### Generated with Claude Code

- DAL: `GetManyForUpdateAsync` (one `WHERE Id IN` query), and the documented clean-context behavior of `SaveStatusChangeAsync` after a failed save.
- BL: `BulkUpdateStatusRequest` / `BulkItem`, `BulkUpdateResultDto` / `BulkItemResultDto` / `BulkOutcome`, `BulkUpdateStatusRequestValidator`, `BulkUpdateStatusAsync` (sharing the per-item checks with the single update); `IStatsCache`, `MemoryStatsCache`, `CacheOptions`, cache use in `RequestQueryService` and invalidation in `RequestCommandService`.
- API: `POST /api/requests/bulk-status`, `CacheOptions` binding and validation, `Requests.http` examples.
- Tests: `BulkUpdateStatusRequestValidatorTests`, `MemoryStatsCacheTests` (fake clock), `BulkUpdateTests` (mixed, failure in the middle, all valid, 400 cases, cache invalidation after PATCH and bulk), `RequestRepositoryTests` (clean context after a failed save).
- Documentation: the bulk section of the API contract, the README sections Bulk Update and Cache, decisions 20–23.

### Reviewed / decided manually

- בחרתי ב-Partial Success ולא ב-All-or-Nothing, כי הפניות אינן תלויות זו בזו.
- החלטתי על IMemoryCache ולא על Redis, כי השרת רץ כמופע יחיד, ו-Redis מתועד כהתאמה לריבוי שרתים.
- החלטתי שהנתונים המסכמים מחושבים על כל הפניות ולא לפי הסינון, כדי שיתאימו ל-Cache.

## Step F – Performance and indexes (T1.3, T9.1–T9.3)

### Generated with Claude Code

- Capture of the exact SQL EF Core sends (from the plan cache) into `docs/performance/queries.sql`.
- Measurement script `docs/performance/measure.ps1` (STATISTICS IO/TIME medians, actual plans as `.sqlplan`, plan operators, API timings) and the experiment files `filtered-index-test.sql` and `bottleneck-tests.sql`.
- The before / after / filtered-index / bottleneck measurements and their results files.
- The indexes in `RequestConfiguration` and the migration `AddRequestIndexes`; `Cache:StatsSeconds = 0` to disable the stats cache for measurement.
- The report draft `docs/performance/README.md`, the README sections Performance and Database & Indexes, decisions 24–26.

### Reviewed / decided manually

- עברתי על תרשימי תוכניות הביצוע לפני ואחרי האינדקסים, והשוויתי אותם לטבלת המדידות. התרשימים נוצרו בעזרת סקריפט מקבצי ה-.sqlplan שב-docs/performance/plans, ולא צולמו מ-SSMS.
- למדתי מהדוח שבעמוד עמוק האינדקס דווקא הגדיל את מספר הקריאות, ושלכן Keyset Pagination מוצע כשיפור.

## Step G – Angular screens (T7.0, T7.2–T7.8)

The UI design (6 screens: Main, StatusDialog, Conflict, History, BulkResult, States) and its design system were
created with **Claude Design** (`docs/design/*.dc.html`); the colors come from the leaves of the Ministry of
Environmental Protection logo.

### Generated with Claude Code

- Design tokens (`_tokens.scss`), the Angular Material theme mapping and the shared control and dialog styles.
- `RequestsListState` (query reducer, debounce, switchMap, URL sync, list and stats view models, selection) and
  `RequestStatusActions` (status update, 409 conflict flow, bulk update).
- Components: requests-page, page-header, stats-cards, requests-filters, active-filters, bulk-action-bar,
  requests-table, requests-paginator, list-state; dialogs: status-change, conflict, history-panel, bulk-result,
  user-name; shared StatusBadge, PriorityLabel, Icon, labels and date helpers.
- `CurrentUserService` and the `X-User-Name` interceptor, plus the backend change that decodes the
  percent-encoded header (`CurrentUserAccessor`) and its integration test.
- Tests: `requests-list.state.spec.ts`, `request-status-actions.spec.ts`, new cases in `error.interceptor.spec.ts`.
- Browser verification against the seeded API (network log of debounce, cancellation, sort/filter/page requests,
  status change, conflict from a second client, bulk, empty and error states) and the documentation updates.

### Reviewed / decided manually

- Noticed in the UI that an InProgress request had no handler, and decided on auto-assignment instead of a full assignment feature.
- Decided to extend the search to the request number (the requirement says 'at least' Title and OrganizationName), and not to more text fields, because each extra LIKE field adds scan cost.
- עיצבתי את המסכים ב-Claude Design, ובחרתי את הצבעים מעלי הלוגו של המשרד להגנת הסביבה
- השוויתי את המימוש לעיצוב ואישרתי את הסטיות: בורר תאריכים של הדפדפן, ועדכון מרובה בלי סטטוס מסומן מראש, כדי למנוע שינוי של פניות רבות בטעות.
- החלטתי לא להוסיף יצירה, עריכה ומחיקה של פניות, כי הן מחוץ לדרישות המבדק.

## Step H – Submission (T10.x)

### Generated with Claude Code

- One-command run: `backend/Dockerfile`, `frontend/Dockerfile` and `frontend/nginx.conf`, the `api` and `web` services in `docker-compose.yml`, and the startup options `Database:MigrateOnStartup` / `Seed:OnStartupIfEmpty` (`DatabaseStartup`).
- CI: `.github/workflows/ci.yml` (backend build and tests with Testcontainers, frontend tests and production build).
- Cleanup: removed the placeholder test, explicit `api/health` route, outdated wording in the docs, `.gitignore` for CI test results; the design images renamed to English file names.
- The README restructure (landing page, quick start, table for reviewers, screenshots, design, technical decisions, AI usage), `docs/images/README.md`, decisions 35–37 and the work plan updates.
- Verification of the Docker run, which found a restart bug: the SQL Server healthcheck passed while `RequestsDb` was still recovering, so the startup migration tried to create the database again. The healthcheck now waits until every database is `ONLINE`.

### Reviewed / decided manually

- כשהכונן התמלא ו-Docker נתקע, עצרתי את העבודה לפני ה-Commit, פיניתי מקום והפעלתי מחדש.
- אישרתי את שינוי טעינת הגופן: בגלל סינון התעבורה (NetFree), הבנייה ב-Docker לא יכלה להוריד אותו.
- הרצתי בעצמי את המערכת ב-Docker, ובדקתי שהיא עובדת.
- צילמתי את המסכים, ובדקתי שהבדיקות עוברות (102 בשרת ו-14 ב-Angular).
