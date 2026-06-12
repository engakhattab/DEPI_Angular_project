# API Contracts: Phase 7 - Advanced HR Features

## Global Contract Rules

- Authentication remains cookie-based.
- Existing routes and response fields remain stable unless Phase 7 explicitly adds fields.
- Existing login/current-user fields remain stable. Role fields are additive.
- Expected errors use `{ "code": "...", "message": "..." }`.
- `401` means unauthenticated or invalid session.
- `403` means authenticated but not allowed by role/scope.
- `404` means requested resource is not found or not visible to the requester.
- `409` means conflict with existing state.
- `413` means an uploaded document exceeds the configured maximum file size and still returns the structured `{ "code": "...", "message": "..." }` payload.
- `422` means domain/business-rule failure.
- List endpoints use `page` and `pageSize` with the existing pagination envelope.

## Startup Bootstrap Contract

Initial System administrator bootstrap is a startup/bootstrap activation path, not a public HTTP endpoint.

- Configuration section: `InitialAdminBootstrap`
- Primary mode: `CreateInitialAdmin`
- Execution timing: before administrative RBAC enforcement can lock out all administrators
- Existing-admin behavior: if any active `SystemAdministrator` exists, do nothing, create no duplicate admin, overwrite no roles, and reset no passwords
- Creation behavior: if no active `SystemAdministrator` exists, create one `ApplicationUser`, create one linked `Employee`, assign `Employee.Role = SystemAdministrator`, and write audit
- Required creation fields: `Enabled`, `Mode`, `EmployeeNumber`, `Email`, `FullName`, `DepartmentId`, `TemporaryPassword`, `ForcePasswordChange`
- Sensitive configuration: `TemporaryPassword` should come from environment variables or user secrets in real deployments
- Duplicate safety: repeated startup does not duplicate users, employees, roles, passwords, or audit side effects
- Failure behavior: missing required fields, invalid mode, duplicate employee number, duplicate email, invalid password, or missing department fail clearly and assign no fallback administrator
- Transaction behavior: if user creation succeeds but employee creation, role assignment, or audit writing fails, the whole bootstrap operation rolls back
- Audit behavior: successful bootstrap writes an audit entry using system actor marker `SYSTEM_BOOTSTRAP`, the created employee id, assigned role `SystemAdministrator`, employee number, email, and UTC timestamp
- Secret handling: temporary password, password hash, tokens, security stamps, and cookies are never included in audit payloads

## Additive Auth Contract

### GET `/api/auth/me`

Adds role context without removing existing fields.

**Response 200**

```json
{
  "employeeId": "00000000-0000-0000-0000-000000000000",
  "fullName": "Example Employee",
  "email": "employee@example.com",
  "role": "HRAdministrator"
}
```

### POST `/api/auth/login`

Existing wrapper and employee fields remain stable. Employee role may be additive.

## Attendance

### POST `/api/attendance/clock-in`

Records clock-in for the authenticated employee using the configured business timezone.

**Request**

```json
{
  "notes": "Optional note"
}
```

**Response 201**

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "employeeId": "00000000-0000-0000-0000-000000000000",
  "attendanceDate": "2026-06-07",
  "clockInAtUtc": "2026-06-07T06:55:00+00:00",
  "clockOutAtUtc": null,
  "workedHours": null,
  "notes": "Optional note"
}
```

**Errors**

- `401` unauthenticated
- `403` employee is suspended, terminated, soft-deleted, or otherwise not eligible
- `409` duplicate clock-in for same attendance date
- `422` invalid attendance date/time rule

### POST `/api/attendance/clock-out`

Completes the authenticated employee's open attendance record for the business date.

**Request**

```json
{
  "notes": "Optional note"
}
```

**Response 200**: `AttendanceRecordResponse`

**Errors**

- `401` unauthenticated
- `403` employee is not eligible
- `404` no open same-date attendance record
- `422` clock-out would be before or equal to clock-in

### GET `/api/attendance`

Lists attendance records for authorized scope.

**Query**

- `employeeId`: optional Guid
- `from`: optional DateOnly
- `to`: optional DateOnly
- `page`: optional int, default 1
- `pageSize`: optional int, default 25, max 100

**Authorization**

- Employee: own records only
- Manager: team records only
- HR administrator/System administrator: all records

## Employee Role Assignment

### PUT `/api/employees/{id}/role`

Assigns exactly one current role to an employee.

**Authorization**: System administrator only

**Request**

```json
{
  "role": "Manager"
}
```

**Response 200**

```json
{
  "employeeId": "00000000-0000-0000-0000-000000000000",
  "role": "Manager",
  "updatedAt": "2026-06-07T09:00:00+00:00"
}
```

**Errors**

- `403` not System administrator
- `404` employee not found
- `422` employee is terminated or soft-deleted

## Compensation

Compensation endpoints are served by `CompensationController` only.

### GET `/api/employees/{id}/compensation`

Returns current compensation and salary history for authorized users.

**Authorization**: HR administrator or System administrator

**Response 200**

```json
{
  "employeeId": "00000000-0000-0000-0000-000000000000",
  "baseSalary": 25000.00,
  "salaryCurrency": "EGP",
  "lastSalaryReviewDate": "2026-06-01",
  "history": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "changedAt": "2026-06-07T09:15:00+00:00",
      "changedByEmployeeId": "00000000-0000-0000-0000-000000000000",
      "previousBaseSalary": 22000.00,
      "newBaseSalary": 25000.00,
      "previousCurrency": "EGP",
      "newCurrency": "EGP",
      "previousReviewDate": "2025-12-01",
      "newReviewDate": "2026-06-01"
    }
  ]
}
```

### PUT `/api/employees/{id}/compensation`

Creates or updates current compensation.

**Authorization**: HR administrator or System administrator

**Request**

```json
{
  "baseSalary": 25000.00,
  "salaryCurrency": "EGP",
  "lastSalaryReviewDate": "2026-06-01"
}
```

**Response 200**: `CompensationResponse`

**Errors**

- `403` not authorized
- `404` employee not found
- `422` invalid salary or currency

## Employee Documents

### POST `/api/employees/{id}/documents`

Uploads a document using multipart form data.

**Authorization**: HR administrator or System administrator

**Form fields**

- `category`: `Identity`, `Contract`, `Certificate`, or `Other`
- `file`: uploaded file

**Response 201**

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "employeeId": "00000000-0000-0000-0000-000000000000",
  "category": "Contract",
  "originalFileName": "employment-contract.pdf",
  "fileSizeBytes": 102400,
  "contentType": "application/pdf",
  "uploadedByEmployeeId": "00000000-0000-0000-0000-000000000000",
  "uploadedAt": "2026-06-07T09:30:00+00:00"
}
```

