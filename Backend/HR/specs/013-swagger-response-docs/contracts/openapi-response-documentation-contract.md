# OpenAPI Response Documentation Contract

This contract defines the response-documentation outcomes Phase 13 must produce. It does not define new runtime API behavior.

## Global Contract Rules

- Existing routes, methods, request bodies, query parameters, response bodies, status codes, cookies, claims, and authorization policies must remain unchanged.
- Route verification may compare OpenAPI paths case-insensitively when controller-token casing differs; Phase 13 must not change route attributes or route casing.
- Protected endpoints must document cookie-session unauthenticated behavior as `401 Unauthorized`; Swagger must not imply bearer-token authentication.
- Role/policy-restricted endpoints must document `403 Forbidden` where current authorization behavior can return it.
- Service error responses must document the structured JSON error shape:

```json
{
  "code": "ERROR_CODE",
  "message": "Human-readable message"
}
```

- Validation responses must match current behavior. If controller model validation currently returns validation-problem details instead of the structured error shape, the documentation must not claim otherwise.
- File download responses must be documented as file/binary responses.
- Empty success responses must be documented as no-body responses.

## Required Endpoint Groups

### Auth

| Operation | Expected Success Documentation | Expected Error Documentation |
|-----------|--------------------------------|------------------------------|
| `POST /api/auth/login` | `200 OK` with `LoginResponse` | `400`/validation behavior where current model validation applies; `401 Unauthorized` for invalid credentials |
| `POST /api/auth/logout` | `204 No Content` | `401 Unauthorized` for missing/invalid session |
| `GET /api/auth/me` | `200 OK` with `CurrentUserResponse` | `401 Unauthorized` for missing/invalid session |

### Employees

| Operation | Expected Success Documentation | Expected Error Documentation |
|-----------|--------------------------------|------------------------------|
| `GET /api/employees` | `200 OK` with `PagedList<EmployeeResponse>` | `401`, `403`, and applicable service errors |
| `GET /api/employees/{id}` | `200 OK` with `EmployeeResponse` | `401`, `403`, `404` |
| `POST /api/employees` | `201 Created` with `EmployeeCreatedResponse` | `400`, `401`, `403`, `409`, `422` where current behavior supports them |
| `PUT /api/employees/{id}` | `200 OK` with `EmployeeResponse` | `400`, `401`, `403`, `404`, `409`, `422` where current behavior supports them |
| `DELETE /api/employees/{id}` | `204 No Content` | `401`, `403`, `404`, `409`, `422` where current behavior supports them |
| `PUT /api/employees/{id}/role` | `200 OK` with `EmployeeRoleResponse` | `400`, `401`, `403`, `404`, `409`, `422` where current behavior supports them |

### Departments

| Operation | Expected Success Documentation | Expected Error Documentation |
|-----------|--------------------------------|------------------------------|
| `GET /api/departments` | `200 OK` with `PagedList<DepartmentResponse>` | `401` and applicable authorization errors |
| `GET /api/departments/{id}` | `200 OK` with `DepartmentResponse` | `401`, `403`, `404` |
| `POST /api/departments` | `201 Created` with `DepartmentResponse` | `400`, `401`, `403`, `409`, `422` where current behavior supports them |
| `PUT /api/departments/{id}` | `200 OK` with `DepartmentResponse` | `400`, `401`, `403`, `404`, `409`, `422` where current behavior supports them |
| `DELETE /api/departments/{id}` | `204 No Content` | `401`, `403`, `404`, `409`, `422` where current behavior supports them |

### Attendance

| Operation | Expected Success Documentation | Expected Error Documentation |
|-----------|--------------------------------|------------------------------|
| `POST /api/attendance/clock-in` | `201 Created` with `AttendanceRecordResponse` | `400`, `401`, `403`, `409`, `422` where current behavior supports them |
| `POST /api/attendance/clock-out` | `200 OK` with `AttendanceRecordResponse` | `400`, `401`, `403`, `404`, `409`, `422` where current behavior supports them |
| `GET /api/attendance` | `200 OK` with `PagedList<AttendanceRecordResponse>` | `401`, `403`, and applicable service errors |

