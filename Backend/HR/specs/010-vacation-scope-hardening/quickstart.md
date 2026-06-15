# Quickstart: Phase 10 Vacation Request Scope Hardening

Use this guide after Phase 10 implementation to validate vacation request access locally.

## Prerequisites

- Phase 8 authorization scope foundation is complete.
- Phase 9 employee access scope hardening is complete.
- Local SQL Server database is migrated with existing approved migrations.
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
- There is one peer manager or peer employee.
- There is one terminated report.
- There is one soft-deleted report.
- Vacation requests exist for the manager, direct report, indirect report, unrelated employee, terminated employee, and soft-deleted employee.
- Include pending, approved, and rejected vacation requests for delete/review checks.

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
- EF reports no pending model changes unless the creator-tracking migration was separately approved and applied
- no new migration files exist unless separately approved

## Manual Smoke Checks

Use Swagger or Postman with cookie authentication. Prefer HTTPS because cookies are configured as secure.

### Normal Employee

1. Login as `employee@test.com`.
2. Call `GET /api/vacationrequests?page=1&pageSize=25`.
3. Expected: only vacation requests owned by the signed-in employee.
4. Call `GET /api/vacationrequests?employeeId={anotherEmployeeId}`.
5. Expected: empty scoped page.
6. Call `GET /api/vacationrequests/{ownRequestId}`.
7. Expected: `200 OK`.
8. Call `GET /api/vacationrequests/{anotherEmployeeRequestId}`.
9. Expected: `403 Forbidden`.
10. Call `POST /api/vacationrequests` with own `employeeId`.
11. Expected: success if existing date, notice, overlap, and balance rules pass.
12. Call `POST /api/vacationrequests` with another employee ID.
13. Expected: `403 Forbidden`.
14. Call `PUT /api/vacationrequests/{anyRequestId}/status`.
15. Expected: `403 Forbidden`.
16. Delete own pending request.
17. Expected: `204 No Content`.
18. Delete another employee's request.
19. Expected: `403 Forbidden`.

### Manager

1. Login as `manager@test.com`.
2. Call `GET /api/vacationrequests?page=1&pageSize=25`.
3. Expected: only active direct and indirect report vacation requests; manager-owned requests are not included.
4. Call `GET /api/vacationrequests?employeeId={managerEmployeeId}`.
5. Expected: only manager-owned vacation requests.
6. Call `GET /api/vacationrequests?employeeId={directReportId}`.
7. Expected: only direct report vacation requests.
8. Call `GET /api/vacationrequests?employeeId={unrelatedEmployeeId}`.
9. Expected: empty scoped page.
10. Call `GET /api/vacationrequests/{managerOwnRequestId}`.
11. Expected: `200 OK`.
12. Call `GET /api/vacationrequests/{directOrIndirectReportRequestId}`.
13. Expected: `200 OK`.
14. Call `GET /api/vacationrequests/{unrelatedRequestId}`.
15. Expected: `403 Forbidden`.
16. Create a vacation request for self.
17. Expected: success if existing business rules pass.
18. Create a vacation request for a team member.
19. Expected: `403 Forbidden`.
20. Approve or reject a team member pending request.
21. Expected: success if existing status rules pass.
22. Approve or reject manager's own request.
23. Expected: self-review rejected.
24. Delete own pending request.
25. Expected: `204 No Content`.
26. Delete team member pending request.
27. Expected: `403 Forbidden`.

### HR Administrator

1. Login as `hr.admin@test.com`.
2. Call `GET /api/vacationrequests?page=1&pageSize=25`.
3. Expected: organization-wide vacation requests.
4. Call `GET /api/vacationrequests/{anyRequestId}`.
5. Expected: `200 OK`.
6. Create a vacation request for an active employee.
7. Expected: success if existing business rules pass.
8. Create a vacation request for inactive, soft-deleted, or terminated employee.
9. Expected: existing business-rule failure.
10. Approve or reject another employee's request.
11. Expected: success if existing status rules pass.
12. Approve or reject HR administrator's own request.
13. Expected: self-review rejected.
14. Delete any pending request.
15. Expected: `204 No Content`.
16. Delete approved or rejected request.
17. Expected: existing pending-only business-rule failure.

### System Administrator

1. Login as `admin@test.com`.
2. Repeat HR administrator list, detail, create, review, and delete checks.
3. Expected: same organization-wide vacation behavior, with self-review still forbidden.

## Compatibility Checks

Confirm these did not change:

- login route and response wrapper
- `/api/auth/me`
- cookie behavior
- existing vacation request success DTO fields
- pagination envelope shape
- employee endpoints
- trip endpoints
- compensation, document, dashboard, attendance, audit, and bootstrap behavior

## Static Checks

```powershell
rg -n "HR.Infrastructure" .\HR.Application
git status --short .\HR.Infrastructure\Data\Migrations
git diff --check
```

Expected:

- `HR.Application` does not reference `HR.Infrastructure`
- no migration files were created unless separately approved
- no whitespace errors
