# Phase 9 Manual Local DB Test Guide

Use this guide to manually verify Phase 9 employee access scope hardening on your local SQL Server database.

This is narrower than [API_LIFECYCLE_TESTING_GUIDE.md](../../API_LIFECYCLE_TESTING_GUIDE.md). It focuses only on employee access behavior introduced by Phase 9.

## What You Are Verifying

Phase 9 hardens these employee endpoints:

- `GET /api/employees`
- `GET /api/employees/{id}`
- `POST /api/employees`
- `PUT /api/employees/{id}`
- `DELETE /api/employees/{id}`
- `PUT /api/employees/{id}/role`

Expected access matrix:

| Operation | Employee | Manager | HRAdministrator | SystemAdministrator |
|-----------|----------|---------|-----------------|---------------------|
| `GET /api/employees` | `403` | active direct + indirect reports only | org-wide including terminated + soft-deleted | org-wide including terminated + soft-deleted |
| `GET /api/employees/{id}` | self only | self + active direct/indirect reports | any existing employee | any existing employee |
| `POST /api/employees` | `403` | `403` | allowed | allowed |
| `PUT /api/employees/{id}` | `403` | `403` | non-`SystemAdministrator` targets only | allowed, except last-active-admin removal |
| `DELETE /api/employees/{id}` | `403` | `403` | non-`SystemAdministrator` targets only | allowed, except last-active-admin removal |
| `PUT /api/employees/{id}/role` | `403` | `403` | `403` | allowed, except last-active-admin demotion |

## Prerequisites

- SQL Server is running.
- Your API points to the intended local database.
- Existing migrations are applied.
- You will test through Swagger or Postman with cookies enabled.
- Prefer HTTPS because auth cookies are configured as secure.

Useful commands:

```powershell
dotnet restore .\HR.slnx
dotnet build .\HR.slnx -c Release
dotnet ef database update --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
dotnet run --project .\HR.API\HR.API.csproj --launch-profile https
```

Swagger URL:

```text
https://localhost:7162/swagger
```

## Test Data You Should Create

Keep these placeholders as you go:

| Placeholder | Purpose |
|-------------|---------|
| `<BOOTSTRAP_DEPARTMENT_ID>` | department used only for initial admin bootstrap |
| `<HR_DEPARTMENT_ID>` | Human Resources department |
| `<ENGINEERING_DEPARTMENT_ID>` | Engineering department |
| `<SYSTEM_ADMIN_EMPLOYEE_ID>` | initial bootstrap admin |
| `<HR_ADMIN_EMPLOYEE_ID>` | HR admin employee |
| `<MANAGER_EMPLOYEE_ID>` | manager employee |
| `<DIRECT_REPORT_ID>` | active direct report under manager |
| `<INDIRECT_REPORT_ID>` | active indirect report under manager |
| `<UNRELATED_EMPLOYEE_ID>` | active employee outside manager scope |
| `<TERMINATED_REPORT_ID>` | terminated employee under manager tree |
| `<DELETED_REPORT_ID>` | soft-deleted employee under manager tree |
| `<SECOND_SYS_ADMIN_ID>` | second active system admin for non-last-admin checks |

Suggested local test passwords:

```text
<TEMPORARY_STRONG_PASSWORD>
<HR_ADMIN_PASSWORD>
<MANAGER_PASSWORD>
<EMPLOYEE_PASSWORD>
<SECOND_SYS_ADMIN_PASSWORD>
```

## Step 1 - Bootstrap the First System Admin

If your database is empty, follow the bootstrap flow from [API_LIFECYCLE_TESTING_GUIDE.md](../../API_LIFECYCLE_TESTING_GUIDE.md).

Minimum bootstrap setup:

1. Insert one department row in SQL:

```sql
DECLARE @DepartmentId uniqueidentifier = NEWID();

INSERT INTO Departments (Id, Name)
VALUES (@DepartmentId, N'Administration');

SELECT @DepartmentId AS BootstrapDepartmentId;
```

2. Save the returned ID as `<BOOTSTRAP_DEPARTMENT_ID>`.
3. Set bootstrap env vars:

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

4. Start the API.
5. Login with:

```json
{
  "identifier": "admin@test.com",
  "password": "<TEMPORARY_STRONG_PASSWORD>"
}
```

6. Save the returned employee ID as `<SYSTEM_ADMIN_EMPLOYEE_ID>`.
7. Disable bootstrap after the first admin works.

## Step 2 - Create Core Departments

Login as `admin@test.com`.

Create HR:

```http
POST /api/departments
```

```json
{
  "name": "Human Resources"
}
```

Save returned `id` as `<HR_DEPARTMENT_ID>`.

Create Engineering:

```http
POST /api/departments
```

