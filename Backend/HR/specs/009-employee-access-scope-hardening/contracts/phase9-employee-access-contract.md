# Phase 9 Employee Access Contract

This contract documents externally visible employee endpoint behavior after Phase 9. Routes and successful response DTO shapes remain unchanged.

## Shared Error Shape

Expected structured error shape:

```json
{
  "code": "FORBIDDEN",
  "message": "Forbidden"
}
```

Existing error codes remain compatible. Phase 9 does not normalize legacy codes.

## Authentication Boundary

- Missing or invalid login cookie: `401 Unauthorized`.
- Missing or invalid `employee_id` claim for employee endpoints: `401 Unauthorized` with structured payload.
- Authenticated but forbidden by role/scope: `403 Forbidden` with structured payload.

## Access Matrix

| Operation | Employee | Manager | HRAdministrator | SystemAdministrator |
|-----------|----------|---------|-----------------|---------------------|
| `GET /api/employees` | `403` | Active direct/indirect reports only | Organization-wide including terminated/soft-deleted | Organization-wide including terminated/soft-deleted |
| `GET /api/employees/{id}` | Self only | Self plus active direct/indirect reports | Any existing employee including terminated/soft-deleted | Any existing employee including terminated/soft-deleted |
| `POST /api/employees` | `403` | `403` | Allowed | Allowed |
| `PUT /api/employees/{id}` | `403` | `403` | Non-`SystemAdministrator` targets only | Allowed except last-active-SystemAdministrator removal |
| `DELETE /api/employees/{id}` | `403` | `403` | Non-`SystemAdministrator` targets only | Allowed except last-active-SystemAdministrator removal |
| `PUT /api/employees/{id}/role` | `403` | `403` | `403` | Allowed except last-active-SystemAdministrator demotion |

## `GET /api/employees`

Request query remains:

| Query | Type | Required | Notes |
|-------|------|----------|-------|
| `status` | `EmployeeStatus` | No | Existing status filter. |
| `page` | integer | No | Existing default remains. |
| `pageSize` | integer | No | Existing max/default behavior remains. |

Success response remains the existing paged employee response shape.

Role behavior:

- `Employee`: `403 Forbidden`.
- `Manager`: returns active direct and indirect reports only. Manager self is not included by list unless they are also in the report set, which should not happen.
- `HRAdministrator`: returns organization-wide records including active, suspended, terminated, and soft-deleted employees.
- `SystemAdministrator`: returns organization-wide records including active, suspended, terminated, and soft-deleted employees.

Scope filtering occurs before pagination and total count calculation.

## `GET /api/employees/{id}`

Success response remains `EmployeeResponse`.

Failure behavior:

- missing employee ID: existing `404 NOT_FOUND`
- existing but out-of-scope employee ID: `403 FORBIDDEN`

Role behavior:

- `Employee`: self only.
- `Manager`: self plus active direct/indirect reports only.
- `HRAdministrator`: any existing employee, including terminated/soft-deleted.
- `SystemAdministrator`: any existing employee, including terminated/soft-deleted.

## `POST /api/employees`

Request and success response remain unchanged.

Role behavior:

- `Employee`: `403 Forbidden`
- `Manager`: `403 Forbidden`
- `HRAdministrator`: allowed, subject to existing employee business rules
- `SystemAdministrator`: allowed, subject to existing employee business rules

## `PUT /api/employees/{id}`

Request and success response remain unchanged.

Role behavior:

- `Employee`: `403 Forbidden`
- `Manager`: `403 Forbidden`
- `HRAdministrator`: allowed only for non-`SystemAdministrator` targets
- `SystemAdministrator`: allowed unless the update/status change would leave zero active system administrators

Existing business-rule failures remain in force after authorization passes.

## `DELETE /api/employees/{id}`

Success response remains `204 No Content`.

Role behavior:

- `Employee`: `403 Forbidden`
- `Manager`: `403 Forbidden`
- `HRAdministrator`: allowed only for non-`SystemAdministrator` targets
- `SystemAdministrator`: allowed unless deletion would leave zero active system administrators

Existing soft-delete behavior remains in force after authorization passes.

## `PUT /api/employees/{id}/role`

Role assignment remains `SystemAdministrator` only.

Phase 9 confirms this protection and does not add a new route or broaden role-assignment authority.

Additional last-active-SystemAdministrator behavior:

- If the target employee is an active `SystemAdministrator`, assigning any non-`SystemAdministrator` role must be rejected before mutation when that target is the only active `SystemAdministrator`.
- If at least one other active `SystemAdministrator` remains, demoting a `SystemAdministrator` may be allowed only when all existing role-assignment rules pass.
- The failure response should follow the project's existing business-rule/conflict convention with the standard structured `{ code, message }` payload; Phase 9 must not normalize existing error codes.

## Compatibility Guarantees

Phase 9 must not change:

- route templates
- successful response DTO fields
- pagination envelope shape
- cookie authentication behavior
- existing login or `/api/auth/me` response behavior
- existing claims
- structured error shape
- vacation request behavior
- trip behavior
- compensation/document/dashboard/audit/bootstrap behavior
