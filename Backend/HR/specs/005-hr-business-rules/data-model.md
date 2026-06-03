# Data Model: Phase 5 - HR Business Logic Improvements

## Employee

Represents the retained HR profile and lifecycle state for a person.

### Existing Fields

- `Id: Guid`
- `EmployeeNumber: string`
- `FullName: string`
- `Email: string?`
- `DepartmentId: Guid`
- `Department: Department?`
- `ManagerId: Guid?`
- `Manager: Employee?`
- `DirectReports: ICollection<Employee>`
- `BirthDate: DateOnly?`
- `JoinDate: DateOnly?`
- `JobTitle: string?`
- `PhoneNumber: string?`
- `Notes: string?`
- `Status: EmployeeStatus`
- `ApplicationUserId: string`

### New Fields

- `VacationBalanceDays: int` default `21`
- `IsDeleted: bool` default `false`
- `TerminatedAt: DateTimeOffset?`

### Relationships

- Many employees belong to one department.
- An employee may have one manager and many direct reports.
- An employee has one associated `ApplicationUser`.
- Employees may request many vacation requests and trips.
- Employees may review many vacation requests.

### Validation Rules

- `EmployeeNumber` is required, unique, and immutable after creation.
- `FullName`, `Email`, and `DepartmentId` remain required through DTO validation.
- Active employee email must be unique among active, non-deleted employees.
- `VacationBalanceDays` starts at `21` and cannot become negative through an approved request.
- Normal employee results exclude `IsDeleted = true`.
- Terminated employees remain visible unless `IsDeleted = true`.
- Soft-deleted employees retain their `ApplicationUserId`.

### State Transitions

| From | Allowed To |
|------|------------|
| `Active` | `Suspended`, `Terminated` |
| `Suspended` | `Active`, `Terminated` |
| `Terminated` | none |

Same-status updates are idempotent no-op successes. They do not change `TerminatedAt`, do not reject pending vacation requests again, do not repeat access-revocation work, and are not rejected merely because the requested status is already current.

### Lifecycle Side Effects

- Transition to `Terminated` sets `TerminatedAt` when not already set.
- Transition to `Terminated` rejects pending vacation requests.
- Transition to `Terminated` denies new sign-ins and rejects existing sessions.
- Delete action becomes soft deletion: set `IsDeleted = true`, set `Status = Terminated`, set `TerminatedAt`, reject pending vacation requests, and retain the Identity user.

## Vacation Request

Represents a leave request and its review lifecycle.

### Existing Fields

- `Id: Guid`
- `EmployeeId: Guid`
- `Employee: Employee?`
- `StartDate: DateOnly`
- `EndDate: DateOnly`
- `Reason: string`
- `Status: VacationRequestStatus`
- `CreatedAt: DateTimeOffset`
- `UpdatedAt: DateTimeOffset?`

### New Fields

- `WorkingDayCount: int`
- `ReviewedByEmployeeId: Guid?`
- `ReviewedBy: Employee?`
- `ReviewedAt: DateTimeOffset?`

### Relationships

- Each request belongs to one employee requester.
- Each reviewed request may reference one reviewer employee.

### Validation Rules

- Requester must exist, be active, and not be soft-deleted.
- `EndDate` must be on or after `StartDate`.
- `StartDate` must not be in the past.
- Notice must include at least three full working days between submission date and vacation start date.
- Working days are Sunday through Thursday; Friday and Saturday are weekend days.
- Pending and approved request ranges block overlapping new requests.
- Rejected request ranges do not block new requests.
- Requested `WorkingDayCount` must not exceed requester `VacationBalanceDays`.
- Reviewer must be an authenticated employee other than the requester.
- Hard deletion is allowed only for `Pending` requests.

### State Transitions

| From | Allowed To | Balance Effect |
|------|------------|----------------|
| `Pending` | `Approved` | Deduct `WorkingDayCount` once |
| `Pending` | `Rejected` | No balance change |
| `Approved` | `Rejected` | Restore `WorkingDayCount` once |
| `Rejected` | none | No balance change |

Same-status status updates are idempotent no-op successes. They do not change `ReviewedByEmployeeId`, `ReviewedAt`, `UpdatedAt`, `WorkingDayCount`, or employee balance, and are not rejected merely because the requested status is already current.

## Trip

Represents a transportation request traceable to an employee.

### Existing Fields

- `Id: Guid`
- `ReferenceName: string`
- `Project: string`
- `Route: string`
- `TripType: string`
- `TripDate: DateOnly`
- `TripCode: string`
- `RequestCode: string`
- `CreatedAt: DateTimeOffset`

### New Fields

- `RequestedByEmployeeId: Guid?`
- `RequestedBy: Employee?`

### Relationships

- Each new trip is requested by one employee.
- Existing pre-Phase-5 trip rows may have no requester when no reliable historical requester source exists.

### Validation Rules

- New trip submissions must provide `RequestedByEmployeeId`; the requesting employee must exist, be active, and not be soft-deleted.
- Existing pre-Phase-5 trip rows may have `RequestedByEmployeeId = null` after migration when no reliable requester source exists.
- `TripDate` must not be in the past.
- `TripDate` must be a working day: Sunday through Thursday.

## Department

Represents an organizational unit.

### Existing Fields

- `Id: Guid`
- `Name: string`
- `Employees: ICollection<Employee>`

### Derived Response Fields

- `EmployeeCount: int` on `DepartmentResponse`

### Validation Rules

- `Name` remains required and unique.
- `EmployeeCount` is derived from employees currently assigned to the department where `IsDeleted = false`.
- Terminated-but-not-soft-deleted employees remain counted.

## Login Identity

Represents the existing ASP.NET Core Identity user associated with an employee.

### Existing Fields

Owned by `ApplicationUser`/Identity tables.

### Validation Rules

- Credential validation still uses `UserManager<ApplicationUser>`.
- New sign-ins are denied when the linked employee is terminated or soft-deleted.
- Existing sessions are rejected when the `employee_id` claim no longer maps to an active, non-deleted employee.
- Soft deletion retains the Identity user and employee association.

## Migration Notes

- Add non-null columns with safe defaults for existing employees and vacation requests.
- Add nullable reviewer fields to vacation requests.
- Add the trip requester relationship without breaking existing trip rows. Do not invent fake requester data for existing trips. Prefer adding `RequestedByEmployeeId` as nullable for existing rows unless the current data model provides a reliable requester source.
- New trip creation after Phase 5 must require a requester through `TripCreateRequest`.
- Existing rows with null requester must be handled safely in repository queries, DTO mapping, compatibility checks, and manual regression.
- If a non-null database constraint is desired later, it must be introduced in a separate approved migration after a reliable backfill is possible.
- Add indexes to support active-email checks, soft-delete filtering, vacation overlap queries, trip requester lookup, and reviewer lookup where useful.