```json
{
  "name": "Engineering"
}
```

Save returned `id` as `<ENGINEERING_DEPARTMENT_ID>`.

## Step 3 - Create the Employee Graph

Still logged in as system admin, create these employees with `POST /api/employees`.

### 3.1 HR Admin

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
  "notes": "Phase 9 manual test HR admin",
  "status": "Active",
  "initialPassword": "<HR_ADMIN_PASSWORD>"
}
```

Save `employee.id` as `<HR_ADMIN_EMPLOYEE_ID>`.

### 3.2 Manager

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
  "notes": "Phase 9 manual test manager",
  "status": "Active",
  "initialPassword": "<MANAGER_PASSWORD>"
}
```

Save `employee.id` as `<MANAGER_EMPLOYEE_ID>`.

### 3.3 Direct Report

```json
{
  "employeeNumber": "EMP004",
  "fullName": "Direct Report",
  "email": "employee@test.com",
  "departmentId": "<ENGINEERING_DEPARTMENT_ID>",
  "managerId": "<MANAGER_EMPLOYEE_ID>",
  "birthDate": "1996-03-10",
  "joinDate": "2026-06-01",
  "jobTitle": "Software Engineer",
  "phoneNumber": "+201000000004",
  "notes": "Phase 9 direct report",
  "status": "Active",
  "initialPassword": "<EMPLOYEE_PASSWORD>"
}
```

Save `employee.id` as `<DIRECT_REPORT_ID>`.

### 3.4 Indirect Report

```json
{
  "employeeNumber": "EMP005",
  "fullName": "Indirect Report",
  "email": "indirect@test.com",
  "departmentId": "<ENGINEERING_DEPARTMENT_ID>",
  "managerId": "<DIRECT_REPORT_ID>",
  "birthDate": "1997-04-10",
  "joinDate": "2026-06-01",
  "jobTitle": "Junior Engineer",
  "phoneNumber": "+201000000005",
  "notes": "Phase 9 indirect report",
  "status": "Active",
  "initialPassword": "IndirectPass1"
}
```

Save `employee.id` as `<INDIRECT_REPORT_ID>`.

### 3.5 Unrelated Active Employee

```json
{
  "employeeNumber": "EMP006",
  "fullName": "Unrelated Employee",
  "email": "other@test.com",
  "departmentId": "<HR_DEPARTMENT_ID>",
  "managerId": null,
  "birthDate": "1994-05-10",
  "joinDate": "2026-06-01",
  "jobTitle": "HR Specialist",
  "phoneNumber": "+201000000006",
  "notes": "Phase 9 unrelated employee",
  "status": "Active",
  "initialPassword": "OtherPass1"
}
```

Save `employee.id` as `<UNRELATED_EMPLOYEE_ID>`.

### 3.6 Employee That Will Be Terminated

Create it active first:

```json
{
  "employeeNumber": "EMP007",
  "fullName": "Terminated Report",
  "email": "terminated@test.com",
  "departmentId": "<ENGINEERING_DEPARTMENT_ID>",
  "managerId": "<MANAGER_EMPLOYEE_ID>",
  "birthDate": "1995-06-10",
  "joinDate": "2026-06-01",
  "jobTitle": "QA Engineer",
  "phoneNumber": "+201000000007",
  "notes": "Phase 9 terminated report seed",
  "status": "Active",
  "initialPassword": "TerminatedPass1"
}
```

Save `employee.id` as `<TERMINATED_REPORT_ID>`.

Then terminate it:

```http
PUT /api/employees/<TERMINATED_REPORT_ID>
```

```json
{
  "fullName": "Terminated Report",
  "email": "terminated@test.com",
  "departmentId": "<ENGINEERING_DEPARTMENT_ID>",
  "managerId": "<MANAGER_EMPLOYEE_ID>",
  "birthDate": "1995-06-10",
  "joinDate": "2026-06-01",
  "jobTitle": "QA Engineer",
  "phoneNumber": "+201000000007",
  "notes": "Phase 9 terminated report",
  "status": "Terminated"
}
```

Expected: `200 OK`.

### 3.7 Employee That Will Be Soft Deleted

Create it active first:

```json
{
  "employeeNumber": "EMP008",
  "fullName": "Deleted Report",
  "email": "deleted@test.com",
  "departmentId": "<ENGINEERING_DEPARTMENT_ID>",
  "managerId": "<MANAGER_EMPLOYEE_ID>",
  "birthDate": "1993-07-10",
  "joinDate": "2026-06-01",
  "jobTitle": "Support Engineer",
  "phoneNumber": "+201000000008",
  "notes": "Phase 9 deleted report seed",
  "status": "Active",
  "initialPassword": "DeletedPass1"
}
```

Save `employee.id` as `<DELETED_REPORT_ID>`.