### Vacation Requests

| Operation | Expected Success Documentation | Expected Error Documentation |
|-----------|--------------------------------|------------------------------|
| `GET /api/vacationrequests` | `200 OK` with `PagedList<VacationRequestResponse>` | `401`, `403`, and applicable service errors |
| `GET /api/vacationrequests/{id}` | `200 OK` with `VacationRequestResponse` | `401`, `403`, `404` |
| `POST /api/vacationrequests` | `201 Created` with `VacationRequestResponse` | `400`, `401`, `403`, `404`, `409`, `422` where current behavior supports them |
| `PUT /api/vacationrequests/{id}/status` | `200 OK` with `VacationRequestResponse` | `400`, `401`, `403`, `404`, `409`, `422` where current behavior supports them |
| `DELETE /api/vacationrequests/{id}` | `204 No Content` | `401`, `403`, `404`, `409`, `422` where current behavior supports them |

### Trips

| Operation | Expected Success Documentation | Expected Error Documentation |
|-----------|--------------------------------|------------------------------|
| `GET /api/trips` | `200 OK` with `PagedList<TripResponse>` | `401`, `403`, and applicable service errors |
| `GET /api/trips/{id}` | `200 OK` with `TripResponse` | `401`, `403`, `404` |
| `POST /api/trips` | `201 Created` with `TripResponse` | `400`, `401`, `403`, `404`, `409`, `422` where current behavior supports them |
| `DELETE /api/trips/{id}` | `204 No Content` | `401`, `403`, `404`, `409`, `422` where current behavior supports them |

### Compensation

| Operation | Expected Success Documentation | Expected Error Documentation |
|-----------|--------------------------------|------------------------------|
| `GET /api/employees/{employeeId}/compensation` | `200 OK` with `CompensationResponse` | `401`, `403`, `404` and applicable service errors |
| `PUT /api/employees/{employeeId}/compensation` | `200 OK` with `CompensationResponse` | `400`, `401`, `403`, `404`, `409`, `422` where current behavior supports them |

### Employee Documents

| Operation | Expected Success Documentation | Expected Error Documentation |
|-----------|--------------------------------|------------------------------|
| `POST /api/employees/{employeeId}/documents` | `201 Created` with `EmployeeDocumentResponse` | `400`, `401`, `403`, `404`, `413`, `422` where current behavior supports them |
| `GET /api/employees/{employeeId}/documents` | `200 OK` with `PagedList<EmployeeDocumentResponse>` | `401`, `403`, `404` and applicable service errors |
| `GET /api/employees/{employeeId}/documents/{documentId}` | `200 OK` with file response | `401`, `403`, `404` and applicable service errors |
| `DELETE /api/employees/{employeeId}/documents/{documentId}` | `204 No Content` | `401`, `403`, `404`, `409`, `422` where current behavior supports them |

### Dashboard

| Operation | Expected Success Documentation | Expected Error Documentation |
|-----------|--------------------------------|------------------------------|
| `GET /api/dashboard/summary` | `200 OK` with `DashboardSummaryResponse` | `401`, `403`, and applicable service errors |

### Audit Logs

| Operation | Expected Success Documentation | Expected Error Documentation |
|-----------|--------------------------------|------------------------------|
| `GET /api/audit-logs` | `200 OK` with `PagedList<AuditLogEntryResponse>` | `401`, `403`, and applicable service errors |

## Verification Contract

Phase 13 is complete only when:

- Swagger UI shows expected success statuses as documented for every reviewed operation.
- The OpenAPI JSON includes all existing routes listed above.
- `POST /api/attendance/clock-in` documents `201 Created`.
- Employee, vacation, and trip scope-hardened endpoints document applicable 401/403/404 outcomes.
- Employee document upload documents `413 Payload Too Large` if the current explicit response path remains present.
- Employee document download is documented as a file response.
- No route, response shape, status behavior, auth behavior, or business behavior changes are included.
