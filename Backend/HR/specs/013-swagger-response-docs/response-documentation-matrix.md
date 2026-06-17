# Response Documentation Matrix: Phase 13

**Purpose**: Track each endpoint group's response documentation entries through Identified → Documented → Verified.

## Legend

- **Status**: `Pending`, `Documented`, `Verified`, or `Follow-up`
- **Category**: Success, ClientError, AuthError, BusinessRule, ServerError
- **Source**: Controller action, ServiceErrorMapping, GlobalExceptionMiddleware

---

## Auth

| Operation | Status Code | Category | Payload Shape | Description | Source | Status |
|-----------|-------------|----------|---------------|-------------|--------|--------|
| `POST /api/auth/login` | 200 | Success | `LoginResponse` | Successful login | AuthController.Login | Documented |
| `POST /api/auth/login` | 400 | ClientError | `ValidationProblemDetails` | Model validation failure | AuthController.Login | Documented |
| `POST /api/auth/login` | 401 | AuthError | `{ code, message }` | Invalid credentials | AuthController.Login | Documented |
| `POST /api/auth/logout` | 204 | Success | No body | Successful logout | AuthController.Logout | Documented |
| `POST /api/auth/logout` | 401 | AuthError | `{ code, message }` | Missing/invalid session | Cookie auth redirect | Documented |
| `GET /api/auth/me` | 200 | Success | `CurrentUserResponse` | Current user info | AuthController.Me | Documented |
| `GET /api/auth/me` | 401 | AuthError | `{ code, message }` | Missing/invalid session | AuthController.Me / Cookie auth | Documented |

## Employees

| Operation | Status Code | Category | Payload Shape | Description | Source | Status |
|-----------|-------------|----------|---------------|-------------|--------|--------|
| `GET /api/employees` | 200 | Success | `PagedList<EmployeeResponse>` | Employee list | EmployeesController.GetEmployees | Documented |
| `GET /api/employees` | 401 | AuthError | `{ code, message }` | Invalid session | EmployeesController.GetEmployees | Documented |
| `GET /api/employees` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `GET /api/employees/{id}` | 200 | Success | `EmployeeResponse` | Employee detail | EmployeesController.GetEmployee | Documented |
| `GET /api/employees/{id}` | 401 | AuthError | `{ code, message }` | Invalid session | EmployeesController.GetEmployee | Documented |
| `GET /api/employees/{id}` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `GET /api/employees/{id}` | 404 | ClientError | `{ code, message }` | Employee not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `POST /api/employees` | 201 | Success | `EmployeeCreatedResponse` | Employee created | EmployeesController.CreateEmployee | Documented |
| `POST /api/employees` | 400 | ClientError | `ValidationProblemDetails` | Model validation failure | EmployeesController.CreateEmployee | Documented |
| `POST /api/employees` | 401 | AuthError | `{ code, message }` | Invalid session | EmployeesController.CreateEmployee | Documented |
| `POST /api/employees` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `POST /api/employees` | 409 | ClientError | `{ code, message }` | Conflict | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `POST /api/employees` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |
| `PUT /api/employees/{id}` | 200 | Success | `EmployeeResponse` | Employee updated | EmployeesController.UpdateEmployee | Documented |
| `PUT /api/employees/{id}` | 400 | ClientError | `ValidationProblemDetails` | Model validation failure | EmployeesController.UpdateEmployee | Documented |
| `PUT /api/employees/{id}` | 401 | AuthError | `{ code, message }` | Invalid session | EmployeesController.UpdateEmployee | Documented |
| `PUT /api/employees/{id}` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `PUT /api/employees/{id}` | 404 | ClientError | `{ code, message }` | Employee not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `PUT /api/employees/{id}` | 409 | ClientError | `{ code, message }` | Conflict | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `PUT /api/employees/{id}` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |
| `DELETE /api/employees/{id}` | 204 | Success | No body | Employee deleted | EmployeesController.DeleteEmployee | Documented |
| `DELETE /api/employees/{id}` | 401 | AuthError | `{ code, message }` | Invalid session | EmployeesController.DeleteEmployee | Documented |
| `DELETE /api/employees/{id}` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `DELETE /api/employees/{id}` | 404 | ClientError | `{ code, message }` | Employee not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `DELETE /api/employees/{id}` | 409 | ClientError | `{ code, message }` | Conflict | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `DELETE /api/employees/{id}` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |
| `PUT /api/employees/{id}/role` | 200 | Success | `EmployeeRoleResponse` | Role updated | EmployeesController.UpdateRole | Documented |
| `PUT /api/employees/{id}/role` | 400 | ClientError | `ValidationProblemDetails` | Model validation failure | EmployeesController.UpdateRole | Documented |
| `PUT /api/employees/{id}/role` | 401 | AuthError | `{ code, message }` | Invalid session | EmployeesController.UpdateRole | Documented |
| `PUT /api/employees/{id}/role` | 403 | AuthError | `{ code, message }` | Forbidden (requires SystemAdministrator) | Cookie auth / ServiceErrorMapping | Documented |
| `PUT /api/employees/{id}/role` | 404 | ClientError | `{ code, message }` | Employee not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `PUT /api/employees/{id}/role` | 409 | ClientError | `{ code, message }` | Conflict | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `PUT /api/employees/{id}/role` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |

