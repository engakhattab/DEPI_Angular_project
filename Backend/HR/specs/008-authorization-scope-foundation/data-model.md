# Phase 8 Data Model: Authorization Scope Foundation

Phase 8 does not add database tables, columns, relationships, indexes, or migrations. This document describes the existing data used by the shared scope foundation.

## Existing Entities Used

### Authenticated Employee Context

Represents the employee identity and account state used by service-level scope checks.

| Field | Source | Required Meaning |
|-------|--------|------------------|
| `EmployeeId` | `Employee.Id` | Stable employee identifier from the authenticated `employee_id` claim |
| `Role` | `Employee.Role` | Single current employee role |
| `IsActive` | `Employee.Status == Active` | Active-state flag for endpoint-specific decisions |
| `IsDeleted` | `Employee.IsDeleted` | Soft-delete flag |
| `IsTerminated` | `Employee.Status == Terminated` | Terminated-state flag |

Validation rules:

- Missing employee record means the session is invalid for scope decisions.
- Deleted and terminated current employees are not scope-eligible.
- Suspended current employees are scope-eligible in Phase 8, but `IsActive` must be false.

### Employee Role

Existing enum: `HR.Domain.Enums.EmployeeRole`.

Allowed values:

- `Employee`
- `Manager`
- `HRAdministrator`
- `SystemAdministrator`

Validation rules:

- No additional roles are introduced in Phase 8.
- No multi-role or permission-union model is introduced in Phase 8.
- Organization scope is available only to non-deleted, non-terminated `HRAdministrator` and `SystemAdministrator` employees.

### Manager Relationship

Existing relationship:

- `Employee.ManagerId`
- `Employee.Manager`
- `Employee.DirectReports`

Required behavior:

- Team scope uses direct plus indirect reports.
- Reports must be active and non-deleted for manager team data.
- Deleted, terminated, and suspended report targets are excluded from visible team data unless a later endpoint-specific phase documents an exception.
- Department mismatch alone does not grant or deny team scope.

### Scope Decision

Represents a reusable authorization answer.

Required decisions:

| Decision | Inputs | Output | Required Meaning |
|----------|--------|--------|------------------|
| Current employee context | `requesterEmployeeId` | `Result<EmployeeAccessContext>` | Resolve requester identity and account state |
| Is self | requester ID, target ID | `bool` | Same employee ID |
| Is manager of target | requester ID, target ID | `bool` | Target is active, non-deleted direct or indirect report |
| Can access employee | requester ID, target ID | `bool` | Self, manager team target, or organization scope |
| Can access team data | requester ID, target ID | `bool` | Manager team target/self or organization scope |
| Is HR administrator | requester ID | `bool` | Role is `HRAdministrator`, not deleted or terminated |
| Is system administrator | requester ID | `bool` | Role is `SystemAdministrator`, not deleted or terminated |
| Has organization scope | requester ID | `bool` | HR administrator or system administrator, not deleted or terminated |
| Visible employee IDs | requester ID | set of IDs | Employee self; manager self plus active direct/indirect reports; organization active employees |

### Visible Employee Set

Represents the employee IDs a requester can see in shared scope decisions.

Rules:

- Employee: self only.
- Manager: self plus active, non-deleted direct and indirect reports.
- HRAdministrator: active organization employees.
- SystemAdministrator: active organization employees.
- Deleted or terminated requester: empty set.
- Missing requester: empty set.

## Persistence Impact

No persistence impact.

Phase 8 uses existing data only:

- `Employees.Id`
- `Employees.Role`
- `Employees.Status`
- `Employees.IsDeleted`
- `Employees.ManagerId`

## Migration Strategy

No migration is planned.

If implementation proves a schema change is required, stop before creating it and document:

1. Which Phase 8 business rule cannot be represented by existing data.
2. Why code-only changes are insufficient.
3. The exact table/column/index changes proposed.
4. Existing data backfill impact.
5. Tests that prove the schema need.