Then soft delete it:

```http
DELETE /api/employees/<DELETED_REPORT_ID>
```

Expected: `204 No Content`.

### 3.8 Second System Admin

Create:

```json
{
  "employeeNumber": "EMP009",
  "fullName": "Second System Admin",
  "email": "admin2@test.com",
  "departmentId": "<HR_DEPARTMENT_ID>",
  "managerId": null,
  "birthDate": "1991-08-10",
  "joinDate": "2026-06-01",
  "jobTitle": "Operations Admin",
  "phoneNumber": "+201000000009",
  "notes": "Phase 9 second admin",
  "status": "Active",
  "initialPassword": "<SECOND_SYS_ADMIN_PASSWORD>"
}
```

Save `employee.id` as `<SECOND_SYS_ADMIN_ID>`.

## Step 4 - Assign Roles

Still logged in as `admin@test.com`:

Assign HR admin role:

```http
PUT /api/employees/<HR_ADMIN_EMPLOYEE_ID>/role
```

```json
{
  "role": "HRAdministrator"
}
```

Assign manager role:

```http
PUT /api/employees/<MANAGER_EMPLOYEE_ID>/role
```

```json
{
  "role": "Manager"
}
```

Assign second system admin role:

```http
PUT /api/employees/<SECOND_SYS_ADMIN_ID>/role
```

```json
{
  "role": "SystemAdministrator"
}
```

Expected for all three: `200 OK`.

## Step 5 - Run the Manual Access Checks

Always logout before switching users:

```http
POST /api/auth/logout
```

Expected: `204 No Content`.

Common forbidden payload:

```json
{
  "code": "FORBIDDEN",
  "message": "Forbidden"
}
```

### 5.1 Normal Employee Checks

Login as `employee@test.com`.

1. `GET /api/employees?page=1&pageSize=25`
   Expected: `403 Forbidden`
2. `GET /api/employees/<DIRECT_REPORT_ID>`
   Expected: `200 OK`
3. `GET /api/employees/<MANAGER_EMPLOYEE_ID>`
   Expected: `403 Forbidden`
4. `GET /api/employees/<UNRELATED_EMPLOYEE_ID>`
   Expected: `403 Forbidden`
5. `POST /api/employees`
   Expected: `403 Forbidden`
6. `PUT /api/employees/<UNRELATED_EMPLOYEE_ID>`
   Expected: `403 Forbidden`
7. `DELETE /api/employees/<UNRELATED_EMPLOYEE_ID>`
   Expected: `403 Forbidden`
8. `PUT /api/employees/<DIRECT_REPORT_ID>/role`
   Expected: `403 Forbidden`

### 5.2 Manager Checks

Login as `manager@test.com`.

1. `GET /api/employees?page=1&pageSize=25`
   Expected items include `<DIRECT_REPORT_ID>` and `<INDIRECT_REPORT_ID>` only from the team scope
2. Confirm the list does not include:
   `<MANAGER_EMPLOYEE_ID>`, `<UNRELATED_EMPLOYEE_ID>`, `<TERMINATED_REPORT_ID>`, `<DELETED_REPORT_ID>`
3. `GET /api/employees/<MANAGER_EMPLOYEE_ID>`
   Expected: `200 OK`
4. `GET /api/employees/<DIRECT_REPORT_ID>`
   Expected: `200 OK`
5. `GET /api/employees/<INDIRECT_REPORT_ID>`
   Expected: `200 OK`
6. `GET /api/employees/<UNRELATED_EMPLOYEE_ID>`
   Expected: `403 Forbidden`
7. `GET /api/employees/<TERMINATED_REPORT_ID>`
   Expected: `403 Forbidden`
8. `GET /api/employees/<DELETED_REPORT_ID>`
   Expected: `403 Forbidden`
9. `POST /api/employees`
   Expected: `403 Forbidden`
10. `PUT /api/employees/<UNRELATED_EMPLOYEE_ID>`
    Expected: `403 Forbidden`
11. `DELETE /api/employees/<UNRELATED_EMPLOYEE_ID>`
    Expected: `403 Forbidden`

### 5.3 HR Admin Checks

Login as `hr.admin@test.com`.

1. `GET /api/employees?page=1&pageSize=100`
   Expected: org-wide list including `<TERMINATED_REPORT_ID>` and `<DELETED_REPORT_ID>`
2. `GET /api/employees?status=Terminated&page=1&pageSize=25`
   Expected: returns terminated employees within org scope
3. `GET /api/employees/<TERMINATED_REPORT_ID>`
   Expected: `200 OK`
4. `GET /api/employees/<DELETED_REPORT_ID>`
   Expected: `200 OK`
5. Update a non-system-admin target, for example:

```http
PUT /api/employees/<UNRELATED_EMPLOYEE_ID>
```

```json
{
  "fullName": "Unrelated Employee Updated By HR",
  "email": "other@test.com",
  "departmentId": "<HR_DEPARTMENT_ID>",
  "managerId": null,
  "birthDate": "1994-05-10",
  "joinDate": "2026-06-01",
  "jobTitle": "HR Specialist",
  "phoneNumber": "+201000000006",
  "notes": "Updated by HR during Phase 9 test",
  "status": "Active"
}
```

Expected: `200 OK`

6. `PUT /api/employees/<SYSTEM_ADMIN_EMPLOYEE_ID>`
   Expected: `403 Forbidden`
7. `DELETE /api/employees/<SYSTEM_ADMIN_EMPLOYEE_ID>`
   Expected: `403 Forbidden`
8. `PUT /api/employees/<UNRELATED_EMPLOYEE_ID>/role`
   Expected: `403 Forbidden`

Optional positive delete:

9. `DELETE /api/employees/<UNRELATED_EMPLOYEE_ID>`
   Expected: `204 No Content`

If you want to keep that user for later checks, skip the delete.

### 5.4 System Admin Checks

Login as `admin@test.com`.

1. `GET /api/employees?page=1&pageSize=100`
   Expected: org-wide list including active, terminated, and soft-deleted records
2. `GET /api/employees/<TERMINATED_REPORT_ID>`
   Expected: `200 OK`
3. `GET /api/employees/<DELETED_REPORT_ID>`
   Expected: `200 OK`
4. Update the second system admin:

```http
PUT /api/employees/<SECOND_SYS_ADMIN_ID>
```

```json
{
  "fullName": "Second System Admin Updated",
  "email": "admin2@test.com",
  "departmentId": "<HR_DEPARTMENT_ID>",
  "managerId": null,
  "birthDate": "1991-08-10",
  "joinDate": "2026-06-01",
  "jobTitle": "Operations Admin",
  "phoneNumber": "+201000000009",
  "notes": "Updated by system admin during Phase 9 test",
  "status": "Active"
}
```

Expected: `200 OK`

5. Delete the second system admin while two active admins still exist:

```http
DELETE /api/employees/<SECOND_SYS_ADMIN_ID>
```

Expected: `204 No Content`

6. Try to suspend the only remaining active system admin:

```http
PUT /api/employees/<SYSTEM_ADMIN_EMPLOYEE_ID>
```

```json
{
  "fullName": "System Admin",
  "email": "admin@test.com",
  "departmentId": "<BOOTSTRAP_DEPARTMENT_ID>",
  "managerId": null,
  "birthDate": null,
  "joinDate": null,
  "jobTitle": null,
  "phoneNumber": null,
  "notes": "Phase 9 last-admin status test",
  "status": "Suspended"
}
```

Expected: `422 Unprocessable Entity`

Expected code/message:

```json
{
  "code": "BUSINESS_RULE_VIOLATION",
  "message": "Cannot remove the last active SystemAdministrator."
}
```

7. Try to demote the only remaining active system admin:

```http
PUT /api/employees/<SYSTEM_ADMIN_EMPLOYEE_ID>/role
```

```json
{
  "role": "Employee"
}
```

Expected: `422 Unprocessable Entity`

Expected code/message:

```json
{
  "code": "BUSINESS_RULE_VIOLATION",
  "message": "Cannot demote the last active SystemAdministrator."
}
```

8. Try to delete the only remaining active system admin:

```http
DELETE /api/employees/<SYSTEM_ADMIN_EMPLOYEE_ID>
```

Expected: `422 Unprocessable Entity`

Expected code/message:

```json
{
  "code": "BUSINESS_RULE_VIOLATION",
  "message": "Cannot remove the last active SystemAdministrator."
}
```

## Step 6 - Spot-Check Compatibility

Phase 9 should not change these:

1. `POST /api/auth/login` still returns the same wrapper and cookie behavior.
2. `GET /api/auth/me` still works after login.
3. Successful employee detail/list response shapes are unchanged.
4. Paged list response still has:

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

5. Vacation and trip endpoints should behave exactly as before Phase 9.

## Expected Result Summary

You can treat Phase 9 as manually verified if all of these are true:

- Employee list is forbidden for normal employees.
- Manager list shows only active direct and indirect reports.
- Manager detail allows self and active team only.
- HR and System Admin list/detail include terminated and soft-deleted employees.
- Employee and Manager write attempts are blocked with `403`.
- HR can manage non-system-admin employees, but cannot update/delete a `SystemAdministrator`.
- Only system admins can change roles.
- Last-active-system-admin demotion, suspension/termination, and deletion are blocked before mutation.
- No route shape, cookie flow, or pagination contract changed.