## Departments

| Operation | Status Code | Category | Payload Shape | Description | Source | Status |
|-----------|-------------|----------|---------------|-------------|--------|--------|
| `GET /api/departments` | 200 | Success | `PagedList<DepartmentResponse>` | Department list | DepartmentsController.GetDepartments | Documented |
| `GET /api/departments` | 401 | AuthError | `{ code, message }` | Unauthenticated | Cookie auth redirect | Documented |
| `GET /api/departments` | 403 | AuthError | `{ code, message }` | Forbidden | Cookie auth redirect | Documented |
| `GET /api/departments/{id}` | 200 | Success | `DepartmentResponse` | Department detail | DepartmentsController.GetDepartment | Documented |
| `GET /api/departments/{id}` | 401 | AuthError | `{ code, message }` | Unauthenticated | Cookie auth redirect | Documented |
| `GET /api/departments/{id}` | 403 | AuthError | `{ code, message }` | Forbidden | Cookie auth redirect | Documented |
| `GET /api/departments/{id}` | 404 | ClientError | `{ code, message }` | Department not found | DepartmentsController.GetDepartment | Documented |
| `POST /api/departments` | 201 | Success | `DepartmentResponse` | Department created | DepartmentsController.CreateDepartment | Documented |
| `POST /api/departments` | 400 | ClientError | `ValidationProblemDetails` | Model validation failure | DepartmentsController.CreateDepartment | Documented |
| `POST /api/departments` | 401 | AuthError | `{ code, message }` | Unauthenticated | Cookie auth redirect | Documented |
| `POST /api/departments` | 403 | AuthError | `{ code, message }` | Forbidden | Cookie auth redirect | Documented |
| `POST /api/departments` | 409 | ClientError | `{ code, message }` | Conflict | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `POST /api/departments` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |
| `PUT /api/departments/{id}` | 200 | Success | `DepartmentResponse` | Department updated | DepartmentsController.UpdateDepartment | Documented |
| `PUT /api/departments/{id}` | 400 | ClientError | `ValidationProblemDetails` | Model validation failure | DepartmentsController.UpdateDepartment | Documented |
| `PUT /api/departments/{id}` | 401 | AuthError | `{ code, message }` | Unauthenticated | Cookie auth redirect | Documented |
| `PUT /api/departments/{id}` | 403 | AuthError | `{ code, message }` | Forbidden | Cookie auth redirect | Documented |
| `PUT /api/departments/{id}` | 404 | ClientError | `{ code, message }` | Department not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `PUT /api/departments/{id}` | 409 | ClientError | `{ code, message }` | Conflict | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `PUT /api/departments/{id}` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |
| `DELETE /api/departments/{id}` | 204 | Success | No body | Department deleted | DepartmentsController.DeleteDepartment | Documented |
| `DELETE /api/departments/{id}` | 401 | AuthError | `{ code, message }` | Unauthenticated | Cookie auth redirect | Documented |
| `DELETE /api/departments/{id}` | 403 | AuthError | `{ code, message }` | Forbidden | Cookie auth redirect | Documented |
| `DELETE /api/departments/{id}` | 404 | ClientError | `{ code, message }` | Department not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `DELETE /api/departments/{id}` | 409 | ClientError | `{ code, message }` | Conflict | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `DELETE /api/departments/{id}` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |

