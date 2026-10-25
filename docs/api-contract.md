# API Contract

The contract between the backend (`backend/src/Requests.Api`) and the Angular client (`frontend/`).
Both sides follow this document; the TypeScript models in `frontend/src/app/models/` mirror it exactly.

Base path: `/api`. All request and response bodies are JSON (`application/json`), property names in camelCase.

## Common rules

- **Enums are serialized as strings**, never as numbers:
  - `RequestStatus`: `New` | `InProgress` | `Waiting` | `Completed`
  - `RequestPriority`: `Low` | `Medium` | `High`
- **`version`** is the SQL Server `rowversion` of the request, sent as a **Base64 string** (e.g. `"AAAAAAAAB9E="`).
  The client treats it as opaque and returns it **unchanged** in update requests.
- **All dates are UTC, ISO 8601** with a `Z` suffix (e.g. `"2026-09-14T08:30:00Z"`). Date query parameters
  (`createdFrom`, `createdTo`) are ISO 8601 too; a date without a time (`2026-09-14`) means midnight UTC.
- **All errors use RFC 7807 ProblemDetails** (`application/problem+json`):

  ```json
  {
    "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
    "title": "One or more validation errors occurred.",
    "status": 400,
    "detail": "Optional human-readable explanation.",
    "errors": { "pageSize": ["pageSize must be between 1 and 100."] }
  }
  ```

  `errors` (field → messages) is present on validation errors only. Some errors add extension members
  (see `current` on 409).

## RequestDto

```json
{
  "id": 1234,
  "title": "בקשה לפינוי פסולת",
  "organizationName": "עיריית חיפה",
  "status": "InProgress",
  "priority": "High",
  "assignedTo": "דנה לוי",
  "createdAt": "2026-03-02T09:15:00Z",
  "updatedAt": "2026-03-05T14:40:12Z",
  "version": "AAAAAAAAB9E="
}
```

| Field | Type | Notes |
|---|---|---|
| `id` | integer | |
| `title` | string | |
| `organizationName` | string | |
| `status` | `RequestStatus` | |
| `priority` | `RequestPriority` | |
| `assignedTo` | string \| `null` | `null` when not assigned |
| `createdAt` | string (UTC ISO 8601) | |
| `updatedAt` | string (UTC ISO 8601) | |
| `version` | string (Base64) | Concurrency token, see Common rules |

---

## GET /api/requests

Paged, filtered and sorted list of requests.

### Query parameters

All parameters are optional.

| Name | Type | Default | Allowed values / rules |
|---|---|---|---|
| `page` | integer | `1` | ≥ 1 |
| `pageSize` | integer | `20` | 1–100 |
| `search` | string | – | Free text; matches `title` **or** `organizationName` (contains) |
| `status` | `RequestStatus` | – | `New`, `InProgress`, `Waiting`, `Completed` |
| `priority` | `RequestPriority` | – | `Low`, `Medium`, `High` |
| `organizationName` | string | – | Exact match |
| `assignedTo` | string | – | Exact match |
| `createdFrom` | ISO 8601 date | – | Inclusive lower bound on `createdAt` |
| `createdTo` | ISO 8601 date | – | Inclusive upper bound on `createdAt`; must be ≥ `createdFrom` |
| `sortBy` | string | `createdAt` | `createdAt`, `updatedAt`, `priority`, `title`, `organizationName` |
| `sortDirection` | string | `desc` | `asc`, `desc` |

Example: `GET /api/requests?page=2&pageSize=50&status=Waiting&sortBy=priority&sortDirection=desc`

### Response `200 OK`

```json
{
  "items": [ { "id": 1234, "title": "...", "organizationName": "...", "status": "Waiting", "priority": "High", "assignedTo": null, "createdAt": "...", "updatedAt": "...", "version": "AAAAAAAAB9E=" } ],
  "totalCount": 9873,
  "page": 2,
  "pageSize": 50
}
```

`items` is a `RequestDto[]`. A page beyond the last one returns `200` with an empty `items` array.

### Status codes

| Code | When |
|---|---|
| 200 | Success |
| 400 | Invalid parameter (e.g. `pageSize=500`, unknown `status` or `sortBy`, unparseable date, `createdTo < createdFrom`) — ProblemDetails with `errors` |

---

## GET /api/requests/{id}

### Response `200 OK`

A single `RequestDto`.

### Status codes

| Code | When |
|---|---|
| 200 | Success |
| 404 | No request with this `id` |

---

## GET /api/requests/stats

Global aggregations (not affected by list filters).

The result is cached on the server for at most 60 seconds (`Cache:StatsSeconds`) and dropped after every
successful status change through `PATCH …/status` or `POST …/bulk-status`, so changes made through the API
are visible on the next call.

