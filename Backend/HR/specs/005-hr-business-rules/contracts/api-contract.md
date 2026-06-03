# API Contract: Phase 5 - HR Business Logic Improvements

Phase 5 preserves existing routes and HTTP verbs. It changes business outcomes and adds response/request fields required by the specification.

## Shared Error Contract

Expected failures continue to return:

```json
{
  "code": "BUSINESS_RULE_VIOLATION",
  "message": "Human-readable failure reason."
}
```

Existing compatibility codes remain valid. New Phase 5 rule failures should prefer existing `ServiceError` mappings:

- `404` with `NOT_FOUND` for missing referenced records
- `409` with `CONFLICT` for duplicate active employee email
- `422` with `BUSINESS_RULE_VIOLATION` for domain-rule failures
- `401` with `UNAUTHORIZED` for invalid or revoked sessions

## Authentication

### `POST /api/auth/login`

Request shape remains unchanged.

New behavior:

- Deny login when the linked employee is `Terminated`.
- Deny login when the linked employee has `IsDeleted = true`.
- Keep existing cookie creation, claim names, login response wrapper, and all existing login response fields. Additive employee fields are allowed only if they do not remove, rename, or change current fields; existing clients must not lose any current login response data.

### Existing authenticated requests

Cookie validation rejects the principal when the `employee_id` claim is missing, invalid, or maps to an employee that is not active or is soft-deleted.

## Vacation Requests

### `POST /api/vacationrequests`

Request shape remains:

```json
{
  "employeeId": "00000000-0000-0000-0000-000000000000",
  "startDate": "2026-06-14",
  "endDate": "2026-06-18",
  "reason": "Annual leave"
}
```

New behavior:

- Reject non-active or soft-deleted employees.
- Reject past starts.
- Reject end date before start date.
- Reject requests without at least three full working days of notice.
- Reject overlap with pending or approved requests.
- Reject requests that exceed available vacation balance.
- Store the request as `Pending` with calculated `workingDayCount`.

### `PUT /api/vacationrequests/{id}/status`

Request body remains:

```json
{
  "status": "Approved"
}
```

Reviewer identity is taken from the authenticated `employee_id` claim, not from the request body.

New behavior:

- Reject requester self-review.
- Allow any authenticated employee except the requester.
- Enforce vacation-request state transitions.
- Treat same-status requests as idempotent no-op successes: do not update reviewer fields, timestamps, or balances again, and do not reject merely because the requested status is already current.
- Record `reviewedByEmployeeId`, reviewer display name in responses, and `reviewedAt`.
- Deduct or restore balance exactly once according to the state transition.

### `DELETE /api/vacationrequests/{id}`

New behavior:

- Pending requests may be hard-deleted.
- Approved and rejected requests are retained and deletion is rejected.

### Vacation response additive fields

`VacationRequestResponse` adds:

```json
{
  "workingDayCount": 4,
  "reviewedByEmployeeId": "00000000-0000-0000-0000-000000000000",
  "reviewedByEmployeeName": "Reviewer Name",
  "reviewedAt": "2026-06-03T12:00:00+00:00"
}
```

## Employees

### `POST /api/employees`

Existing request fields remain. New behavior:

- Reject duplicate active employee email.
- Reject circular manager assignments.
- Allow cross-department manager assignment while logging a warning.
- New employees start with `vacationBalanceDays = 21` unless a later approved policy changes the default.

### `PUT /api/employees/{id}`

Existing request fields remain. New behavior:

- Reject invalid employee status transitions.
- Treat same-status requests as idempotent no-op successes: do not update termination timestamps, reject pending vacation requests again, or repeat access-revocation work.
- Reject circular manager assignments.
- Reject duplicate active employee email.
- Preserve immutable employee number.
- When the status becomes `Terminated`, set `terminatedAt`, reject pending vacation requests, and revoke access.

### `DELETE /api/employees/{id}`

New behavior:

- Soft-delete the employee instead of hard-deleting.
- Retain the associated Identity user.
- Set `isDeleted = true`, `status = Terminated`, and `terminatedAt`.
- Reject pending vacation requests.
- Exclude the employee from normal employee list/detail results after deletion.

### Employee response additive fields

`EmployeeResponse` adds:

```json
{
  "vacationBalanceDays": 21,
  "isDeleted": false,
  "terminatedAt": null
}
```

## Trips

### `POST /api/trips`

Request body adds `requestedByEmployeeId`:

```json
{
  "referenceName": "Client visit",
  "project": "Project A",
  "route": "HQ to Site",
  "tripType": "Business",
  "tripDate": "2026-06-14",
  "requestedByEmployeeId": "00000000-0000-0000-0000-000000000000"
}
```

New behavior:

- `requestedByEmployeeId` is required for new Phase 5 trip submissions.
- Reject missing, suspended, terminated, or soft-deleted requesting employees.
- Reject past dates.
- Reject Friday/Saturday dates.

### Trip response additive fields

`TripResponse` adds:

```json
{
  "requestedByEmployeeId": "00000000-0000-0000-0000-000000000000",
  "requestedByEmployeeName": "Requester Name"
}
```

Existing pre-Phase-5 trip rows may return `requestedByEmployeeId: null` and `requestedByEmployeeName: null` when no reliable historical requester source exists. Queries and compatibility checks must handle those nulls safely.

## Departments

### `GET /api/departments`

### `GET /api/departments/{id}`

Response adds `employeeCount`:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "Engineering",
  "employeeCount": 12
}
```

`employeeCount` includes employees assigned to the department where `isDeleted = false`. Terminated-but-not-soft-deleted employees remain counted.
