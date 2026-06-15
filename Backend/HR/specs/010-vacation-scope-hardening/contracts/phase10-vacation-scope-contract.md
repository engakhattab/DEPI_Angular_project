# Phase 10 Vacation Scope Contract

This contract documents externally visible vacation request endpoint behavior after Phase 10. Routes and successful response DTO compatibility remain unchanged.

## Shared Error Shape

Expected structured error shape:

```json
{
  "code": "FORBIDDEN",
  "message": "Forbidden"
}
```

Existing error codes remain compatible. Phase 10 does not normalize legacy codes.

## Authentication Boundary

- Missing or invalid login cookie: `401 Unauthorized`.
- Missing or invalid `employee_id` claim for vacation endpoints: `401 Unauthorized` with structured payload.
- Authenticated but forbidden by role/scope: `403 Forbidden` with structured payload.
- Missing vacation request ID for detail, review, or delete: existing `404 NOT_FOUND`.
- Existing but out-of-scope vacation request ID: `403 FORBIDDEN` before domain-rule validation.

## Access Matrix

| Operation | Employee | Manager | HRAdministrator | SystemAdministrator |
|-----------|----------|---------|-----------------|---------------------|
| `GET /api/vacationrequests` | Own requests only | Active direct/indirect team requests by default; own requests only with `employeeId` self filter | Organization-wide | Organization-wide |
| `GET /api/vacationrequests/{id}` | Own request only | Self plus active direct/indirect team requests | Any existing request | Any existing request |
| `POST /api/vacationrequests` | Create for self only | Create for self only | Create for any eligible employee | Create for any eligible employee |
| `PUT /api/vacationrequests/{id}/status` | `403` | Review active direct/indirect team requests only; self-review forbidden | Review any non-self request | Review any non-self request |
| `DELETE /api/vacationrequests/{id}` | Own pending request only | Own pending request only; no team delete | Any pending request | Any pending request |

## `GET /api/vacationrequests`

Request query remains:

| Query | Type | Required | Notes |
|-------|------|----------|-------|
| `status` | `VacationRequestStatus` | No | Existing status filter. |
| `employeeId` | GUID | No | Existing owner filter; applied within requester scope. |
| `page` | integer | No | Existing default remains. |
| `pageSize` | integer | No | Existing max/default behavior remains. |

Success response remains the existing paged vacation request response shape.

Role behavior:

- `Employee`: returns own vacation requests only. If `employeeId` is supplied for another employee, returns an empty scoped page.
- `Manager`: unfiltered list returns active direct and indirect team-owned requests only and excludes manager-owned requests.
- `Manager` with `employeeId` equal to self: returns only manager-owned vacation requests.
- `Manager` with `employeeId` for active direct/indirect report: returns only that report's requests.
- `Manager` with `employeeId` outside self/team scope: returns an empty scoped page.
- `HRAdministrator`: organization-wide results, subject to filters.
- `SystemAdministrator`: organization-wide results, subject to filters.

Scope filtering occurs before pagination and total-count calculation.

Out-of-scope list filters:

- If a scoped list request includes an `employeeId`, manager, requester, or target-style filter outside the authenticated user's allowed scope, the response is an empty scoped page/list.
- The response preserves the normal pagination envelope.
- The response does not reveal whether the out-of-scope employee exists.
- The response must not include unauthorized vacation requests or employee data.

## `GET /api/vacationrequests/{id}`

Success response remains `VacationRequestResponse`.

Failure behavior:

- missing vacation request ID: existing `404 NOT_FOUND`
- existing but out-of-scope vacation request ID: `403 FORBIDDEN`

Role behavior:

- `Employee`: own request only.
- `Manager`: own request plus active direct/indirect team request only.
- `HRAdministrator`: any existing vacation request.
- `SystemAdministrator`: any existing vacation request.

## `POST /api/vacationrequests`

Request body remains:

```json
{
  "employeeId": "00000000-0000-0000-0000-000000000000",
  "startDate": "2026-06-22",
  "endDate": "2026-06-24",
  "reason": "Annual leave"
}
```

Success response remains `201 Created` with `VacationRequestResponse`.

Role behavior:

- `Employee`: allowed only when `employeeId` equals requester employee ID; another employee ID returns `403` before target lookup.
- `Manager`: allowed only when `employeeId` equals requester employee ID; team or outside employee ID returns `403` before target lookup.
- `HRAdministrator`: allowed for any target employee that passes existing vacation eligibility and validation.
- `SystemAdministrator`: allowed for any target employee that passes existing vacation eligibility and validation.

Creator behavior:

- After approved creator-tracking storage exists, new rows set `CreatedByEmployeeId` to the authenticated requester.
- Before that approved migration, Phase 10 must not fake persisted creator metadata.

## `PUT /api/vacationrequests/{id}/status`

Request body remains:

```json
{
  "status": "Approved"
}
```

Success response remains `200 OK` with `VacationRequestResponse`.

Failure behavior:

- missing vacation request ID: existing `404 NOT_FOUND`
- existing but out-of-scope vacation request ID: `403 FORBIDDEN`
- self-review attempt: existing business-rule style structured error
- invalid transition after authorization: existing business-rule style structured error

Role behavior:

- `Employee`: `403 Forbidden`.
- `Manager`: active direct/indirect team requests only.
- `HRAdministrator`: any non-self request.
- `SystemAdministrator`: any non-self request.

Same-status behavior:

- Same-status review remains idempotent no-op success.
- It must not duplicate side effects.
- It must not update timestamps again.
- It must not reject merely because the requested status is already current.

## `DELETE /api/vacationrequests/{id}`

Success response remains `204 No Content`.

Failure behavior:

- missing vacation request ID: existing `404 NOT_FOUND`
- existing but out-of-scope vacation request ID: `403 FORBIDDEN`
- existing in-scope non-pending request: existing business-rule style structured error

Role behavior:

- `Employee`: own pending request only.
- `Manager`: own pending request only; no team delete.
- `HRAdministrator`: any pending request.
- `SystemAdministrator`: any pending request.

## Compatibility Guarantees

Phase 10 must not change:

- route templates
- request DTO field names
- existing successful response DTO fields
- pagination envelope shape
- cookie authentication behavior
- existing login or `/api/auth/me` response behavior
- existing claims
- structured error shape
- employee endpoint behavior
- trip endpoint behavior
- compensation, document, dashboard, attendance, audit, or bootstrap behavior
- Swagger/OpenAPI documentation behavior