## Attendance

| Operation | Status Code | Category | Payload Shape | Description | Source | Status |
|-----------|-------------|----------|---------------|-------------|--------|--------|
| `POST /api/attendance/clock-in` | 201 | Success | `AttendanceRecordResponse` | Clock-in recorded | AttendanceController.ClockIn | Documented |
| `POST /api/attendance/clock-in` | 400 | ClientError | `ValidationProblemDetails` | Model validation failure | AttendanceController.ClockIn | Documented |
| `POST /api/attendance/clock-in` | 401 | AuthError | `{ code, message }` | Invalid session | AttendanceController.ClockIn | Documented |
| `POST /api/attendance/clock-in` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `POST /api/attendance/clock-in` | 409 | ClientError | `{ code, message }` | Conflict (already clocked in) | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `POST /api/attendance/clock-in` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |
| `POST /api/attendance/clock-out` | 200 | Success | `AttendanceRecordResponse` | Clock-out recorded | AttendanceController.ClockOut | Documented |
| `POST /api/attendance/clock-out` | 400 | ClientError | `ValidationProblemDetails` | Model validation failure | AttendanceController.ClockOut | Documented |
| `POST /api/attendance/clock-out` | 401 | AuthError | `{ code, message }` | Invalid session | AttendanceController.ClockOut | Documented |
| `POST /api/attendance/clock-out` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `POST /api/attendance/clock-out` | 404 | ClientError | `{ code, message }` | No active clock-in record | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `POST /api/attendance/clock-out` | 409 | ClientError | `{ code, message }` | Conflict | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `POST /api/attendance/clock-out` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |
| `GET /api/attendance` | 200 | Success | `PagedList<AttendanceRecordResponse>` | Attendance list | AttendanceController.GetAttendance | Documented |
| `GET /api/attendance` | 401 | AuthError | `{ code, message }` | Invalid session | AttendanceController.GetAttendance | Documented |
| `GET /api/attendance` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |

## Vacation Requests

| Operation | Status Code | Category | Payload Shape | Description | Source | Status |
|-----------|-------------|----------|---------------|-------------|--------|--------|
| `GET /api/vacationrequests` | 200 | Success | `PagedList<VacationRequestResponse>` | Vacation request list | VacationRequestsController.GetVacationRequests | Documented |
| `GET /api/vacationrequests` | 401 | AuthError | `{ code, message }` | Invalid session | VacationRequestsController.GetVacationRequests | Documented |
| `GET /api/vacationrequests` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `GET /api/vacationrequests/{id}` | 200 | Success | `VacationRequestResponse` | Vacation request detail | VacationRequestsController.GetVacationRequest | Documented |
| `GET /api/vacationrequests/{id}` | 401 | AuthError | `{ code, message }` | Invalid session | VacationRequestsController.GetVacationRequest | Documented |
| `GET /api/vacationrequests/{id}` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `GET /api/vacationrequests/{id}` | 404 | ClientError | `{ code, message }` | Vacation request not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `POST /api/vacationrequests` | 201 | Success | `VacationRequestResponse` | Vacation request created | VacationRequestsController.CreateVacationRequest | Documented |
| `POST /api/vacationrequests` | 400 | ClientError | `ValidationProblemDetails` | Model validation failure | VacationRequestsController.CreateVacationRequest | Documented |
| `POST /api/vacationrequests` | 401 | AuthError | `{ code, message }` | Invalid session | VacationRequestsController.CreateVacationRequest | Documented |
| `POST /api/vacationrequests` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `POST /api/vacationrequests` | 404 | ClientError | `{ code, message }` | Related entity not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `POST /api/vacationrequests` | 409 | ClientError | `{ code, message }` | Conflict | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `POST /api/vacationrequests` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |
| `PUT /api/vacationrequests/{id}/status` | 200 | Success | `VacationRequestResponse` | Status updated | VacationRequestsController.UpdateVacationStatus | Documented |
| `PUT /api/vacationrequests/{id}/status` | 400 | ClientError | `ValidationProblemDetails` | Model validation failure | VacationRequestsController.UpdateVacationStatus | Documented |
| `PUT /api/vacationrequests/{id}/status` | 401 | AuthError | `{ code, message }` | Invalid session | VacationRequestsController.UpdateVacationStatus | Documented |
| `PUT /api/vacationrequests/{id}/status` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `PUT /api/vacationrequests/{id}/status` | 404 | ClientError | `{ code, message }` | Vacation request not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `PUT /api/vacationrequests/{id}/status` | 409 | ClientError | `{ code, message }` | Conflict (invalid status transition) | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `PUT /api/vacationrequests/{id}/status` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |
| `DELETE /api/vacationrequests/{id}` | 204 | Success | No body | Vacation request deleted | VacationRequestsController.DeleteVacationRequest | Documented |
| `DELETE /api/vacationrequests/{id}` | 401 | AuthError | `{ code, message }` | Invalid session | VacationRequestsController.DeleteVacationRequest | Documented |
| `DELETE /api/vacationrequests/{id}` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `DELETE /api/vacationrequests/{id}` | 404 | ClientError | `{ code, message }` | Vacation request not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `DELETE /api/vacationrequests/{id}` | 409 | ClientError | `{ code, message }` | Conflict | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `DELETE /api/vacationrequests/{id}` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |

