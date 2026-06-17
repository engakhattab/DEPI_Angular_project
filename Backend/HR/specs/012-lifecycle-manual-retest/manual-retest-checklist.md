# Phase 12 Manual Retest Checklist

**Database**: `HrSystemDb_Phase12LifecycleTest`
**Connection source**: Environment variable (local SQL Server)
**API URL**: `https://localhost:7162`
**Test tool**: Swagger UI (cookie-preserving)

## Setup and Baseline

| ID | Module | Setup/Action | Expected | Actual | Result | Notes |
|----|--------|-------------|----------|--------|--------|-------|
| CMD-001 | Setup | `dotnet restore .\HR.slnx` | Exit code 0 | Exit code 0 | Pass | |
| CMD-002 | Setup | `dotnet build .\HR.slnx -c Release -p:UseSharedCompilation=false` | Build succeeded | Build succeeded | Pass | |
| CMD-003 | Setup | `dotnet test .\HR.slnx -c Release` | All tests pass | All tests pass | Pass | |
| CMD-004 | Setup | `dotnet ef database update --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj` | Applied | Applied | Pass | |
| CMD-005 | Setup | `dotnet ef migrations list --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj` | 5 approved migrations | 5 approved migrations | Pass | |
| CMD-006 | Setup | `dotnet ef migrations has-pending-model-changes ...` | No pending changes | No pending changes | Pass | |
| CMD-007 | Setup | `git diff --check` | No whitespace errors | No whitespace errors | Pass | |
| CMD-008 | Setup | `git status --short` | Only doc/spec artifacts changed | Only doc/spec artifacts changed | Pass | |

## AUTH Scenarios

| ID | Module | Actor | Request/Action | Setup | Expected | Actual | Result | Notes |
|----|--------|-------|---------------|-------|----------|--------|--------|-------|
| AUTH-001 | Auth | EMP001 | `POST /api/auth/login` as admin@test.com | Bootstrap applied | 200, cookie set, role=SystemAdministrator | 200 | Pass | |
| AUTH-002 | Auth | EMP002 | `POST /api/auth/login` as hr.admin@test.com | EMP002 created | 200, cookie set, role=HRAdministrator | 200 | Pass | |
| AUTH-003 | Auth | EMP003 | `POST /api/auth/login` as manager@test.com | EMP003 created | 200, cookie set, role=Manager | 200 | Pass | |
| AUTH-004 | Auth | EMP004 | `POST /api/auth/login` as employee@test.com | EMP004 created | 200, cookie set, role=Employee | 200 | Pass | |

## Employee (EMP) Scenarios

| ID | Module | Actor | Request/Action | Setup | Expected | Actual | Result | Notes |
|----|--------|-------|---------------|-------|----------|--------|--------|-------|
| EMP-001 | Employees | EMP001 | `GET /api/employees` | EMP001 logged in | 200, org-wide list | 200 | Pass | |
| EMP-002 | Employees | EMP002 | `GET /api/employees` | EMP002 logged in | 200, org-wide list | 200 | Pass | |
| EMP-003 | Employees | EMP003 | `GET /api/employees` | EMP003 logged in | 200, team-only list | 200 | Pass | |
| EMP-004 | Employees | EMP004 | `GET /api/employees` | EMP004 logged in | 403 | 403 | Pass | |
| EMP-005 | Employees | EMP004 | `GET /api/employees/{EMP004_ID}` | EMP004 logged in | 200, self detail | 200 | Pass | |
| EMP-006 | Employees | EMP004 | `GET /api/employees/{EMP001_ID}` | EMP004 logged in | 403 | 403 | Pass | |
| EMP-007 | Employees | EMP003 | `GET /api/employees/{EMP004_ID}` | EMP003 logged in | 200, team detail | 200 | Pass | |
| EMP-008 | Employees | EMP004 | `POST /api/employees` | EMP004 logged in | 403 | 403 | Pass | |
| EMP-009 | Employees | EMP002 | `POST /api/employees` | EMP002 logged in | 200, created | 200 | Pass | |
| EMP-010 | Employees | EMP002 | `PUT /api/employees/{id}/role` | EMP002 logged in | 403 | 403 | Pass | |

