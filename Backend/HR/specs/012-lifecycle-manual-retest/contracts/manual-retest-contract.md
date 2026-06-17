# Contract: Phase 12 Manual Lifecycle Retest

This contract defines the documentation and evidence shape required for Phase 12. It does not define new API endpoints.

## Database Contract

- Retest database name: `HrSystemDb_Phase12LifecycleTest`
- Connection string source: environment variable, user secret, or local uncommitted configuration
- Required migration state: existing approved migrations applied through Phase 11
- Forbidden database work: creating new migrations, editing existing migrations, changing committed schema design

Expected approved migration list:

```text
20251114215718_InitialCreate
20260603014628_Phase5HrBusinessRules
20260606235241_Phase7AdvancedHrFeatures
20260615170903_AddVacationRequestCreatedByEmployee
20260615212225_AddTripRequesterEmployee
```

## Actor Contract

| Employee Number | Email | Role | Purpose |
|-----------------|-------|------|---------|
| `EMP001` | `admin@test.com` | `SystemAdministrator` | Full access and role assignment validation |
| `EMP002` | `hr.admin@test.com` | `HRAdministrator` | HR-wide access without role assignment |
| `EMP003` | `manager@test.com` | `Manager` | Team-scoped validation |
| `EMP004` | `employee@test.com` | `Employee` | Self-service and forbidden-access validation |

Passwords may be documented only as local-only placeholders. They must not be customer or production secrets.

## Manual Retest Scenario Contract

Each scenario in the Phase 12 manual retest checklist must include:

| Field | Required | Notes |
|-------|----------|-------|
| `id` | Yes | Stable scenario ID |
| `module` | Yes | Auth, Employees, Vacations, Trips, Attendance, Compensation, Documents, Dashboard, or Audit |
| `actor` | Yes | One of the four test actors |
| `request/action` | Yes | HTTP method + endpoint or manual setup action |
| `setup` | Yes | IDs/data needed before execution |
| `expected` | Yes | Expected status/result and response shape |
| `actual` | Yes after execution | Observed status/result |
| `result` | Yes after execution | Pass, fail, or blocked |
| `notes` | Required on fail/block | Include follow-up defect if runtime mismatch is suspected |

## Role-Scope Coverage Contract

### Employees

- Employee cannot list all employees.
- Employee cannot view another employee detail.
- Manager sees team employees only and cannot see outside-team employee details.
- HR Administrator and System Administrator can list organization employees.
- Employee creation is HR/System only.
- Role assignment is SystemAdministrator only.
- HRAdministrator cannot assign roles.
- Last active SystemAdministrator protection remains expected.

### Vacations

- Employee sees and creates own vacation requests only.
- Manager sees and reviews team vacation requests only.
- HR/System see organization vacation requests and can create for employees.
- Employee and Manager actors can create vacation requests for self only.
- HR Administrator and System Administrator actors can create vacation requests for employees.
- Self-review is blocked for every role.
- Out-of-scope filters do not leak unauthorized vacation data.

### Trips

- Employee sees, creates, and deletes own trips only.
- Manager sees, creates, and deletes own/team trips only.
- HR/System see, create, and delete organization trips.
- Authenticated requester identity is not trusted from the request body.
- Traveler/requester behavior is documented.
- Out-of-scope filters do not leak unauthorized trip data.

### Sensitive Modules

- Employee and Manager cannot access audit logs.
- Employee and Manager actors cannot access compensation records.
- Dashboard metric scope must match the Phase 7 metric scope table.
- Document download/removal must require authorized access.

## Error Compatibility Contract

Manual retest and documentation must preserve existing compatibility expectations:

- `401` for unauthenticated or invalid session behavior.
- `403` for authenticated forbidden access.
- `404` for not-found behavior where current contract requires it.
- `409` for conflict/business guard failures where current convention uses conflict.
- `422` for validation/domain/business-rule failures where current convention uses unprocessable entity.
- Error payload shape must remain compatible with `{ code, message }`.
- Phase 12 must not normalize existing error codes.

## Evidence Contract

The Phase 12 implementation summary must include:

- Database name and connection source used.
- Applied migration list or migration verification command result.
- API base URL and manual test tool.
- Commands run for restore/build/test/EF checks.
- Manual retest checklist result summary.
- Failed/blocked scenarios and follow-up defect notes.
- Confirmation no source code was modified for implementation fixes.
- Confirmation no new migrations were created.
- Deferred Phase 13 Swagger/OpenAPI documentation items, if any.