## Trips

| Operation | Status Code | Category | Payload Shape | Description | Source | Status |
|-----------|-------------|----------|---------------|-------------|--------|--------|
| `GET /api/trips` | 200 | Success | `PagedList<TripResponse>` | Trip list | TripsController.GetTrips | Documented |
| `GET /api/trips` | 401 | AuthError | `{ code, message }` | Invalid session | TripsController.GetTrips | Documented |
| `GET /api/trips` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `GET /api/trips/{id}` | 200 | Success | `TripResponse` | Trip detail | TripsController.GetTrip | Documented |
| `GET /api/trips/{id}` | 401 | AuthError | `{ code, message }` | Invalid session | TripsController.GetTrip | Documented |
| `GET /api/trips/{id}` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `GET /api/trips/{id}` | 404 | ClientError | `{ code, message }` | Trip not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `POST /api/trips` | 201 | Success | `TripResponse` | Trip created | TripsController.CreateTrip | Documented |
| `POST /api/trips` | 400 | ClientError | `ValidationProblemDetails` | Model validation failure | TripsController.CreateTrip | Documented |
| `POST /api/trips` | 401 | AuthError | `{ code, message }` | Invalid session | TripsController.CreateTrip | Documented |
| `POST /api/trips` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `POST /api/trips` | 404 | ClientError | `{ code, message }` | Related entity not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `POST /api/trips` | 409 | ClientError | `{ code, message }` | Conflict | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `POST /api/trips` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |
| `DELETE /api/trips/{id}` | 204 | Success | No body | Trip deleted | TripsController.DeleteTrip | Documented |
| `DELETE /api/trips/{id}` | 401 | AuthError | `{ code, message }` | Invalid session | TripsController.DeleteTrip | Documented |
| `DELETE /api/trips/{id}` | 403 | AuthError | `{ code, message }` | Forbidden | ServiceErrorMapping / Cookie auth | Documented |
| `DELETE /api/trips/{id}` | 404 | ClientError | `{ code, message }` | Trip not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `DELETE /api/trips/{id}` | 409 | ClientError | `{ code, message }` | Conflict | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `DELETE /api/trips/{id}` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |

## Compensation