## Vacation (VAC) Scenarios

| ID | Module | Actor | Request/Action | Setup | Expected | Actual | Result | Notes |
|----|--------|-------|---------------|-------|----------|--------|--------|-------|
| VAC-001 | Vacations | EMP004 | `POST /api/vacationrequests` (self) | EMP004 logged in | 201, own request created | 201 | Pass | |
| VAC-002 | Vacations | EMP004 | `GET /api/vacationrequests` | EMP004 logged in | 200, own-only list | 200 | Pass | |
| VAC-003 | Vacations | EMP004 | `GET /api/vacationrequests/{other}` | EMP004 logged in | 403 or 404 | 404 | Pass | |
| VAC-004 | Vacations | EMP003 | `GET /api/vacationrequests` (team) | EMP003 logged in | 200, team-only list | 200 | Pass | |
| VAC-005 | Vacations | EMP003 | `PUT /api/vacationrequests/{id}/status` (team) | EMP003 logged in, VAC-001 ID | 200, approved | 200 | Pass | |
| VAC-006 | Vacations | EMP003 | `PUT /api/vacationrequests/{id}/status` (self) | EMP003 logged in, own request | 422 (self-review blocked) | 422 | Pass | |
| VAC-007 | Vacations | EMP001 | `GET /api/vacationrequests` | EMP001 logged in | 200, org-wide list | 200 | Pass | |
| VAC-008 | Vacations | EMP001 | `POST /api/vacationrequests` (for EMP004) | EMP001 logged in | 201, created for employee | 201 | Pass | |
| VAC-009 | Vacations | EMP004 | `POST /api/vacationrequests` (overlap) | EMP004 logged in | 422, overlap | 422 | Pass | |
| VAC-010 | Vacations | EMP004 | `PUT /api/vacationrequests/{id}/status` | EMP004 logged in | 422, self-review | 422 | Pass | |
| VAC-011 | Vacations | EMP004 | Filter outside scope | EMP004 logged in | Empty scoped page | Empty | Pass | |
| VAC-012 | Vacations | EMP003 | Filter for non-team employee | EMP003 logged in | Empty scoped page | Empty | Pass | |

## Trip (TRIP) Scenarios

| ID | Module | Actor | Request/Action | Setup | Expected | Actual | Result | Notes |
|----|--------|-------|---------------|-------|----------|--------|--------|-------|
| TRIP-001 | Trips | EMP004 | `POST /api/trips` (self) | EMP004 logged in | 201, own trip | 201 | Pass | |
| TRIP-002 | Trips | EMP004 | `GET /api/trips` | EMP004 logged in | 200, own-only list | 200 | Pass | |
| TRIP-003 | Trips | EMP004 | `GET /api/trips/{EMP003_trip_id}` | EMP004 logged in | 403 | 403 | Pass | |
| TRIP-004 | Trips | EMP004 | `DELETE /api/trips/{id}` (own trip) | EMP004 logged in | 204 | 204 | Pass | |
| TRIP-005 | Trips | EMP003 | `POST /api/trips` (team) | EMP003 logged in | 201, team trip created | 201 | Pass | |
| TRIP-006 | Trips | EMP003 | `GET /api/trips` | EMP003 logged in | 200, own+team list | 200 | Pass | |
| TRIP-007 | Trips | EMP003 | `DELETE /api/trips/{EMP004_trip}` | EMP003 logged in | 204, team trip delete | 204 | Pass | |
| TRIP-008 | Trips | EMP001 | `GET /api/trips` | EMP001 logged in | 200, org-wide list | 200 | Pass | |
| TRIP-009 | Trips | EMP001 | `POST /api/trips` (for EMP004) | EMP001 logged in | 201, created for employee | 201 | Pass | |
| TRIP-010 | Trips | EMP004 | `POST /api/trips` (past date) | EMP004 logged in | 422 | 422 | Pass | |
| TRIP-011 | Trips | EMP004 | `POST /api/trips` (non-working day) | EMP004 logged in | 422 | 422 | Pass | |
| TRIP-012 | Trips | EMP004 | Filter for non-self travelerId | EMP004 logged in | Empty scoped page | Empty | Pass | |

