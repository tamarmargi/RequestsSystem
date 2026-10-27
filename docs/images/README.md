# Screenshots checklist

Images used by the root [README.md](../../README.md). Take them with the full system running
(`docker compose up --build`, UI at http://localhost:4200) on the seeded data, in a browser window about
1440 px wide, unless noted otherwise.

## docs/images/

| Done | File | What it must show | Used in README |
|---|---|---|---|
| [ ] | `requests-page.png` | The main screen: header, stats cards, search box, filters and the first page of the table with data | Top of the page, Screenshots, Design (Main) |
| [ ] | `status-dialog.png` | The "שינוי סטטוס" dialog of one request, showing the current status and only the allowed target statuses | Screenshots, Design (Status change) |
| [ ] | `conflict.png` | The conflict (409) dialog: "השינוי שלך" next to "המצב הנוכחי בשרת", who changed it and when, and both buttons | Screenshots, Design (Conflict), Conflict handling in the UI |
| [ ] | `history.png` | The history side panel of a request with several changes, newest first | Screenshots, Design (History) |
| [ ] | `bulk-result.png` | The bulk result dialog with a mix of outcomes (updated, conflict, invalid transition) | Screenshots, Design (Bulk result) |
| [ ] | `empty-state.png` | A search or filter with no results: "לא נמצאו פניות…" with "ניקוי כל המסננים" | Screenshots, Design (States) |
| [ ] | `error-state.png` | The error state with "ניסיון חוזר" (e.g. with the API container stopped: `docker compose stop api`) | Screenshots, Design (States) |
| [ ] | `swagger.png` | Swagger UI at http://localhost:5000/swagger with all 7 endpoints | Screenshots |
| [ ] | `tests.png` | Passing test runs: `dotnet test backend/RequestsSystem.sln` and `cd frontend && npm test` (or the green CI run) | Screenshots |
| [ ] | `conflict-demo.gif` | Animated: two browser tabs on the same request; the first changes the status, the second then tries and gets the conflict dialog. Caption: "שתי לשוניות מעדכנות את אותה פנייה: השנייה מקבלת התנגשות" | Screenshots |

## docs/performance/images/

Execution plans from SSMS or the VS Code mssql extension: open the matching `.sqlplan` file from
[docs/performance/plans](../performance/plans/) and capture the graphical plan.

| Done | File | Plan file | Used in |
|---|---|---|---|
| [ ] | `q1-before.png` | `plans/before/Q1-page.sqlplan` (Clustered Index Scan + parallel Sort) | README Screenshots, performance report |
| [ ] | `q1-after.png` | `plans/after/Q1-page.sqlplan` (Index Scan of `IX_Requests_CreatedAt` + Key Lookups) | README Screenshots, performance report |
| [ ] | `q2-before.png` | `plans/before/Q2-page.sqlplan` (Clustered Index Scan + Sort) | README Screenshots, performance report |
| [ ] | `q2-after.png` | `plans/after/Q2-page.sqlplan` (Index Seek of `IX_Requests_Status_CreatedAt`) | README Screenshots, performance report |
| [ ] | `q5-before.png` | `plans/before/Q5-stats.sqlplan` (Clustered Index Scan) | README Screenshots, performance report |
| [ ] | `q5-after.png` | `plans/after/Q5-stats.sqlplan` (Index Scan of `IX_Requests_Status_Priority`) | README Screenshots, performance report |

## docs/design/ (already present)

`Main.png`, `StatusDialog.png`, `Conflict.png`, `History.png`, `BulkResult.png`, `States.png`: the Claude Design
screens, shown next to the implemented screens in the README section *Design*.