| Operation | Status Code | Category | Payload Shape | Description | Source | Status |
|-----------|-------------|----------|---------------|-------------|--------|--------|
| `GET /api/employees/{employeeId}/compensation` | 200 | Success | `CompensationResponse` | Compensation details | CompensationController.Get | Documented |
| `GET /api/employees/{employeeId}/compensation` | 401 | AuthError | `{ code, message }` | Invalid session | CompensationController.Get | Documented |
| `GET /api/employees/{employeeId}/compensation` | 403 | AuthError | `{ code, message }` | Forbidden (requires HRAdministrator) | Cookie auth / ServiceErrorMapping | Documented |
| `GET /api/employees/{employeeId}/compensation` | 404 | ClientError | `{ code, message }` | Compensation not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `PUT /api/employees/{employeeId}/compensation` | 200 | Success | `CompensationResponse` | Compensation updated | CompensationController.Update | Documented |
| `PUT /api/employees/{employeeId}/compensation` | 400 | ClientError | `ValidationProblemDetails` | Model validation failure | CompensationController.Update | Documented |
| `PUT /api/employees/{employeeId}/compensation` | 401 | AuthError | `{ code, message }` | Invalid session | CompensationController.Update | Documented |
| `PUT /api/employees/{employeeId}/compensation` | 403 | AuthError | `{ code, message }` | Forbidden (requires HRAdministrator) | Cookie auth / ServiceErrorMapping | Documented |
| `PUT /api/employees/{employeeId}/compensation` | 404 | ClientError | `{ code, message }` | Employee or compensation not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `PUT /api/employees/{employeeId}/compensation` | 409 | ClientError | `{ code, message }` | Conflict | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `PUT /api/employees/{employeeId}/compensation` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |

## Employee Documents

| Operation | Status Code | Category | Payload Shape | Description | Source | Status |
|-----------|-------------|----------|---------------|-------------|--------|--------|
| `POST /api/employees/{employeeId}/documents` | 201 | Success | `EmployeeDocumentResponse` | Document uploaded | EmployeeDocumentsController.Upload | Documented |
| `POST /api/employees/{employeeId}/documents` | 400 | ClientError | `ValidationProblemDetails` | Model validation failure | EmployeeDocumentsController.Upload | Documented |
| `POST /api/employees/{employeeId}/documents` | 401 | AuthError | `{ code, message }` | Invalid session | EmployeeDocumentsController.Upload | Documented |
| `POST /api/employees/{employeeId}/documents` | 403 | AuthError | `{ code, message }` | Forbidden (requires HRAdministrator) | Cookie auth / ServiceErrorMapping | Documented |
| `POST /api/employees/{employeeId}/documents` | 404 | ClientError | `{ code, message }` | Employee not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `POST /api/employees/{employeeId}/documents` | 413 | ClientError | `{ code, message }` | Payload too large | EmployeeDocumentsController.Upload | Documented |
| `POST /api/employees/{employeeId}/documents` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |
| `GET /api/employees/{employeeId}/documents` | 200 | Success | `PagedList<EmployeeDocumentResponse>` | Document list | EmployeeDocumentsController.List | Documented |
| `GET /api/employees/{employeeId}/documents` | 401 | AuthError | `{ code, message }` | Invalid session | EmployeeDocumentsController.List | Documented |
| `GET /api/employees/{employeeId}/documents` | 403 | AuthError | `{ code, message }` | Forbidden (requires HRAdministrator) | Cookie auth / ServiceErrorMapping | Documented |
| `GET /api/employees/{employeeId}/documents` | 404 | ClientError | `{ code, message }` | Employee not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `GET /api/employees/{employeeId}/documents/{documentId}` | 200 | Success | File (binary) | Document download | EmployeeDocumentsController.Download | Documented |
| `GET /api/employees/{employeeId}/documents/{documentId}` | 401 | AuthError | `{ code, message }` | Invalid session | EmployeeDocumentsController.Download | Documented |
| `GET /api/employees/{employeeId}/documents/{documentId}` | 403 | AuthError | `{ code, message }` | Forbidden (requires HRAdministrator) | Cookie auth / ServiceErrorMapping | Documented |
| `GET /api/employees/{employeeId}/documents/{documentId}` | 404 | ClientError | `{ code, message }` | Document not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `DELETE /api/employees/{employeeId}/documents/{documentId}` | 204 | Success | No body | Document deleted | EmployeeDocumentsController.Delete | Documented |
| `DELETE /api/employees/{employeeId}/documents/{documentId}` | 401 | AuthError | `{ code, message }` | Invalid session | EmployeeDocumentsController.Delete | Documented |
| `DELETE /api/employees/{employeeId}/documents/{documentId}` | 403 | AuthError | `{ code, message }` | Forbidden (requires HRAdministrator) | Cookie auth / ServiceErrorMapping | Documented |
| `DELETE /api/employees/{employeeId}/documents/{documentId}` | 404 | ClientError | `{ code, message }` | Document not found | ServiceErrorMapping / ServiceError.NotFound | Documented |
| `DELETE /api/employees/{employeeId}/documents/{documentId}` | 409 | ClientError | `{ code, message }` | Conflict | ServiceErrorMapping / ServiceError.Conflict | Documented |
| `DELETE /api/employees/{employeeId}/documents/{documentId}` | 422 | ClientError | `{ code, message }` | Validation/business rule violation | ServiceErrorMapping / ServiceError.Validation / BusinessRule | Documented |

