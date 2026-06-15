# Data Model: Phase 10 - Vacation Request Scope Hardening

Phase 10 scope hardening can use existing data for owner, reviewer, employee role, employee status, and manager hierarchy. Persisted creator tracking is required by the spec but needs a separately approved migration because the current schema has no creator field.

## Existing Entity: VacationRequest

Represents a vacation request owned by an employee.

Existing relevant fields:

- `Id`: unique vacation request identifier.
- `EmployeeId`: owner employee whose vacation balance and absence period are affected.
- `Employee`: owner navigation used for response display and scope checks.
- `StartDate`: requested vacation start date.
- `EndDate`: requested vacation end date.
- `Reason`: requester-provided reason.
- `Status`: vacation lifecycle state: `Pending`, `Approved`, or `Rejected`.
- `WorkingDayCount`: working-day count deducted/refunded on approval/rejection.
- `ReviewedByEmployeeId`: reviewer employee ID when reviewed.
- `ReviewedBy`: reviewer navigation used for response display.
- `ReviewedAt`: review timestamp.
- `CreatedAt`: creation timestamp.
- `UpdatedAt`: update timestamp when status changes.

## Required Approval-Gated Field: CreatedByEmployeeId

Required persisted creator metadata after migration approval:

- `CreatedByEmployeeId`: nullable employee ID of the authenticated creator.
- `CreatedBy`: optional navigation to `Employee`.

Rules:

- Existing rows may keep `CreatedByEmployeeId = null`.
- Do not backfill existing rows with owner-as-creator unless a reliable source is approved.
- New rows after approved migration and implementation must set `CreatedByEmployeeId` to the authenticated requester.
- Queries and responses must handle null creator metadata safely.
- A future non-null constraint requires a separate approved migration after reliable backfill is possible.

Proposed database shape if approved:

- Table: `VacationRequests`
- Column: `CreatedByEmployeeId uniqueidentifier null`
- FK: `CreatedByEmployeeId -> Employees.Id`
- Delete behavior: restrict/no action
- Index: `IX_VacationRequests_CreatedByEmployeeId`
- Proposed migration name: `AddVacationRequestCreatedByEmployee`

## Existing Entity: Employee

Represents the authorization subject, vacation owner, creator, or reviewer.

Relevant fields:

- `Id`: unique employee identifier.
- `FullName`: display name for owner/reviewer response fields.
- `ManagerId`: manager relationship used for direct and indirect team scope.
- `Role`: single role: `Employee`, `Manager`, `HRAdministrator`, or `SystemAdministrator`.
- `Status`: lifecycle state: `Active`, `Suspended`, or `Terminated`.
- `IsDeleted`: soft-delete marker.

## Derived Model: Requesting Employee

The signed-in employee resolved from the `employee_id` claim.

Required rule inputs:

- employee ID
- role
- status
- soft-delete state
- termination state

Rules:

- missing requester: unauthorized
- soft-deleted requester: no vacation scope
- terminated requester: no vacation scope
- suspended requester: preserve Phase 8 behavior unless existing vacation-specific validation forbids the target/action

## Derived Model: Vacation Request Owner Scope

The owner employee ID used to decide whether a requester may see or mutate a vacation request.

Rules:

- Employee: own owner ID only.
- Manager unfiltered list: active direct and indirect report owner IDs only.
- Manager explicit self filter: manager owner ID only.
- Manager detail/review: self for detail, active team for review, never self-review.
- HR/System: any owner ID after organization-scope authorization.

## Derived Model: Manager Team Scope

Calculated from existing `Employee.ManagerId` relationships through Phase 8 access services.

Rules:

- includes active direct reports
- includes active indirect reports
- excludes peers and unrelated employees
- excludes soft-deleted employees
- excludes terminated employees
- unfiltered manager vacation list excludes manager self
- explicit manager self list filter includes manager self only

## Derived Model: Organization Scope

Granted to requesters with:

- `Role == HRAdministrator` or `Role == SystemAdministrator`
- `Status != Terminated`
- `IsDeleted == false`

Rules:

- list/detail access includes organization-wide vacation requests
- create may target any employee who passes existing vacation eligibility rules
- review may target any non-self request
- delete may target any pending request

## Vacation Request State Transitions

Phase 10 preserves existing status transition rules:

- `Pending -> Approved`
- `Pending -> Rejected`
- `Approved -> Rejected`
- no transitions out of `Rejected`
- same-status update is an idempotent no-op success with no duplicate side effects and no timestamp changes

Phase 10 adds authorization preconditions before these transitions:

- Employee cannot review.
- Manager can review only active team-owned requests.
- HR/System can review any non-self request.
- Self-review is always forbidden.
- Existing out-of-scope requests return `403` before transition validation.

## Validation Rules

- Vacation list scope filtering happens before pagination and total-count calculation.
- Vacation list `employeeId` filter is intersected with allowed owner scope.
- Vacation detail distinguishes missing request (`404`) from existing out-of-scope request (`403`).
- Vacation create authorization happens before target lookup for Employee/Manager non-self attempts.
- Vacation review/delete authorization happens before vacation domain-rule validation for existing out-of-scope targets.
- Pending-only delete rule remains unchanged after authorization passes.
- Existing date order, future date, notice period, overlap, balance, target employee eligibility, transition, reviewer metadata, balance deduction, and balance refund rules remain active.

## Migration Impact

No migration is created during planning.

Scope hardening is possible with existing fields. Persisted creator tracking requires an approved migration before implementation can store `CreatedByEmployeeId`.
