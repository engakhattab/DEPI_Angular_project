# Data Model: Phase 9 - Employee Access Scope Hardening

Phase 9 does not add or change database schema. This document describes the existing data used to enforce access decisions.

## Existing Entity: Employee

Represents an employee profile and authorization subject.

Relevant fields:

- `Id`: unique employee identifier.
- `EmployeeNumber`: immutable business identifier.
- `FullName`: profile display name.
- `Email`: employee email and Identity username source.
- `DepartmentId`: owning department.
- `ManagerId`: manager relationship used for direct and indirect team scope.
- `Status`: employee lifecycle state: `Active`, `Suspended`, or `Terminated`.
- `Role`: single employee role: `Employee`, `Manager`, `HRAdministrator`, or `SystemAdministrator`.
- `IsDeleted`: soft-delete marker.
- `TerminatedAt`: termination/deletion timestamp when applicable.
- `ApplicationUserId`: linked ASP.NET Identity user.

## Derived Model: Requesting Employee

The signed-in employee resolved from the `employee_id` claim.

Required rule inputs:

- employee ID
- role
- status
- soft-delete state

Authorization rules:

- missing requester: invalid session or unauthorized
- soft-deleted requester: no employee scope
- terminated requester: no employee scope
- suspended requester: preserve Phase 8 behavior unless existing employee-management rules forbid the action

## Derived Model: Target Employee

The employee record being listed, viewed, created, updated, deleted, or assigned a role.

Rules:

- missing target detail/update/delete ID keeps existing not-found behavior
- existing but out-of-scope target returns `403 Forbidden`
- managers can view active direct and indirect report targets only
- HR/System can view active, suspended, terminated, and soft-deleted targets
- HR administrators cannot update or delete `SystemAdministrator` targets

## Derived Model: Manager Team Scope

Calculated from existing `Employee.ManagerId` relationships.

Rules:

- includes active direct reports
- includes active indirect reports
- excludes manager self for `GET /api/employees`
- allows manager self for `GET /api/employees/{id}`
- excludes peers, unrelated employees, manager's own manager, soft-deleted reports, and terminated reports
- department mismatch alone does not grant or deny scope

## Derived Model: Organization Scope

Granted to requesters with:

- `Role == HRAdministrator` or `Role == SystemAdministrator`
- `Status != Terminated`
- `IsDeleted == false`

Rules:

- employee list/detail access includes active, suspended, terminated, and soft-deleted target records
- employee create is allowed
- employee update/delete is allowed according to protected-target rules
- role assignment remains system-administrator-only

## Derived Model: Active System Administrator Count

Calculated from existing `Employee` rows:

- `Role == SystemAdministrator`
- `Status == Active`
- `IsDeleted == false`

Rules:

- update, delete/soft-delete, termination, status-change, and role assignment/demotion operations must be rejected before mutation when they would leave zero active system administrators
- no new persisted counter is introduced

## State Transitions

Phase 9 preserves existing employee status transition rules:

- `Active -> Suspended`
- `Active -> Terminated`
- `Suspended -> Active`
- `Suspended -> Terminated`
- no transitions out of `Terminated`
- same-status updates are idempotent no-op with respect to status side effects

Additional Phase 9 guard:

- any permitted transition or role demotion that would leave zero active system administrators is rejected before mutation

## Validation Rules

- Employee list scope filtering happens before pagination.
- Employee detail distinguishes missing target (`404`) from existing out-of-scope target (`403`).
- Employee create/update/delete authorization happens before state mutation.
- HR administrator update/delete is rejected for `SystemAdministrator` targets.
- Last-active-system-administrator removal or role demotion is rejected before state mutation.
- Existing duplicate email, duplicate employee number, employee-number immutability, manager-cycle, soft-delete, and structured error rules remain active.

## Migration Impact

No migration is planned. Existing columns represent all Phase 9 decisions.
