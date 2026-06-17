# HR API Lifecycle Testing Guide

## 1. What This Guide Is For

This guide explains the practical order for testing the HR backend API end to end. It starts from a fresh or migrated local database and walks through the full HR lifecycle:

- running the API
- creating the first `SystemAdministrator` through startup bootstrap
- logging in with cookie authentication
- creating departments and employees
- assigning roles
- testing attendance, vacations, trips, compensation, documents, dashboard, and audit logs
- running negative/security checks

The examples use the real controllers and DTO fields in this project. JSON uses the API's configured camelCase naming policy.

## 2. Testing Tools

You can test with Swagger, Postman, or a browser.

| Tool | Best Use | Important Cookie Note |
|------|----------|-----------------------|
| Swagger | Quick manual endpoint testing from the API host | Login first through `/api/auth/login`; the browser must keep the auth cookie |
| Postman | Full lifecycle testing with saved variables | Enable the cookie jar and keep the `.AspNetCore.Cookies` cookie after login |

**Important**: Log out or clear cookies before switching between actors. A stale session cookie from a previous actor will produce unexpected authorization results.
| Browser dev tools | Checking cookies and responses | Use the same API origin and prefer HTTPS |

This project uses cookie authentication:

- Login must happen first.
- Login returns employee data in the response body and writes the auth cookie to the response.
- Protected endpoints return `401 Unauthorized` if there is no valid login cookie.
- Protected endpoints return `403 Forbidden` if the user is logged in but does not have the required role/scope.
- `Program.cs` sets `Cookie.SecurePolicy = Always`, so browser cookie testing should use HTTPS, not plain HTTP.
- Swagger does not define a separate bearer-token auth button. Use the login endpoint from Swagger first, then call protected endpoints from the same Swagger browser tab.

## 3. Before You Start

Prerequisites:

- SQL Server is running.
- `HR.API/appsettings.json` or `HR.API/appsettings.Development.json` points to the correct database.
- For Phase 12 manual retest, use the disposable database `HrSystemDb_Phase12LifecycleTest` via environment variable.
- EF migrations are applied.
- The API can start without an old locked `HR.API.exe` process.
- `BusinessSettings:TimeZoneId` is configured, for example `Africa/Cairo`.
- `DocumentStorage:RootPath`, allowed extensions, and max size are configured.
- Initial admin bootstrap values are available through environment variables or user secrets.

Checklist:

- [ ] SQL Server running
- [ ] connection string configured
- [ ] migrations applied
- [ ] a bootstrap department row exists in `Departments`
- [ ] API running
- [ ] Swagger opened over HTTPS when testing cookies
- [ ] bootstrap admin configured
- [ ] ready to login

Useful commands:

```powershell
dotnet restore .\HR.slnx
dotnet build .\HR.slnx -c Release
dotnet ef database update --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

The approved migration list through Phase 11 required for Phase 12 retest:

```text
20251114215718_InitialCreate
20260603014628_Phase5HrBusinessRules
20260606235241_Phase7AdvancedHrFeatures
20260615170903_AddVacationRequestCreatedByEmployee
20260615212225_AddTripRequesterEmployee
```

If Visual Studio build fails because files are locked, stop the running `HR.API` process first.

## 4. Recommended Test Data

Use these values for local testing. Replace passwords before real client use.

### Initial Admin

| Field | Value |
|-------|-------|
| EmployeeNumber | `EMP001` |
| Email | `admin@test.com` |
| FullName | `System Admin` |
| TemporaryPassword | `<TEMPORARY_STRONG_PASSWORD>` |
| Role | `SystemAdministrator` |

### Departments

| Placeholder | Name |
|-------------|------|
| `<BOOTSTRAP_DEPARTMENT_ID>` | `Administration` |
| `<HR_DEPARTMENT_ID>` | `Human Resources` |
| `<ENGINEERING_DEPARTMENT_ID>` | `Engineering` |

### Employees

| Placeholder | EmployeeNumber | Email | FullName | Target Role |
|-------------|----------------|-------|----------|-------------|
| `<SYSTEM_ADMIN_EMPLOYEE_ID>` | `EMP001` | `admin@test.com` | `System Admin` | `SystemAdministrator` |
| `<HR_ADMIN_EMPLOYEE_ID>` | `EMP002` | `hr.admin@test.com` | `HR Admin` | `HRAdministrator` |
| `<MANAGER_EMPLOYEE_ID>` | `EMP003` | `manager@test.com` | `Engineering Manager` | `Manager` |
| `<EMPLOYEE_ID>` | `EMP004` | `employee@test.com` | `Normal Employee` | `Employee` |

Suggested local-only test passwords:

```text
<TEMPORARY_STRONG_PASSWORD>
<HR_ADMIN_PASSWORD>
<MANAGER_PASSWORD>
<EMPLOYEE_PASSWORD>
```

Password policy in infrastructure requires at least 8 characters, one digit, one uppercase letter, and one lowercase letter. Non-alphanumeric characters are not required.

## 5. Step 1 - Configure Initial Admin Bootstrap

Initial System Administrator bootstrap is not an HTTP endpoint. It runs during API startup through `InitialSystemAdminBootstrapper`.

Before enabling bootstrap on an empty database, create one department row because bootstrap requires `InitialAdminBootstrap:DepartmentId` to reference an existing department.

Run this SQL against `HrSystemDb`:

```sql
DECLARE @DepartmentId uniqueidentifier = NEWID();

INSERT INTO Departments (Id, Name)
VALUES (@DepartmentId, N'Administration');