### Response `200 OK`

```json
{
  "byStatus": { "New": 15000, "InProgress": 25000, "Waiting": 10000, "Completed": 50000 },
  "byPriority": { "Low": 50000, "Medium": 35000, "High": 15000 },
  "total": 100000
}
```

Every enum value is always present as a key (`0` when there are no requests in it).

### Status codes

| Code | When |
|---|---|
| 200 | Success |

---

## GET /api/requests/{id}/history

Status change history of one request, **newest first** (ordered by `changedAt` descending; rows with the same
`changedAt` by insertion order, newest first). Every successful `PATCH …/status` adds exactly one row, in the
same transaction as the change. Rows from the seeded test data have `changedBy: "system"` for the first step.

### Response `200 OK`

```json
[
  { "previousStatus": "InProgress", "newStatus": "Completed", "changedAt": "2026-03-05T14:40:12Z", "changedBy": "דנה לוי" },
  { "previousStatus": "New", "newStatus": "InProgress", "changedAt": "2026-03-03T10:02:00Z", "changedBy": "דנה לוי" }
]
```

| Field | Type |
|---|---|
| `previousStatus` | `RequestStatus` |
| `newStatus` | `RequestStatus` |
| `changedAt` | string (UTC ISO 8601) |
| `changedBy` | string |

A request that was never changed returns `200` with `[]`.

### Status codes

| Code | When |
|---|---|
| 200 | Success |
| 404 | No request with this `id` |

---

## PATCH /api/requests/{id}/status

Change the status of one request, with optimistic concurrency.

### Headers

| Header | Required | Notes |
|---|---|---|
| `X-User-Name` | No | Stored as `changedBy` in the history row. **Percent-encoded UTF-8** (`encodeURIComponent` in the browser, e.g. `%D7%93%D7%A0%D7%94` for `דנה`): browsers refuse raw header values with characters such as Hebrew letters. The server decodes it, then trims and cuts it to 100 characters; missing or empty → `"anonymous"`. Plain ASCII values without `%` arrive unchanged. There is no authentication yet. |

### Body

```json
{ "newStatus": "Completed", "version": "AAAAAAAAB9E=" }
```

| Field | Type | Rules |
|---|---|---|
| `newStatus` | `RequestStatus` | Required; the transition from the current status must be allowed |
| `version` | string (Base64) | Required; the `version` the client last received for this request (Base64 of 8 bytes) |

### Allowed transitions

| From | Allowed to |
|---|---|
| `New` | `InProgress` |
| `InProgress` | `Waiting`, `Completed` |
| `Waiting` | `InProgress`, `Completed` |
| `Completed` | – (final) |

Changing a status to itself is not allowed.

### Order of checks

The checks run in this order; the first one that fails decides the response.

| # | Check | Failure |
|---|---|---|
| 1 | Body is valid (`newStatus` is a known value, `version` is present and valid Base64 of 8 bytes) | `400` ValidationProblemDetails with `errors` |
| 2 | The request exists | `404` ProblemDetails |
| 3 | `version` equals the current version | `409` with `current` |
| 4 | The transition is allowed (table above) | `400` ProblemDetails, title `Invalid status transition` |
| 5 | Save: the database `UPDATE` matches the row only if the version is still the same | `409` with `current` (someone saved between check 3 and the save) |

The version check (3) comes before the transition rule (4): when the client's data is stale, a transition
error would be misleading; the current state is the actionable answer.

### Response `200 OK`

The updated `RequestDto` (with the new `status`, `updatedAt` and `version`, and `assignedTo` if it was auto-assigned).

### Auto-assignment

When the change moves a request from `New` to `InProgress` and the request has no handler
(`assignedTo` is `null`), the user who made the change (the decoded `X-User-Name`, the same value stored as
`changedBy`) becomes its handler. Rules:

- `"anonymous"` (no or empty `X-User-Name`) assigns nobody: `assignedTo` stays `null`.
- An existing `assignedTo` is never overwritten.
- No other transition changes `assignedTo`.
- The handler is written in the same transaction as the status change and the history row, under the same
  `version` check: a `409` or any other failure assigns nobody.

### Status codes

| Code | When |
|---|---|
| 200 | Updated |
| 400 | Invalid body (missing/unknown `newStatus`, missing or malformed `version`) — ValidationProblemDetails with `errors`; or the transition is not allowed — ProblemDetails with `detail` |
| 404 | No request with this `id` |
| 409 | Conflict: `version` does not match (someone else changed the request). ProblemDetails with the current state under `current` |

