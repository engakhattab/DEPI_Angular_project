# Quickstart: Phase 9 Employee Access Scope Hardening

Use this guide after Phase 9 implementation to validate the employee access behavior locally.

## Prerequisites

- Phase 8 authorization scope foundation is complete.
- Local SQL Server database is migrated with existing migrations.
- API can start successfully.
- Test users exist:

| Role | Email |
|------|-------|
| SystemAdministrator | `admin@test.com` |
| HRAdministrator | `hr.admin@test.com` |
| Manager | `manager@test.com` |
| Employee | `employee@test.com` |

Recommended hierarchy:

- Manager has one direct active report.
- That direct report has one indirect active report.
- There is one unrelated active employee.
- There is one terminated employee.
- There is one soft-deleted employee.
- There are at least two active `SystemAdministrator` employees when validating allowed demotion, and a separate case with exactly one active `SystemAdministrator` when validating last-active rejection.

## Automated Validation

Run from repository root:

```powershell
dotnet restore .\HR.slnx
dotnet build .\HR.slnx -c Release
dotnet test .\HR.slnx -c Release --no-build
dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

Expected:

- restore passes
- build passes
- tests pass
- EF reports no pending model changes
- no new migration files exist under `HR.Infrastructure/Data/Migrations`

## Manual Smoke Checks

Use Swagger or Postman with cookie authentication. Prefer HTTPS because cookies are configured as secure.

### Normal Employee

1. Login as `employee@test.com`.
2. Call `GET /api/employees?page=1&pageSize=25`.
3. Expected: `403 Forbidden`.
4. Call `GET /api/auth/me`.
5. Expected: `200 OK`.
6. Call `GET /api/employees/{ownEmployeeId}`.
7. Expected: `200 OK`.
8. Call `GET /api/employees/{anotherEmployeeId}`.
9. Expected: `403 Forbidden`.
10. Try `POST`, `PUT`, and `DELETE /api/employees`.
11. Expected: `403 Forbidden`.

### Manager

1. Login as `manager@test.com`.
2. Call `GET /api/employees?page=1&pageSize=25`.
3. Expected: only active direct and indirect reports appear.
4. Confirm manager self is not included in the list.
5. Call `GET /api/employees/{managerEmployeeId}`.
6. Expected: `200 OK`.
7. Call `GET /api/employees/{directReportId}` and `{indirectReportId}`.
8. Expected: `200 OK`.
9. Call `GET /api/employees/{peerOrUnrelatedEmployeeId}`.
10. Expected: `403 Forbidden`.
11. Try `POST`, `PUT`, and `DELETE /api/employees`.
12. Expected: `403 Forbidden`.

### HR Administrator

1. Login as `hr.admin@test.com`.
2. Call `GET /api/employees?page=1&pageSize=25`.
3. Expected: organization-wide employee records, including terminated and soft-deleted records.
4. Call `GET /api/employees/{terminatedEmployeeId}` and `{softDeletedEmployeeId}`.
5. Expected: `200 OK`.
6. Create a non-system-administrator employee.
7. Expected: success if existing business rules pass.
8. Update or delete a normal employee.
9. Expected: success if existing business rules pass.
10. Update or delete a `SystemAdministrator` employee.
11. Expected: `403 Forbidden`.
12. Try `PUT /api/employees/{id}/role`.
13. Expected: `403 Forbidden`.

### System Administrator

1. Login as `admin@test.com`.
2. Call `GET /api/employees?page=1&pageSize=25`.
3. Expected: organization-wide employee records, including terminated and soft-deleted records.
4. Update or delete a non-last active `SystemAdministrator` employee.
5. Expected: success if existing business rules pass.
6. Try to delete, terminate, or suspend the only active `SystemAdministrator`.
7. Expected: operation rejected before mutation.
8. Call `PUT /api/employees/{onlyActiveSystemAdministratorId}/role` with `{ "role": "Employee" }`.
9. Expected: operation rejected before mutation using the existing business-rule/conflict error convention.
10. With at least two active `SystemAdministrator` employees, call `PUT /api/employees/{oneSystemAdministratorId}/role` with a non-`SystemAdministrator` role.
11. Expected: allowed only if all existing role-assignment rules pass and at least one other active `SystemAdministrator` remains.

## Compatibility Checks

Confirm these did not change:

- login route and response wrapper
- `/api/auth/me`
- cookie behavior
- existing employee success DTO fields
- pagination envelope shape
- vacation request endpoints
- trip endpoints
- compensation, document, dashboard, audit, and bootstrap behavior

## Static Checks

```powershell
rg -n "HR.Infrastructure" .\HR.Application
git status --short .\HR.Infrastructure\Data\Migrations
git diff --check
```

Expected:

- `HR.Application` does not reference `HR.Infrastructure`
- no migration files were created
- no whitespace errors