SELECT @DepartmentId AS BootstrapDepartmentId;
```

Copy the returned value and save it as:

```text
<BOOTSTRAP_DEPARTMENT_ID>
```

Set bootstrap values through environment variables or user secrets. Do not hardcode real customer passwords in committed config.

PowerShell example:

```powershell
$env:InitialAdminBootstrap__Enabled="true"
$env:InitialAdminBootstrap__Mode="CreateInitialAdmin"
$env:InitialAdminBootstrap__EmployeeNumber="EMP001"
$env:InitialAdminBootstrap__Email="admin@test.com"
$env:InitialAdminBootstrap__FullName="System Admin"
$env:InitialAdminBootstrap__DepartmentId="<BOOTSTRAP_DEPARTMENT_ID>"
$env:InitialAdminBootstrap__TemporaryPassword="<TEMPORARY_STRONG_PASSWORD>"
$env:InitialAdminBootstrap__ForcePasswordChange="true"
```

Bootstrap behavior to verify:

- If no active `SystemAdministrator` exists, startup creates one `ApplicationUser` and linked `Employee`.
- The created employee role is `SystemAdministrator`.
- A successful bootstrap audit row uses actor marker `SYSTEM_BOOTSTRAP`.
- If an active `SystemAdministrator` already exists, bootstrap does nothing.
- Bootstrap does not use a fallback employee.
- Missing config, duplicate email, duplicate employee number, invalid password, or missing department should fail startup clearly.

## 6. Step 2 - Run the API

Required command:

```powershell
dotnet run --project .\HR.API\HR.API.csproj
```

Recommended command for browser/Swagger cookie testing:

```powershell
dotnet run --project .\HR.API\HR.API.csproj --launch-profile https
```

Launch settings currently expose:

```text
https://localhost:7162
http://localhost:5098
```

Open Swagger:

```text
https://localhost:7162/swagger
```

If you use HTTP:

```text
http://localhost:5098/swagger
```

For authenticated browser testing, prefer the HTTPS Swagger URL because auth cookies are configured as secure.

## 7. Step 3 - Login as System Administrator

Endpoint:

```http
POST /api/auth/login
```

Request body uses `LoginRequest`, which has `identifier` and `password`. Use the admin email as the identifier:

```json
{
  "identifier": "admin@test.com",
  "password": "<TEMPORARY_STRONG_PASSWORD>"
}
```

Expected success:

- Status: `200 OK`
- Response body wrapper: `{ "employee": { ... } }`
- Auth cookie: set by the response

Example response shape:

```json
{
  "employee": {
    "id": "00000000-0000-0000-0000-000000000000",
    "employeeNumber": "EMP001",
    "fullName": "System Admin",
    "email": "admin@test.com",
    "departmentId": "00000000-0000-0000-0000-000000000000",
    "departmentName": "Administration",
    "managerId": null,
    "managerName": null,
    "birthDate": null,
    "joinDate": null,
    "jobTitle": null,
    "phoneNumber": null,
    "notes": null,
    "status": "Active",
    "role": "SystemAdministrator",
    "vacationBalanceDays": 21,
    "isDeleted": false,
    "terminatedAt": null,
    "identityUserId": "identity-user-id",
    "userName": "admin@test.com"
  }
}
```

Copy:

```text
employee.id -> <SYSTEM_ADMIN_EMPLOYEE_ID>
employee.departmentId -> <BOOTSTRAP_DEPARTMENT_ID>
```

Verify current user:

```http
GET /api/auth/me
```

Expected response:

```json
{
  "employeeId": "<SYSTEM_ADMIN_EMPLOYEE_ID>",
  "fullName": "System Admin",
  "email": "admin@test.com",
  "role": "SystemAdministrator"
}
```

### Expected Auth/Me Results for All Actors

| Actor | Email | Expected Role |
|-------|-------|---------------|
| EMP001 | `admin@test.com` | `SystemAdministrator` |
| EMP002 | `hr.admin@test.com` | `HRAdministrator` |
| EMP003 | `manager@test.com` | `Manager` |
| EMP004 | `employee@test.com` | `Employee` |

The returned role determines what endpoints each actor can access. See employee, vacation, trip, and sensitive module sections for role-specific behavior.

Common login failures:

- Wrong field name: use `identifier`, not `email`.
- Bootstrap did not run.
- Temporary password does not satisfy Identity rules.
- Account is soft-deleted or terminated.
- Testing over HTTP causes secure cookie not to be sent on later requests.

## 8. Step 4 - Disable Bootstrap After First Login

After confirming the first admin can login, disable bootstrap.

If using PowerShell environment variables:

```powershell
$env:InitialAdminBootstrap__Enabled="false"
```

Then restart the API process.

If using user secrets or deployment environment variables, remove the temporary bootstrap password and set:

```text
InitialAdminBootstrap__Enabled=false
```

Why this matters:

- Bootstrap is only for first-run administration.
- The API no-ops when an active System Administrator exists, but secrets should still not remain configured.
- A restart is required for changed environment variables or config values to apply.

## 9. Step 5 - Create Core Departments

Use the logged-in System Administrator session.

Current controller-level requirement: authenticated user. Recommended lifecycle actor: `SystemAdministrator`.

### Create HR Department

```http
POST /api/departments
```

Request body uses `DepartmentCreateRequest`:

```json
{
  "name": "Human Resources"
}
```

Expected:

- Status: `201 Created`
- Response body has `id`, `name`, `employeeCount`

Copy:

```text
id -> <HR_DEPARTMENT_ID>
```

### Create Engineering Department

```http
POST /api/departments
```

```json
{
  "name": "Engineering"
}
```

Copy:

```text
id -> <ENGINEERING_DEPARTMENT_ID>
```

List departments:

```http
GET /api/departments?page=1&pageSize=25
```

Paged response fields:

```json
{
  "items": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 25,
  "totalPages": 0,
  "hasNext": false,
  "hasPrevious": false
}
```

## 10. Step 6 - Create Employees

Endpoint:

```http
POST /api/employees
```

Effective authorization (Phase 9): `HRAdministrator` or `SystemAdministrator` only; Employee/Manager roles receive `403 Forbidden`. Recommended lifecycle actor: `SystemAdministrator` or `HRAdministrator`.

Create responses use:

```json
{
  "employee": {
    "id": "..."
  },
  "temporaryPassword": "..."
}
```

If `initialPassword` is supplied, `temporaryPassword` is normally `null`. Use the supplied password for login.

### Create HR Administrator Employee

```json
{
  "employeeNumber": "EMP002",
  "fullName": "HR Admin",
  "email": "hr.admin@test.com",
  "departmentId": "<HR_DEPARTMENT_ID>",
  "managerId": null,
  "birthDate": "1990-01-15",
  "joinDate": "2026-06-01",
  "jobTitle": "HR Administrator",
  "phoneNumber": "+201000000002",
  "notes": "Lifecycle test HR admin",
  "status": "Active",
  "initialPassword": "<HR_ADMIN_PASSWORD>"
}
```

Copy:

```text
employee.id -> <HR_ADMIN_EMPLOYEE_ID>
```

### Create Manager Employee

```json
{
  "employeeNumber": "EMP003",
  "fullName": "Engineering Manager",
  "email": "manager@test.com",
  "departmentId": "<ENGINEERING_DEPARTMENT_ID>",
  "managerId": null,
  "birthDate": "1988-02-20",
  "joinDate": "2026-06-01",
  "jobTitle": "Engineering Manager",
  "phoneNumber": "+201000000003",
  "notes": "Lifecycle test manager",
  "status": "Active",
  "initialPassword": "<MANAGER_PASSWORD>"
}
```

Copy:

```text
employee.id -> <MANAGER_EMPLOYEE_ID>
```

### Create Normal Employee

```json
{
  "employeeNumber": "EMP004",
  "fullName": "Normal Employee",
  "email": "employee@test.com",
  "departmentId": "<ENGINEERING_DEPARTMENT_ID>",
  "managerId": "<MANAGER_EMPLOYEE_ID>",
  "birthDate": "1996-03-10",
  "joinDate": "2026-06-01",
  "jobTitle": "Software Engineer",
  "phoneNumber": "+201000000004",
  "notes": "Lifecycle test employee",
  "status": "Active",
  "initialPassword": "<EMPLOYEE_PASSWORD>"
}
```

Copy:

```text
employee.id -> <EMPLOYEE_ID>
```

Verify employees:

```http
GET /api/employees?page=1&pageSize=25
GET /api/employees/<EMPLOYEE_ID>
```

Important employee rules:

- `employeeNumber` is created only through `EmployeeCreateRequest`; update DTO does not expose it.
- Duplicate employee numbers return `409 Conflict`.
- Duplicate active employee email returns `409 Conflict`.
- Manager cycles are rejected with a business-rule error.
- Cross-department manager assignment is allowed but logged as a warning.
- New employees default to role `Employee` until role assignment is called.

### Employee Authorization Matrix (Phase 9)

| Operation | Employee | Manager | HRAdministrator | SystemAdministrator |
|-----------|----------|---------|-----------------|---------------------|
| `GET /api/employees` | `403` | Team only | Org-wide | Org-wide |
| `GET /api/employees/{id}` | Self only | Self + team | Any employee | Any employee |
| `POST /api/employees` | `403` | `403` | Allowed | Allowed |
| `PUT /api/employees/{id}` | `403` | `403` | Non-SystemAdmin targets | Allowed (last-admin guard) |
| `DELETE /api/employees/{id}` | `403` | `403` | Non-SystemAdmin targets | Allowed (last-admin guard) |
| `PUT /api/employees/{id}/role` | `403` | `403` | `403` | Allowed (last-admin demotion guard) |

Note: HRAdministrator receives `403` for role assignment. Only SystemAdministrator can assign roles.
Note: Last-active-SystemAdministrator protection prevents deleting, terminating, or demoting the only remaining SystemAdministrator.

## 11. Step 7 - Assign or Verify Roles

Roles in this system:

| Role | Meaning |
|------|---------|
| `Employee` | Normal employee |
| `Manager` | Team manager with team-scoped access |
| `HRAdministrator` | HR admin with sensitive HR access |
| `SystemAdministrator` | Highest admin role; can assign roles |

Role assignment endpoint:

```http
PUT /api/employees/{id}/role
```

Required role:

```text
SystemAdministrator
```

### Assign HR Administrator Role

```http
PUT /api/employees/<HR_ADMIN_EMPLOYEE_ID>/role
```

```json
{
  "role": "HRAdministrator"
}
```

Expected:

```json
{
  "employeeId": "<HR_ADMIN_EMPLOYEE_ID>",
  "role": "HRAdministrator",
  "updatedAt": "2026-06-07T09:00:00+00:00"
}
```

### Assign Manager Role

```http
PUT /api/employees/<MANAGER_EMPLOYEE_ID>/role
```

```json
{
  "role": "Manager"
}
```

Expected: `200 OK`.

### Verify Normal Employee Role

No role update is needed for the normal employee if the response already has:

```json
{
  "role": "Employee"
}
```

Role behavior:

- System Admin can assign roles.
- HR Admin and System Admin can access compensation, documents, audit logs, and organization dashboard.
- Manager can access dashboard and team-scoped attendance views.
- Normal Employee can login and record own attendance.
- Same-role assignment is treated as success and should not duplicate side effects.

## 12. Step 8 - Login as Different Roles

Use this pattern for each role:

1. Logout current session.
2. Login as the test user.
3. Call one allowed endpoint.
4. Call one forbidden endpoint.

Logout:

```http
POST /api/auth/logout
```

Expected: `204 No Content`.

### System Administrator

Login:

```json
{
  "identifier": "admin@test.com",
  "password": "<TEMPORARY_STRONG_PASSWORD>"
}
```

Allowed tests:

```http
GET /api/employees?page=1&pageSize=25
GET /api/employees/<EMPLOYEE_ID>
POST /api/employees
PUT /api/employees/<EMPLOYEE_ID>/role
```

Forbidden test: none expected for SystemAdministrator (wide access, except last-admin guard applies).

### HR Administrator

Login:

```json
{
  "identifier": "hr.admin@test.com",
  "password": "<HR_ADMIN_PASSWORD>"
}
```

Allowed tests:

```http
GET /api/employees?page=1&pageSize=25
GET /api/employees/<EMPLOYEE_ID>
POST /api/employees
GET /api/audit-logs?page=1&pageSize=25
```

Forbidden tests:

```http
PUT /api/employees/<SYSTEM_ADMIN_EMPLOYEE_ID>
PUT /api/employees/<EMPLOYEE_ID>/role
```

Expected: `403 Forbidden`. HRAdmin cannot assign roles or modify SystemAdministrator records.

### Manager

Login:

```json
{
  "identifier": "manager@test.com",
  "password": "<MANAGER_PASSWORD>"
}
```

Allowed tests:

```http
GET /api/employees?page=1&pageSize=25
GET /api/employees/<EMP004_ID>
GET /api/dashboard/summary
```

Forbidden tests:

```http
GET /api/employees/<EMP001_ID>   (outside scope)
POST /api/employees
GET /api/audit-logs?page=1&pageSize=25
```

Expected: `403 Forbidden` for out-of-scope detail, employee creation, and audit logs. Manager team list shows only active direct/indirect reports.

### Normal Employee

Login:

```json
{
  "identifier": "employee@test.com",
  "password": "<EMPLOYEE_PASSWORD>"
}
```

Allowed tests:

```http
GET /api/employees/<EMP004_ID>   (self detail)
POST /api/attendance/clock-in
```

Forbidden tests:

```http
GET /api/employees?page=1&pageSize=25
GET /api/employees/<EMP001_ID>   (outside scope)
POST /api/employees
GET /api/employees/<EMP004_ID>/compensation
GET /api/dashboard/summary
GET /api/audit-logs?page=1&pageSize=25
```

Expected: `403 Forbidden` for employee list, out-of-scope detail, employee create, compensation, dashboard, and audit logs.

## 13. Step 9 - Attendance Lifecycle

Attendance uses the authenticated employee from the cookie. The client does not send the employee ID or local date for clock-in/clock-out.

Business-time behavior:

- `clockInAtUtc` and `clockOutAtUtc` are stored as UTC timestamps.
- `attendanceDate` is derived from `BusinessSettings:TimeZoneId`.
- The default local setting is `Africa/Cairo`.
- The API does not trust client-provided local dates.

### Clock In

Login as `employee@test.com`.

```http
POST /api/attendance/clock-in
```

```json
{
  "notes": "Morning check-in"
}
```

Expected:

- Status: `201 Created`
- Copy `id` if you want to inspect the database later.
- Save `attendanceDate` for query checks.

### Duplicate Clock In

Call the same endpoint again on the same business date:

```json
{
  "notes": "Duplicate check-in attempt"
}
```

Expected:

- Status: `409 Conflict`
- Structured payload:

```json
{
  "code": "CONFLICT",
  "message": "Attendance has already been recorded for this business date."
}
```

### Clock Out

```http
POST /api/attendance/clock-out
```

```json
{
  "notes": "End of day"
}
```

Expected:

- Status: `200 OK`
- `clockOutAtUtc` is set.
- `workedHours` is calculated.

### Clock Out Without Open Record

Login as another active employee who has not clocked in today, then call:

```http
POST /api/attendance/clock-out
```

Expected:

- Status: `404 Not Found`
- Message: no open attendance record for this business date.

### Query Attendance

As employee, own records:

```http
GET /api/attendance?employeeId=<EMPLOYEE_ID>&page=1&pageSize=25
```

As manager, team records:

```http
GET /api/attendance?employeeId=<EMPLOYEE_ID>&page=1&pageSize=25
```

As HR Admin or System Admin, all records:

```http
GET /api/attendance?from=2026-06-01&to=2026-06-30&page=1&pageSize=25
```

Expected list response: paged envelope with `items`.

## 14. Step 10 - Vacation Request Lifecycle

Vacation endpoints:

```http
GET /api/vacationrequests
GET /api/vacationrequests/{id}
POST /api/vacationrequests
PUT /api/vacationrequests/{id}/status
DELETE /api/vacationrequests/{id}
```

### Vacation Authorization Scope (Phase 10)

| Operation | Employee | Manager | HRAdministrator | SystemAdministrator |
|-----------|----------|---------|-----------------|---------------------|
| `GET /api/vacationrequests` | Own only | Team only | Org-wide | Org-wide |
| `GET /api/vacationrequests/{id}` | Own only | Self + team | Any | Any |
| `POST /api/vacationrequests` | Self only | Self only | For any employee | For any employee |
| `PUT /api/vacationrequests/{id}/status` | Self-review blocked | Team only (self blocked) | Org-wide (self blocked) | Org-wide (self blocked) |
| `DELETE /api/vacationrequests/{id}` | Own only | Team only | Org-wide | Org-wide |

Self-review is blocked for every role. Out-of-scope vacation detail returns `404` or `403` depending on scenario. Out-of-scope list filters return an empty scoped page with normal pagination shape.

Recommended lifecycle actors:

- Employee submits own vacation.
- Manager or HR Administrator reviews it.

Use future working dates. Friday and Saturday are non-working days in `WorkingDayCalendar`. The service requires at least three full working days of notice.

### Employee Creates Vacation Request

Login as `employee@test.com`.

```http
POST /api/vacationrequests
```

```json
{
  "employeeId": "<EMPLOYEE_ID>",
  "startDate": "2026-07-06",
  "endDate": "2026-07-08",
  "reason": "Annual leave"
}
```

If these dates are now in the past, replace them with future working dates that have at least three full working days of notice.

Expected:

- Status: `201 Created`
- `status`: `Pending`
- `workingDayCount`: calculated excluding Friday/Saturday
- `createdByEmployeeId`: the authenticated employee who submitted the request
- `createdByEmployeeName`: the authenticated employee name when available

Copy:

```text
id -> <VACATION_REQUEST_ID>
```

### Manager or HR Approves Vacation

Logout and login as `manager@test.com` or `hr.admin@test.com`.

```http
PUT /api/vacationrequests/<VACATION_REQUEST_ID>/status
```

```json
{
  "status": "Approved"
}
```

Expected:

- Status: `200 OK`
- `status`: `Approved`
- employee vacation balance is deducted once.

### Reject an Approved Request

```http
PUT /api/vacationrequests/<VACATION_REQUEST_ID>/status
```

```json
{
  "status": "Rejected"
}
```

Expected:

- Status: `200 OK`
- `status`: `Rejected`
- vacation balance is restored once.

### Same-Status No-Op

Call the same status update again:

```json
{
  "status": "Rejected"
}
```

Expected: `200 OK` with no duplicate side effects.

### Invalid Transition

Try changing a rejected request back to approved:

```json
{
  "status": "Approved"
}
```

Expected:

- Status: `422 Unprocessable Entity`
- Structured `{ "code": "BUSINESS_RULE_VIOLATION", "message": "..." }`

### Self-Approval

Login as the employee who created the request and try to approve it.

Expected:

- Status: `422 Unprocessable Entity`
- Message: employees cannot approve or reject their own vacation requests.

### Overlap Test

Create a second pending or approved request for overlapping dates.

Expected:

- Status: `422 Unprocessable Entity`
- Message: employee already has an overlapping pending or approved vacation request.

Rejected requests do not block overlap.

## 15. Step 11 - Trip / Transportation Lifecycle

Trip endpoints:

```http
GET /api/trips
GET /api/trips/{id}
POST /api/trips
DELETE /api/trips/{id}
```

### Trip Authorization Scope (Phase 11)

| Operation | Employee | Manager | HRAdministrator | SystemAdministrator |
|-----------|----------|---------|-----------------|---------------------|
| `GET /api/trips` | Own only | Own + active team | Org-wide | Org-wide |
| `GET /api/trips/{id}` | Own only | Own + team | Any | Any |
| `POST /api/trips` | Self only | Self + team | For any employee | For any employee |
| `DELETE /api/trips/{id}` | Own only | Own + team | Any | Any |

Important: The current API does not have trip review, approve, or reject endpoints. Test create, list, get by id, traveler validation, requester metadata, date validation, and delete. The `requestedByEmployeeId` request field is compatibility traveler data; the authenticated employee is recorded separately as requester.

The response includes both traveler (`requestedByEmployeeId`, `requestedByEmployeeName`) and requester (`requesterEmployeeId`, `requesterEmployeeName`) fields. For historical trips, `requesterEmployeeId` may be null. Out-of-scope trip detail/delete returns `403 Forbidden`. Out-of-scope list filters return an empty page with normal pagination shape.

### Create Trip Request

Login as HR Admin or System Admin for organization-wide validation. Repeat the same flow as an Employee for self-only behavior and as a Manager for own/team behavior.

```http
POST /api/trips
```

```json
{
  "requestedByEmployeeId": "<EMPLOYEE_ID>",
  "referenceName": "Client Site Visit",
  "project": "Project Alpha",
  "route": "Cairo Office to Client Site",
  "tripType": "Business",
  "tripDate": "2026-07-06"
}
```

If this date is now in the past, replace it with a future working day. Friday and Saturday are rejected as non-working days.

Expected:

- Status: `201 Created`
- Response has existing fields `id`, `requestedByEmployeeId`, `requestedByEmployeeName`, `tripCode`, `requestCode`
- Response also includes additive `travelerEmployeeId`, `travelerEmployeeName`, `requesterEmployeeId`, and `requesterEmployeeName`

Copy:

```text
id -> <TRIP_ID>
tripCode -> <TRIP_CODE>
requestCode -> <REQUEST_CODE>
```

### Query Trips

```http
GET /api/trips?page=1&pageSize=25
GET /api/trips?travelerEmployeeId=<EMPLOYEE_ID>&page=1&pageSize=25
GET /api/trips/<TRIP_ID>
```

Expected scope behavior:

- Employee: own trips only. Out-of-scope `travelerEmployeeId` filters return an empty page.
- Manager: own plus active direct/indirect team trips only. Peer, unrelated, soft-deleted, or terminated report filters return an empty page.
- HR/System: organization-wide trips.
- Existing out-of-scope trip detail/delete returns `403 Forbidden`; missing trip IDs return `404 Not Found`.

### Invalid Trip Tests

Missing target traveler for HR/System:

```json
{
  "requestedByEmployeeId": "00000000-0000-0000-0000-000000000000",
  "referenceName": "Invalid traveler",
  "project": "Project Alpha",
  "route": "Cairo",
  "tripType": "Business",
  "tripDate": "2026-07-06"
}
```

Expected: `404 Not Found`.

Out-of-scope traveler for Employee or Manager:

- Create for another employee outside scope: `403 Forbidden`
- Detail/delete existing trip outside scope: `403 Forbidden`
- List filter outside scope: successful empty page

Past or non-working trip date:

- Past date: `422 Unprocessable Entity`
- Friday/Saturday date: `422 Unprocessable Entity`

### Delete Trip

```http
DELETE /api/trips/<TRIP_ID>
```

Expected: `204 No Content`.

## 16. Step 12 - Compensation Lifecycle

Compensation endpoints are served by `CompensationController`.

```http
GET /api/employees/{employeeId}/compensation
PUT /api/employees/{employeeId}/compensation
```

Required role:

```text
HRAdministrator or SystemAdministrator
```

Normal employees and managers must not access compensation endpoints. Normal employee profile/list DTOs do not include salary fields.

### Create or Update Compensation

Login as `hr.admin@test.com`.

```http
PUT /api/employees/<EMPLOYEE_ID>/compensation
```

```json
{
  "baseSalary": 25000.00,
  "salaryCurrency": "EGP",
  "lastSalaryReviewDate": "2026-06-01"
}
```

Expected:

- Status: `200 OK`
- Creates compensation if none exists.
- Updates compensation if it already exists.
- Adds salary history when values change.
- Does not add duplicate salary history when submitted values do not change.

### View Compensation

```http
GET /api/employees/<EMPLOYEE_ID>/compensation
```

Expected response:

```json
{
  "employeeId": "<EMPLOYEE_ID>",
  "baseSalary": 25000.00,
  "salaryCurrency": "EGP",
  "lastSalaryReviewDate": "2026-06-01",
  "history": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "changedAt": "2026-06-07T09:15:00+00:00",
      "changedByEmployeeId": "<HR_ADMIN_EMPLOYEE_ID>",
      "previousBaseSalary": null,
      "newBaseSalary": 25000.00,
      "previousCurrency": null,
      "newCurrency": "EGP",
      "previousReviewDate": null,
      "newReviewDate": "2026-06-01"
    }
  ]
}
```

### Forbidden Access Test

Login as `employee@test.com`.

```http
GET /api/employees/<EMPLOYEE_ID>/compensation
```

Expected: `403 Forbidden`.

### Invalid Compensation Tests

Negative salary:

```json
{
  "baseSalary": -1,
  "salaryCurrency": "EGP",
  "lastSalaryReviewDate": "2026-06-01"
}
```

Expected: `422 Unprocessable Entity`.

Invalid currency:

```json
{
  "baseSalary": 25000,
  "salaryCurrency": "12",
  "lastSalaryReviewDate": "2026-06-01"
}
```

Expected: `422 Unprocessable Entity`.

## 17. Step 13 - Employee Documents Lifecycle

Document endpoints:

```http
POST /api/employees/{employeeId}/documents
GET /api/employees/{employeeId}/documents
GET /api/employees/{employeeId}/documents/{documentId}
DELETE /api/employees/{employeeId}/documents/{documentId}
```

Required role:

```text
HRAdministrator or SystemAdministrator
```

Document storage config:

```json
{
  "DocumentStorage": {
    "RootPath": "App_Data/EmployeeDocuments",
    "AllowedExtensions": [ ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" ],
    "MaxFileSizeBytes": 10485760
  }
}
```

Allowed categories:

```text
Identity
Contract
Certificate
Other
```

### Upload Employee Document

Swagger upload uses `multipart/form-data`.

```http
POST /api/employees/<EMPLOYEE_ID>/documents
```

Form fields:

| Field | Type | Example |
|-------|------|---------|
| `category` | text enum | `Contract` |
| `file` | file | `employment-contract.pdf` |

Expected:

- Status: `201 Created`
- Response includes document metadata only, not binary content.

Example response:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "employeeId": "<EMPLOYEE_ID>",
  "category": "Contract",
  "originalFileName": "employment-contract.pdf",
  "fileSizeBytes": 102400,
  "contentType": "application/pdf",
  "uploadedByEmployeeId": "<HR_ADMIN_EMPLOYEE_ID>",
  "uploadedAt": "2026-06-07T09:30:00+00:00"
}
```

Copy:

```text
id -> <DOCUMENT_ID>
```

### List Documents

```http
GET /api/employees/<EMPLOYEE_ID>/documents?page=1&pageSize=25
```

Expected: paged envelope with current, non-removed documents.

### Download Document

```http
GET /api/employees/<EMPLOYEE_ID>/documents/<DOCUMENT_ID>
```

Expected:

- Status: `200 OK`
- File content returned through the authorized API endpoint.
- Download name uses original file name metadata.

### Remove Document

```http
DELETE /api/employees/<EMPLOYEE_ID>/documents/<DOCUMENT_ID>
```

Expected:

- Status: `204 No Content`
- Metadata is marked removed.
- Physical file is deleted from backend-managed local storage.
- Removal is audited.

### Verify Removed Document Cannot Be Downloaded

```http
GET /api/employees/<EMPLOYEE_ID>/documents/<DOCUMENT_ID>
```

Expected: `404 Not Found`.

### Oversized File Test

Upload a file larger than `10485760` bytes.

Expected:

- Status: `413 Payload Too Large`
- Structured payload:

```json
{
  "code": "PAYLOAD_TOO_LARGE",
  "message": "Uploaded document exceeds the maximum file size."
}
```

### Invalid File Type Test

Upload a file with an extension outside the allowed list, for example `.exe`.

Expected:

- Status: `422 Unprocessable Entity`
- Structured `{ "code": "BUSINESS_RULE_VIOLATION", "message": "..." }`

## 18. Step 14 - Dashboard Lifecycle

Endpoint:

```http
GET /api/dashboard/summary
```

Required role:

```text
Manager, HRAdministrator, or SystemAdministrator
```

Metric scope:

| Metric | Manager | HRAdministrator | SystemAdministrator |
|--------|---------|-----------------|---------------------|
| `totalActiveEmployees` | Team-scoped direct/indirect reports | Organization-wide | Organization-wide |
| `totalDepartments` | Hidden/null | Organization-wide | Organization-wide |
| `pendingVacationRequests` | Team-scoped | Organization-wide | Organization-wide |
| `approvedVacationsThisMonth` | Team-scoped | Organization-wide | Organization-wide |
| `employeesOnVacationToday` | Team-scoped | Organization-wide | Organization-wide |
| `newHiresThisMonth` | Team-scoped | Organization-wide | Organization-wide |
| `upcomingTripsThisWeek` | Team-scoped | Organization-wide | Organization-wide |
| `employeesPerDepartment` | Team-scoped grouped by department | Organization-wide | Organization-wide |
| `vacationRequestsByStatus` | Team-scoped | Organization-wide | Organization-wide |

### Manager Dashboard

Login as `manager@test.com`.

```http
GET /api/dashboard/summary
```

Expected:

- Status: `200 OK`
- `totalDepartments` is `null`
- metrics are scoped to direct and indirect reports

### HR Administrator Dashboard

Login as `hr.admin@test.com`.

```http
GET /api/dashboard/summary
```

Expected:

- Status: `200 OK`
- `totalDepartments` has an organization-wide value
- metrics are organization-wide

### Normal Employee Forbidden Test

Login as `employee@test.com`.

```http
GET /api/dashboard/summary
```

Expected: `403 Forbidden`.

## 19. Step 15 - Audit Logs Lifecycle

Endpoint:

```http
GET /api/audit-logs
```

Required role:

```text
HRAdministrator or SystemAdministrator
```

Audit query parameters:

| Query | Type |
|-------|------|
| `entityType` | string |
| `entityId` | Guid |
| `actorEmployeeId` | Guid |
| `action` | `AuditActionType` string |
| `from` | DateTimeOffset |
| `to` | DateTimeOffset |
| `page` | int |
| `pageSize` | int |

Actions that should create audit logs include:

- initial admin bootstrap: `InitialAdminCreated`
- role assignment: `RoleChanged`
- attendance clock-in: `ClockedIn`
- attendance clock-out: `ClockedOut`
- compensation change: `CompensationChanged`
- document upload: `DocumentUploaded`
- document removal: `DocumentRemoved`

The enum also includes:

```text
Created
Updated
Deleted
StatusChanged
SystemAdministratorBootstrapped
```

### Query Recent Audit Logs

Login as HR Admin or System Admin.

```http
GET /api/audit-logs?page=1&pageSize=25
```

Expected response:

```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "entityType": "Employee",
      "entityId": "00000000-0000-0000-0000-000000000000",
      "actionType": "RoleChanged",
      "actorEmployeeId": "<SYSTEM_ADMIN_EMPLOYEE_ID>",
      "actorMarker": null,
      "performedAt": "2026-06-07T10:00:00+00:00",
      "changedFields": [ "Role" ],
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

### Filter by Action

```http
GET /api/audit-logs?action=RoleChanged&page=1&pageSize=25
GET /api/audit-logs?action=DocumentUploaded&page=1&pageSize=25
GET /api/audit-logs?action=CompensationChanged&page=1&pageSize=25
```

### Bootstrap Audit

Search:

```http
GET /api/audit-logs?action=InitialAdminCreated&page=1&pageSize=25
```

Expected bootstrap audit characteristics:

- `actorEmployeeId`: `null`
- `actorMarker`: `SYSTEM_BOOTSTRAP`
- affected `entityType`: `Employee`
- `newValues` identifies employee number, email, and role
- no temporary password, password hash, token, cookie, or security stamp is present

### Forbidden Audit Test

Login as normal employee.

```http
GET /api/audit-logs?page=1&pageSize=25
```

Expected: `403 Forbidden`.

## 20. Step 16 - Negative / Security Tests

Expected failure examples:

### Protected Endpoint Without Login

Logout or clear cookies, then call:

```http
GET /api/auth/me
```

Expected:

- Status: `401 Unauthorized`
- Payload:

```json
{
  "code": "UNAUTHORIZED",
  "message": "Authentication is required."
}
```

### Admin/Sensitive Endpoint as Employee

Login as normal employee:

```http
GET /api/audit-logs?page=1&pageSize=25
```

Expected: `403 Forbidden`.

### Duplicate Employee Number

Login as admin and call:

```http
POST /api/employees
```

Use `employeeNumber` already used by another employee:

```json
{
  "employeeNumber": "EMP004",
  "fullName": "Duplicate Employee",
  "email": "duplicate.employee@test.com",
  "departmentId": "<ENGINEERING_DEPARTMENT_ID>",
  "managerId": "<MANAGER_EMPLOYEE_ID>",
  "birthDate": "1997-01-01",
  "joinDate": "2026-06-01",
  "jobTitle": "Tester",
  "phoneNumber": "+201000000099",
  "notes": "Duplicate employee number test",
  "status": "Active",
  "initialPassword": "<EMPLOYEE_PASSWORD>"
}
```

Expected: `409 Conflict`.

### Duplicate Active Email

Use a unique employee number but an email already used by an active employee.

Expected: `409 Conflict`.

### Vacation Overlap

Create a vacation request overlapping an existing pending or approved request.

Expected: `422 Unprocessable Entity`.

### Vacation Notice Rule

Create a vacation request too close to today.

Expected: `422 Unprocessable Entity`.

### Document Oversized

Upload a file larger than the configured max.

Expected:

- `413 Payload Too Large`
- `{ "code": "PAYLOAD_TOO_LARGE", "message": "..." }`

### Removed Document Access

Download a removed document:

```http
GET /api/employees/<EMPLOYEE_ID>/documents/<DOCUMENT_ID>
```

Expected: `404 Not Found`.

### Compensation as Normal Employee

```http
GET /api/employees/<EMPLOYEE_ID>/compensation
```

Expected: `403 Forbidden`.

### Terminated or Soft-Deleted Auth

Terminate or delete an employee, then try to login as that employee.

Expected:

- Login rejected with `401 Unauthorized`, or existing session becomes invalid on validation.

## 21. Full Recommended Testing Order

1. Apply migrations to the local SQL Server database.
2. Create one bootstrap department row in SQL and copy its `Id`.
3. Configure initial admin bootstrap environment variables.
4. Run API with HTTPS launch profile.
5. Open Swagger at `https://localhost:7162/swagger`.
6. Login as System Admin through `POST /api/auth/login`.
7. Confirm `/api/auth/me` returns `SystemAdministrator`.
8. Disable bootstrap and restart the API.
9. Create HR department.
10. Create Engineering department.
11. Create HR Admin employee.
12. Create Manager employee.
13. Create Normal Employee under the Manager.
14. Assign `HRAdministrator` role.
15. Assign `Manager` role.
16. Test login per role.
17. Test allowed and forbidden role access:
    - Employee: `403` on `GET /api/employees`, self-only detail
    - Manager: team-only `GET /api/employees`, `403` on outside-scope detail
    - HR Admin: org-wide employee access, `403` on role assignment
    - System Admin: full access, role assignment
18. Test attendance clock-in, duplicate clock-in, and clock-out.
19. Test vacation create (employee self, HR creates for employee), approve/reject (manager team, HR org-wide), same-status no-op, invalid transition, overlap, self-review blocked.
20. Test trip create (employee self, manager team, HR for employee), list scope (employee own-only, manager own+team, HR org-wide), get/detail scope, validation failures (past date, non-working day), delete scope.
21. Test compensation update, view, history, no-change update, and forbidden access.
22. Test document upload, list, download, remove, removed download, oversized file, and invalid type.
23. Test dashboard as Manager and HR Admin.
24. Test audit logs and filters.
25. Run final negative/security checks.

## 22. Common Problems

### 401 Unauthorized

Usually means there is no valid login cookie.

Check:

- Did you call `POST /api/auth/login` first?
- Are you using HTTPS so the secure cookie is sent?
- Did you clear cookies or switch browser profiles?
- Did the employee become terminated or soft-deleted?

### 403 Forbidden

Usually means login worked but the role/scope is not allowed.

Examples:

- Employee calling `GET /api/employees` (list).
- Employee calling another employee detail outside self.
- Manager calling employee detail for an outside-team employee.
- Employee calling compensation endpoint.
- Manager calling audit logs.
- HR Admin trying to assign roles.
- Employee creating a trip for another employee.

Note: Some out-of-scope scenarios return `404 Not Found` instead of `403` where the current contract requires non-disclosure. Either response is valid depending on the specific endpoint and context.

### 400 Validation Error

Usually means the JSON body does not match the DTO or required fields are missing.

Check:

- Login uses `identifier`, not `email`.
- Department create uses only `name`.
- Employee create requires `employeeNumber`, `fullName`, `email`, and `departmentId`.
- DateOnly values must look like `YYYY-MM-DD`.

### 409 Conflict

Usually means the request conflicts with existing state.

Examples:

- duplicate employee number
- duplicate active employee email
- duplicate attendance clock-in for the same business date

### 413 Payload Too Large

The document file is larger than `DocumentStorage:MaxFileSizeBytes`.

Current default:

```text
10485760 bytes
```

### 422 Business Rule Error

The JSON shape is valid, but the domain rule rejects it.

Examples:

- vacation starts in the past
- vacation does not have three full working days of notice
- vacation overlaps pending/approved request
- vacation self-review
- trip date is in the past or not a working day
- compensation salary is negative
- document extension is not allowed

### Bootstrap Did Not Create Admin

Check:

- No active `SystemAdministrator` already exists.
- `InitialAdminBootstrap__Enabled` is `true`.
- `InitialAdminBootstrap__Mode` is `CreateInitialAdmin`.
- `InitialAdminBootstrap__DepartmentId` points to an existing department.
- Email and employee number are unique.
- Temporary password satisfies Identity password rules.
- SQL Server connection string points to the database you are checking.

### Cannot Use Swagger Auth

This project uses cookie auth, not bearer token auth.

Use this flow:

1. Open Swagger over HTTPS.
2. Call `/api/auth/login`.
3. Confirm the browser receives the cookie.
4. Call protected endpoints from the same Swagger tab.

If still failing, use Postman with cookie jar enabled.

### Database Has Old Test Data

Old data can cause duplicates and lifecycle confusion.

Options:

- Use a fresh database name in the connection string.
- Delete only your own test rows carefully.
- Use unique employee numbers and emails for each run, such as `EMP101`, `EMP102`, etc.

## 23. Copy-Paste Test Run Sheet

Use this sheet during a complete manual run.

Setup:

- [ ] SQL Server running
- [ ] migrations applied
- [ ] bootstrap department created
- [ ] bootstrap environment variables set
- [ ] API running over HTTPS
- [ ] Swagger open

Authentication and core data:

- [ ] Login as System Admin
- [ ] Copy `<SYSTEM_ADMIN_EMPLOYEE_ID>`
- [ ] Disable bootstrap and restart API
- [ ] Create HR department
- [ ] Copy `<HR_DEPARTMENT_ID>`
- [ ] Create Engineering department
- [ ] Copy `<ENGINEERING_DEPARTMENT_ID>`
- [ ] Create HR Admin
- [ ] Copy `<HR_ADMIN_EMPLOYEE_ID>`
- [ ] Create Manager
- [ ] Copy `<MANAGER_EMPLOYEE_ID>`
- [ ] Create Employee
- [ ] Copy `<EMPLOYEE_ID>`
- [ ] Assign HR Admin role
- [ ] Assign Manager role

Role testing:

- [ ] Login as HR Admin
- [ ] Login as Manager
- [ ] Login as Employee
- [ ] Confirm Employee `GET /api/employees` returns `403`
- [ ] Confirm Employee cannot view another employee detail: `403`
- [ ] Confirm Manager team list excludes outside-scope employees
- [ ] Confirm Manager cannot view outside-team employee detail: `403`
- [ ] Confirm Employee cannot access compensation: `403`
- [ ] Confirm Manager cannot access audit logs: `403`
- [ ] Confirm HR Admin cannot assign roles: `403`
- [ ] Confirm HR Admin can access org-wide employee list: `200`
- [ ] Confirm Employee vacation list shows only own requests
- [ ] Confirm Manager vacation review is team-only
- [ ] Confirm Employee trip list shows only own trips
- [ ] Confirm Manager trip list includes own + team trips
- [ ] Confirm self-review blocked for all roles on vacation endpoints: `422`

Lifecycle modules:

- [ ] Check-in
- [ ] Duplicate check-in returns `409`
- [ ] Check-out
- [ ] Submit vacation
- [ ] Review vacation
- [ ] Test vacation invalid transition
- [ ] Create trip
- [ ] Query trip
- [ ] Add compensation
- [ ] View compensation history
- [ ] Confirm employee DTO excludes compensation fields
- [ ] Upload document
- [ ] Download document
- [ ] Remove document
- [ ] Confirm removed document download fails
- [ ] View Manager dashboard
- [ ] View HR Admin dashboard
- [ ] Query audit logs

Final negative checks:

- [ ] Protected endpoint without login returns `401`
- [ ] Sensitive endpoint with wrong role returns `403`
- [ ] Duplicate employee number returns `409`
- [ ] Vacation overlap returns `422`
- [ ] Oversized document returns `413`
- [ ] Invalid document type returns `422`

## Endpoints and Shapes to Verify From Swagger

The guide uses routes and DTOs from the current source code. Verify these in Swagger if the API changes:

- `POST /api/employees/{employeeId}/documents` multipart form rendering for `category` and `file`.
- Casing of controller-token routes such as `/api/vacationrequests`; ASP.NET routing is case-insensitive, but Swagger shows the generated route.
- Controller-level authorization for departments is authenticated access. Vacation request endpoints use role-specific authorization (Phase 10): Employee own-only list/detail/create/delete; Manager team-only list (excludes self by default), self filter available, self/team detail, self-only create/delete, team-only review (self-review forbidden); HR/System organization-wide list/detail/create/delete/review (self-review forbidden). Employee endpoints use role-specific authorization (Phase 9): Employee/Manager receive `403` for list/create/update/delete, Employee self-only for detail, Manager team-scoped list, HR/System wide access. Trip endpoints use role-specific authorization (Phase 11): Employee own-only list/detail/create/delete; Manager own plus active direct/indirect team list/detail/create/delete; HR/System organization-wide list/detail/create/delete. Verify role expectations from the Phase 9, Phase 10, and Phase 11 access matrices.
- Trip approval/rejection endpoints do not exist in the current API.
- Compensation has only `GET` and `PUT`; `PUT` creates or updates compensation.
