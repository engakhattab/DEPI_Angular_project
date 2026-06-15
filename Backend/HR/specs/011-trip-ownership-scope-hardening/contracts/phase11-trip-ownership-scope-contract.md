# Phase 11 Trip Ownership Scope Contract

This contract documents externally visible trip endpoint behavior after Phase 11. Routes and successful response DTO compatibility remain unchanged.

## Shared Error Shape

Expected structured error shape:

```json
{
  "code": "FORBIDDEN",
  "message": "Forbidden"
}
```

Existing error codes remain compatible. Phase 11 does not normalize legacy codes.

## Authentication Boundary

- Missing or invalid login cookie: `401 Unauthorized`.
- Missing or invalid `employee_id` claim for trip endpoints: `401 Unauthorized` with structured payload.
- Authenticated but forbidden by role/scope: `403 Forbidden` with structured payload.
- Missing trip ID for detail or delete: existing `404 NOT_FOUND`.
- Existing but out-of-scope trip ID: `403 FORBIDDEN` before mutation or domain-rule validation.

## Access Matrix

| Operation | Employee | Manager | HRAdministrator | SystemAdministrator |
|-----------|----------|---------|-----------------|---------------------|
| `GET /api/trips` | Own trips only | Own plus active direct/indirect team trips | Organization-wide | Organization-wide |
| `GET /api/trips/{id}` | Own trip only | Own plus active direct/indirect team trips | Any existing trip | Any existing trip |
| `POST /api/trips` | Create for self only | Create for self or active direct/indirect team member | Create for any eligible employee | Create for any eligible employee |
| `DELETE /api/trips/{id}` | Own trip only | Own or active team trip only | Any existing trip | Any existing trip |

## `GET /api/trips`

Existing request query remains supported:

| Query | Type | Required | Notes |
|-------|------|----------|-------|
| `page` | integer | No | Existing default remains. |
| `pageSize` | integer | No | Existing max/default behavior remains. |

Optional additive query:

| Query | Type | Required | Notes |
|-------|------|----------|-------|
| `travelerEmployeeId` | GUID | No | Narrows list to one traveler inside requester scope. Existing clients can omit it. |

Success response remains the existing paged trip response shape. Any added requester/traveler metadata must be additive.

Role behavior:

- `Employee`: returns own trips only. If `travelerEmployeeId` is supplied for another employee, returns an empty scoped page.
- `Manager`: returns trips for self and active direct/indirect reports only.
- `Manager` with `travelerEmployeeId` for peer, unrelated employee, soft-deleted report, or terminated report: returns an empty scoped page.
- `HRAdministrator`: organization-wide results, subject to filters.
- `SystemAdministrator`: organization-wide results, subject to filters.

Scope filtering occurs before pagination and total-count calculation.

Out-of-scope list filters:

- If a scoped list request includes a traveler filter outside the authenticated user's allowed scope, the response is an empty scoped page/list.
- The response preserves the normal pagination envelope.
- The response does not reveal whether the out-of-scope employee exists.
- The response must not include unauthorized trips or employee data.

## `GET /api/trips/{id}`

Success response remains `TripResponse`.

Failure behavior:

- missing trip ID: existing `404 NOT_FOUND`
- existing but out-of-scope trip ID: `403 FORBIDDEN`

Role behavior:

- `Employee`: own trip only.
- `Manager`: own trip plus active direct/indirect team trip only.
- `HRAdministrator`: any existing trip.
- `SystemAdministrator`: any existing trip.

## `POST /api/trips`

Request body remains:

```json
{
  "requestedByEmployeeId": "00000000-0000-0000-0000-000000000000",
  "referenceName": "Site visit",
  "project": "Client onboarding",
  "route": "Cairo - Alexandria",
  "tripType": "Business",
  "tripDate": "2026-06-22"
}
```

Compatibility semantics:

- `requestedByEmployeeId` remains accepted.
- In Phase 11 it is the target traveler employee ID for compatibility.
- The authenticated requester comes from the session claims and service context, not from the body.

Success response remains `201 Created` with `TripResponse`. Existing response fields remain stable.

Role behavior:

- `Employee`: allowed only when `requestedByEmployeeId` equals requester employee ID.
- `Manager`: allowed only when `requestedByEmployeeId` equals requester employee ID or an active direct/indirect report employee ID.
- `HRAdministrator`: allowed for any target employee that passes existing trip eligibility and validation.
- `SystemAdministrator`: allowed for any target employee that passes existing trip eligibility and validation.

Requester behavior:

- After approved requester-tracking storage exists, new rows set `RequesterEmployeeId` to the authenticated requester.
- Before that approved migration, Phase 11 must not fake persisted requester metadata.

## `DELETE /api/trips/{id}`

Success response remains `204 No Content`.

Failure behavior:

- missing trip ID: existing `404 NOT_FOUND`
- existing but out-of-scope trip ID: `403 FORBIDDEN`

Role behavior:

- `Employee`: own trip only.
- `Manager`: own or active direct/indirect team trip only.
- `HRAdministrator`: any existing trip.
- `SystemAdministrator`: any existing trip.

Current hard-delete behavior is preserved for in-scope trips after authorization succeeds.

## Compatibility Guarantees

Phase 11 must not change:

- route templates
- existing request DTO field names
- existing successful response DTO fields
- pagination envelope shape
- cookie authentication behavior
- existing login or `/api/auth/me` response behavior
- existing claims
- structured error shape
- employee endpoint behavior
- vacation endpoint behavior
- compensation, document, dashboard, attendance, audit, bootstrap, or Swagger behavior

Any requester/traveler response fields added after migration approval must be additive and must handle null requester metadata for historical rows.
