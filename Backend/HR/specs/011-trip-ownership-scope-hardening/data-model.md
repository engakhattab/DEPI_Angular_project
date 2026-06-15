# Data Model: Phase 11 - Trip Ownership and Scope Hardening

Phase 11 scope hardening can use existing data for traveler ownership, employee role, employee status, and manager hierarchy. Persisted requester tracking is required by the spec but needs a separately approved migration because the current trip schema has only one employee reference.

## Existing Entity: Trip

Represents a transportation trip request.

Existing relevant fields:

- `Id`: unique trip identifier.
- `RequestedByEmployeeId`: existing employee reference. Phase 11 treats this as compatibility traveler data.
- `RequestedBy`: existing employee navigation. Phase 11 treats this as compatibility traveler navigation.
- `ReferenceName`: required trip reference text.
- `Project`: required project text.
- `Route`: required route text.
- `TripType`: required trip type text.
- `TripDate`: trip date.
- `TripCode`: generated trip code.
- `RequestCode`: generated request code.
- `CreatedAt`: creation timestamp.

Compatibility rules:

- Existing rows keep their current `RequestedByEmployeeId` values.
- Existing response fields `RequestedByEmployeeId` and `RequestedByEmployeeName` remain stable.
- Phase 11 must not reinterpret `RequestedByEmployeeId` as the authenticated requester.
- Optional additive traveler aliases may be added only if they do not remove or change existing fields.

## Required Approval-Gated Field: RequesterEmployeeId

Required persisted requester metadata after migration approval:

- `RequesterEmployeeId`: nullable employee ID of the authenticated trip requester.
- `Requester`: optional navigation to `Employee`.

Rules:

- Existing rows may keep `RequesterEmployeeId = null`.
- Do not backfill existing rows with traveler-as-requester unless a reliable source is approved.
- New rows after approved migration and implementation must set `RequesterEmployeeId` to the authenticated requester.
- Queries and responses must handle null requester metadata safely.
- A future non-null constraint requires a separate approved migration after reliable backfill is possible.

Proposed database shape if approved:

- Table: `Trips`
- Column: `RequesterEmployeeId uniqueidentifier null`
- FK: `RequesterEmployeeId -> Employees.Id`
- Delete behavior: restrict/no action
- Index: `IX_Trips_RequesterEmployeeId`
- Proposed migration name: `AddTripRequesterEmployee`

## Existing Entity: Employee

Represents the authorization subject, traveler, requester, or manager.

Relevant fields:

- `Id`: unique employee identifier.
- `FullName`: display name for traveler/requester response fields.
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

- missing requester: unauthorized or no trip scope
- soft-deleted requester: no trip scope
- terminated requester: no trip scope
- suspended requester: preserve Phase 8 behavior unless existing trip-specific validation forbids the action

## Derived Model: Trip Traveler Scope

The traveler employee ID used to decide whether a requester may see or mutate a trip.

Rules:

- Employee: own traveler ID only.
- Manager: requester ID plus active direct and indirect report IDs.
- HR/System: any traveler ID after organization-scope authorization.
- List filtering applies inside the allowed traveler ID set before pagination.

## Derived Model: Manager Team Scope

Calculated from existing `Employee.ManagerId` relationships through Phase 8 access services.

Rules:

- includes active direct reports
- includes active indirect reports
- excludes peers and unrelated employees
- excludes soft-deleted employees
- excludes terminated employees
- manager's own employee ID is included for trip operations

## Derived Model: Organization Scope

Granted to requesters with:

- `Role == HRAdministrator` or `Role == SystemAdministrator`
- `Status != Terminated`
- `IsDeleted == false`

Rules:

- list/detail access includes organization-wide trips
- create may target any employee who passes existing trip eligibility rules
- delete may target any existing trip after authorization succeeds

## Validation Rules

- Trip list scope filtering happens before pagination and total-count calculation.
- Optional trip list `travelerEmployeeId` filter is intersected with allowed traveler scope.
- Out-of-scope list filters return an empty page with normal pagination metadata.
- Trip detail distinguishes missing trip (`404`) from existing out-of-scope trip (`403`).
- Trip create authorization happens before target mutation and before trusting request-body ownership.
- Trip delete authorization happens before removal for existing out-of-scope trips.
- Existing active employee, non-deleted employee, future date, working-day, required text, generated trip code, and generated request code rules remain active.

## Migration Impact

No migration is created during planning.

Scope hardening is possible against existing traveler data. Persisted requester tracking requires an approved migration before implementation can store `RequesterEmployeeId`.
