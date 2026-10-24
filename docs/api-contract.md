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

Status change history of one request, **newest first**.

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

### Body

```json
{ "newStatus": "Completed", "version": "AAAAAAAAB9E=" }
```

| Field | Type | Rules |
|---|---|---|
| `newStatus` | `RequestStatus` | Required; the transition from the current status must be allowed |
| `version` | string (Base64) | Required; the `version` the client last received for this request |

### Response `200 OK`

The updated `RequestDto` (with the new `status`, `updatedAt` and `version`).

### Status codes

| Code | When |
|---|---|
| 200 | Updated |
| 400 | Invalid body (missing/unknown `newStatus`, missing or non-Base64 `version`), or the transition is not allowed — ProblemDetails |
| 404 | No request with this `id` |
| 409 | Conflict: `version` does not match (someone else changed the request). ProblemDetails with the current state under `current` |

`409` body:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "The request was modified by another user.",
  "status": 409,
  "detail": "The version you sent is out of date.",
  "current": { "id": 1234, "title": "...", "organizationName": "...", "status": "Waiting", "priority": "High", "assignedTo": "דנה לוי", "createdAt": "...", "updatedAt": "2026-03-06T08:00:00Z", "version": "AAAAAAAAB+A=" }
}
```

`current` is a `RequestDto`. The client may retry with `current.version`.

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
| `newStatus` | `RequestStatus` | Required |
| `items` | array of `{ id: integer, version: string }` | 1–100 items, no duplicate `id`s |

### Response `200 OK`

```json
{
  "succeeded": 1,
  "failed": 1,
  "results": [
    { "id": 1234, "outcome": "Updated" },
    { "id": 1235, "outcome": "Conflict", "message": "The request was modified by another user." }
  ]
}
```

| Field | Type | Notes |
|---|---|---|
| `succeeded` | integer | Number of `Updated` results |
| `failed` | integer | Number of other results |
| `results[].id` | integer | |
| `results[].outcome` | string | `Updated` \| `NotFound` \| `Conflict` \| `InvalidTransition` |
| `results[].message` | string, optional | Present on failures |

`results` has one entry per item, in the order of the request.

### Status codes

| Code | When |
|---|---|
| 200 | The request was processed (even if some or all items failed — see `results`) |
| 400 | The request itself is invalid: missing/unknown `newStatus`, 0 or more than 100 items, duplicate `id`s, invalid `version` — ProblemDetails |

---

## GET /api/health

Liveness check used by the client's connection indicator.

`200 OK` → `{ "status": "ok" }`