**Errors**

- `403` not authorized
- `404` employee not found
- `413 Payload Too Large` when the uploaded file exceeds configured maximum; response body uses the standard `{ "code": "...", "message": "..." }` payload
- `422` invalid file type, missing file, or invalid category

### GET `/api/employees/{id}/documents`

Lists current documents for an employee.

**Authorization**: HR administrator or System administrator

**Response 200**: paged or bounded list of `EmployeeDocumentResponse`

### GET `/api/employees/{id}/documents/{documentId}`

Downloads document content through an authorized endpoint.

**Authorization**: HR administrator or System administrator

**Response 200**: file content with original file name as download metadata

**Errors**

- `403` not authorized
- `404` document not found, not visible, or already removed

### DELETE `/api/employees/{id}/documents/{documentId}`

Marks document metadata removed or soft-deleted, deletes the physical file from backend-managed local storage, audits the removal, and prevents future downloads.

**Authorization**: HR administrator or System administrator

**Response 204**

**Post-removal behavior**

- The document is not returned by list endpoints.
- Download attempts for the removed document are no longer available and must not return the deleted file.
- The removal audit entry identifies the actor, affected document, affected employee, action, and timestamp.

## Dashboard

### GET `/api/dashboard/summary`

Returns current HR metrics scoped to the signed-in user's role.

**Authorization**

- Manager: team-scoped dashboard
- HR administrator/System administrator: organization-wide dashboard

**Metric scope**

| Metric | Manager | HR Administrator | System Administrator |
|--------|---------|------------------|----------------------|
| `totalActiveEmployees` | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| `totalDepartments` | Hidden / not applicable | Organization-wide | Organization-wide |
| `pendingVacationRequests` | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| `approvedVacationsThisMonth` | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| `employeesOnVacationToday` | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| `newHiresThisMonth` | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| `upcomingTripsThisWeek` | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| `employeesPerDepartment` | Team-scoped direct and indirect reports grouped by department | Organization-wide | Organization-wide |
| `vacationRequestsByStatus` | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |

Hidden or not applicable manager metrics must not expose organization-wide values.

**Response 200**

```json
{
  "totalActiveEmployees": 42,
  "totalDepartments": 5,
  "pendingVacationRequests": 3,
  "approvedVacationsThisMonth": 7,
  "employeesOnVacationToday": 2,
  "newHiresThisMonth": 4,
  "upcomingTripsThisWeek": 6,
  "employeesPerDepartment": {
    "Engineering": 18,
    "HR": 4
  },
  "vacationRequestsByStatus": {
    "Pending": 3,
    "Approved": 7,
    "Rejected": 1
  }
}
```

## Audit Logs

### GET `/api/audit-logs`

Searches audit history.

**Authorization**: HR administrator or System administrator

**Query**

- `entityType`: optional string
- `entityId`: optional Guid
- `actorEmployeeId`: optional Guid
- `action`: optional string
- `from`: optional DateTimeOffset
- `to`: optional DateTimeOffset
- `page`: optional int, default 1
- `pageSize`: optional int, default 25, max 100

**Response 200**

```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "entityType": "Employee",
      "entityId": "00000000-0000-0000-0000-000000000000",
      "actionType": "RoleChanged",
      "actorEmployeeId": "00000000-0000-0000-0000-000000000000",
      "actorMarker": null,
      "performedAt": "2026-06-07T10:00:00+00:00",
      "changedFields": ["Role"],
      "oldValues": { "role": "Employee" },
      "newValues": { "role": "Manager" },
      "sensitiveSummary": null
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 25,
  "totalPages": 1,
  "hasNext": false,
  "hasPrevious": false
}
```

**Sensitive audit behavior**

- Compensation amounts, document content, and protected document storage details are redacted or summarized in general audit logs.
- Detailed sensitive values remain available only through their dedicated authorized feature records.
- Initial bootstrap admin creation uses `actorEmployeeId: null` and `actorMarker: "SYSTEM_BOOTSTRAP"` and identifies the affected employee, assigned role, employee number, email, and timestamp without including temporary password, password hash, tokens, security stamps, or cookies.