`400` body for a transition that is not allowed:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Invalid status transition",
  "status": 400,
  "detail": "Cannot change status from Completed to New. Allowed from Completed: none (Completed is final).",
  "traceId": "00-e95ece38406873d03e0f6057b82fa307-a7e95ff184c582fc-00"
}
```

`409` body:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "The request was modified by another user",
  "status": 409,
  "detail": "The version you sent is out of date. See 'current' for the latest state.",
  "traceId": "00-e9f7592e727e40d68f2de36682aa029a-ee2d12f315ca83ec-00",
  "current": { "id": 4088, "title": "...", "organizationName": "Galil Systems Ltd", "status": "Waiting", "priority": "Low", "assignedTo": "יעל ברק", "createdAt": "2024-10-01T01:15:48Z", "updatedAt": "2026-10-25T00:15:46.9581555Z", "version": "AAAAAAAGKIc=" }
}
```

`current` is a `RequestDto`. The client should show the user the current state and, if the user still wants
the change, send it again with `current.version`.

---

## POST /api/requests/bulk-status

Change the status of many requests in one call. **Partial success**: each item succeeds or fails on its own.

### Body

```json
{
  "newStatus": "Completed",
  "items": [
    { "id": 1234, "version": "AAAAAAAAB9E=" },
    { "id": 1235, "version": "AAAAAAAAB9I=" }
  ]
}
```

| Field | Type | Rules |
|---|---|---|
| `newStatus` | `RequestStatus` | Required; the same target status for every item |
| `items` | array of `{ id: integer, version: string }` | 1–100 items, no duplicate `id`s; each `version` is the one the client last received for that request |

The optional `X-User-Name` header is stored as `changedBy` in the history row of every updated item (same
rules as for `PATCH …/status`).

### Request-level validation

If any of these rules fails, the whole call is rejected with `400` ValidationProblemDetails and **nothing is
touched in the database**:

| Rule | Error key | Example message |
|---|---|---|
| `newStatus` is present and a known value | `newStatus` | `newStatus must be one of: New, InProgress, Waiting, Completed.` |
| `items` is present and not empty | `items` | `items must contain at least one item.` |
| at most 100 items | `items` | `items must contain at most 100 items.` |
| no duplicate `id`s | `items` | `items must not contain duplicate ids: 5.` |
| every `version` is valid Base64 of 8 bytes | `items[i].version` | `version must be the Base64 value received from the API.` |

### Per-item processing and outcomes

All requests are loaded with one query. Then each item, **in input order**, goes through the same checks, in
the same order, as `PATCH …/status`, and a successful item is saved **in its own transaction** together with
its history row. A failed item never rolls back the items saved before it, and does not prevent the items
after it.

The [auto-assignment rule](#auto-assignment) of `PATCH …/status` applies to every item: with
`newStatus: "InProgress"`, each `New` item without a handler gets the user from `X-User-Name` as its handler
(not for `"anonymous"`, never overwriting an existing handler), in the same transaction as that item's
status change. The results do not include `assignedTo`; the client reloads the list to see it.

| Outcome | When | Extra fields |
|---|---|---|
| `Updated` | The status was changed and a history row was written | `version`: the new version |
| `NotFound` | No request with this `id` | `message` |
| `Conflict` | `version` is out of date, or someone saved the request between the load and the save | `message` |
| `InvalidTransition` | The transition from the current status to `newStatus` is not allowed (see the transitions table) | `message` naming the current status, the requested one and the allowed targets |

### Response `200 OK`

Returned **whenever the request itself is valid**, even if every item failed.

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

| Field | Type | Notes |
|---|---|---|
| `succeeded` | integer | Number of `Updated` results |
| `failed` | integer | Number of other results |
| `results[].id` | integer | |
| `results[].outcome` | string | `Updated` \| `NotFound` \| `Conflict` \| `InvalidTransition` |
| `results[].version` | string (Base64), optional | Present on `Updated`: the new version of the request |
| `results[].message` | string, optional | Present on failures |

`results` has exactly one entry per item, **in the order of the request**. To retry a `Conflict`, the client
reloads the request (`GET /api/requests/{id}`) to get its current state and version.

### Status codes

| Code | When |
|---|---|
| 200 | The request was valid and processed (even if some or all items failed — see `results`) |
| 400 | The request itself is invalid (rules above) — ValidationProblemDetails; nothing was changed |

Why `200` and not `207 Multi-Status`: `207` comes from WebDAV and is poorly supported by HTTP clients; the
body already carries the outcome of every item.

---

## GET /api/health

Liveness check used by the client's connection indicator.

`200 OK` → `{ "status": "ok" }`