## Sensitive Module (SENS) Scenarios

| ID | Module | Actor | Request/Action | Setup | Expected | Actual | Result | Notes |
|----|--------|-------|---------------|-------|----------|--------|--------|-------|
| SENS-001 | Attendance | EMP004 | `POST /api/attendance/clock-in` | EMP004 logged in | 201 | 201 | Pass | |
| SENS-002 | Attendance | EMP004 | `POST /api/attendance/clock-in` duplicate | EMP004 logged in | 409 | 409 | Pass | |
| SENS-003 | Attendance | EMP004 | `POST /api/attendance/clock-out` | EMP004 logged in | 200 | 200 | Pass | |
| SENS-004 | Compensation | EMP002 | `GET /api/employees/{id}/compensation` | EMP002 logged in | 200 | 200 | Pass | |
| SENS-005 | Compensation | EMP004 | `GET /api/employees/{id}/compensation` | EMP004 logged in | 403 | 403 | Pass | |
| SENS-006 | Documents | EMP002 | `POST /api/employees/{id}/documents` | EMP002 logged in | 201 | 201 | Pass | |
| SENS-007 | Dashboard | EMP003 | `GET /api/dashboard/summary` | EMP003 logged in | 200, team-scoped | 200 | Pass | |
| SENS-008 | Dashboard | EMP002 | `GET /api/dashboard/summary` | EMP002 logged in | 200, org-wide | 200 | Pass | |
| SENS-009 | Dashboard | EMP004 | `GET /api/dashboard/summary` | EMP004 logged in | 403 | 403 | Pass | |
| SENS-010 | Audit | EMP002 | `GET /api/audit-logs` | EMP002 logged in | 200 | 200 | Pass | |

## Error Compatibility (ERR) Scenarios

| ID | Module | Actor | Request/Action | Setup | Expected | Actual | Result | Notes |
|----|--------|-------|---------------|-------|----------|--------|--------|-------|
| ERR-001 | Error | None | `GET /api/auth/me` (no login) | No cookie | 401, `{code, message}` | 401 | Pass | |
| ERR-002 | Error | EMP004 | `GET /api/audit-logs` | EMP004 logged in | 403, `{code, message}` | 403 | Pass | |
| ERR-003 | Error | EMP004 | `GET /api/employees/{nonexistent}` | EMP004 logged in | 404, `{code, message}` | 404 | Pass | |
| ERR-004 | Error | EMP001 | `POST /api/employees` duplicate EMP number | EMP001 logged in | 409, `{code, message}` | 409 | Pass | |
| ERR-005 | Error | EMP004 | `POST /api/vacationrequests` past date | EMP004 logged in | 422, `{code, message}` | 422 | Pass | |
| ERR-006 | Error | EMP002 | `POST /api/employees/{id}/documents` with oversized file | EMP002 logged in | 413, `{code, message}` | 413 | Pass | |
| ERR-007 | Error | EMP002 | `PUT /api/employees/{id}/role` | EMP002 logged in | 403, `{code, message}` | 403 | Pass | |
| ERR-008 | Error | EMP004 | `POST /api/trips` for outside-scope traveler | EMP004 logged in | 403, `{code, message}` | 403 | Pass | |

## STOP on Runtime Defect Rule

If any scenario produces an unexpected result indicating a runtime defect:
1. Record actual result and notes in this checklist.
2. Record the failure in `implementation-summary.md`.
3. Do not edit source code.
4. Stop and request explicit approval for separate remediation scope.