## Dashboard

| Operation | Status Code | Category | Payload Shape | Description | Source | Status |
|-----------|-------------|----------|---------------|-------------|--------|--------|
| `GET /api/dashboard/summary` | 200 | Success | `DashboardSummaryResponse` | Dashboard summary | DashboardController.Summary | Documented |
| `GET /api/dashboard/summary` | 401 | AuthError | `{ code, message }` | Invalid session | DashboardController.Summary | Documented |
| `GET /api/dashboard/summary` | 403 | AuthError | `{ code, message }` | Forbidden (requires Manager) | Cookie auth / ServiceErrorMapping | Documented |

## Audit Logs

| Operation | Status Code | Category | Payload Shape | Description | Source | Status |
|-----------|-------------|----------|---------------|-------------|--------|--------|
| `GET /api/audit-logs` | 200 | Success | `PagedList<AuditLogEntryResponse>` | Audit log search | AuditLogsController.Search | Documented |
| `GET /api/audit-logs` | 401 | AuthError | `{ code, message }` | Invalid session | AuditLogsController.Search | Documented |
| `GET /api/audit-logs` | 403 | AuthError | `{ code, message }` | Forbidden (requires HRAdministrator) | Cookie auth / ServiceErrorMapping | Documented |

## ServiceErrorMapping Statuses

From `HR.API/Extensions/ServiceErrorMappingExtensions.cs`:

| ServiceError Type | HTTP Status | Error Code Examples | Payload |
|-------------------|-------------|-------------------|---------|
| PAYLOAD_TOO_LARGE (code match) | 413 | `PAYLOAD_TOO_LARGE` | `{ code, message }` |
| NotFound | 404 | Various `NOT_FOUND`-style codes | `{ code, message }` |
| Conflict | 409 | Various `CONFLICT`-style codes | `{ code, message }` |
| Validation | 400 | Various `VALIDATION`-style codes | `{ code, message }` |
| Unauthorized | 401 | Various `UNAUTHORIZED`-style codes | `{ code, message }` |
| Forbidden | 403 | Various `FORBIDDEN`-style codes | `{ code, message }` |
| BusinessRule | 422 | Various `BUSINESS_RULE`-style codes | `{ code, message }` |
| Internal | 500 | Various `SERVER_ERROR`-style codes | `{ code, message }` |
| Default (unmatched) | 400 | Various | `{ code, message }` |

## GlobalExceptionMiddleware Statuses

From `HR.API/Middleware/GlobalExceptionMiddleware.cs`:

| Exception Type | HTTP Status | Error Code | Payload |
|----------------|-------------|------------|---------|
| NotFoundException | 404 | `NOT_FOUND` | `{ code, message }` |
| ConflictException | 409 | `CONFLICT` | `{ code, message }` |
| BusinessRuleException | 422 | `BUSINESS_RULE` | `{ code, message }` |
| Exception (unhandled) | 500 | `SERVER_ERROR` | `{ code, message }` |

## Cookie Auth Redirect Statuses

From Program.cs cookie auth events:

| Event | HTTP Status | Error Code | Payload |
|-------|-------------|------------|---------|
| OnRedirectToLogin | 401 | `UNAUTHORIZED` | `{ code, message }` |
| OnRedirectToAccessDenied | 403 | `FORBIDDEN` | `{ code, message }` |

## Schema Decision Note

Model-validation `400` responses (returned via `ValidationProblem(ModelState)`) are documented with `ValidationProblemDetails` to match current behavior. Service error responses are documented with `ErrorResponseDocumentation` (`{ code, message }`) where a single status schema is possible.
