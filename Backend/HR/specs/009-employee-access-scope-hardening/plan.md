# Implementation Plan: Phase 9 - Employee Access Scope Hardening

**Branch**: `009-employee-access-scope-hardening` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/009-employee-access-scope-hardening/spec.md`

## Summary

Harden employee endpoint access using the completed Phase 8 authorization scope foundation. Normal employees must not list all employees and may only view their own detail record. Managers may list direct and indirect active reports only and may view self/team details. HR administrators and system administrators retain organization-wide list/detail access, including terminated and soft-deleted employee records. Employee create, update, and delete become HR/System administrator operations, with HR administrators blocked from updating or deleting `SystemAdministrator` records and all updates/deletes/terminations/status changes or role demotions blocked if they would leave zero active system administrators.

The implementation should keep existing routes, request/response DTOs, cookie authentication, claims, structured errors, and pagination shape. It should make employee service methods requester-aware, keep authorization decisions in the service layer, and update controllers only to pass the current employee ID and map `Result` failures.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core 8, ASP.NET Core Identity, Entity Framework Core 8.0.20, SQL Server provider, System.Text.Json, Swashbuckle

**Storage**: Existing SQL Server database through `ApplicationDbContext`; no Phase 9 schema change is planned.

**Testing**: xUnit, SQLite integration tests through `SqliteTestEnvironment`, controller tests for structured response compatibility, focused employee access matrix tests, EF pending-model check.

**Target Platform**: Windows or Linux server hosted with Kestrel or IIS

**Project Type**: Layered ASP.NET Core Web API

**Performance Goals**: Employee list scope filtering should happen before pagination and should not expose out-of-scope counts or items. Manager team filtering should reuse direct/indirect report IDs and remain suitable for the current project scale.

**Constraints**: Preserve cookie-based sessions, existing login/current-user behavior, current employee routes, request DTOs, success response DTOs, pagination envelope, structured `{ code, message }` error shape, existing business rule error codes, Phase 8 scope behavior, and Phase 6 DI ownership. Do not change vacation, trip, compensation, document, dashboard, audit, bootstrap, or Swagger behavior. Do not create migrations.

**Scale/Scope**: One focused authorization hardening slice for `EmployeesController`, `IEmployeeService`, `EmployeeService`, `IEmployeeRepository`/`EmployeeRepository`, employee access tests, and lifecycle documentation wording.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Layered Architecture (I)**: PASS. Controller changes remain thin HTTP adaptation. Service contracts stay in `HR.Application`. Implementation and EF-backed repository changes stay in `HR.Infrastructure`. No new project or dependency direction is introduced.
- **Cookie-Based Session Authentication (II)**: PASS. Phase 9 keeps cookie auth and existing claims. No JWT, token refresh, SSO, or auth redesign is planned.
- **Service Layer Separation (III)**: PASS. Business scope checks are planned in `EmployeeService`, not only in controller attributes. Controllers only extract the requester employee ID and map service results.
- **Domain Integrity (IV)**: PASS. Existing employee business rules remain active, including duplicate email/number checks, status transition rules, employee-number immutability, manager-cycle prevention, soft deletion, same-status idempotence, and last-active-system-administrator protection for updates, deletes, termination, status changes, and role demotion added by this phase.
- **Global Error Handling (V)**: PASS. Expected access failures use existing `ServiceError.Forbidden()` and structured `{ code, message }` mapping. Existing error codes are not renamed.
- **Data Access Abstraction (VI)**: PASS. Employee service continues to use repositories and unit of work. New query needs belong in `IEmployeeRepository`/`EmployeeRepository`; services do not directly reference `ApplicationDbContext`.
- **Simplicity & YAGNI (VII)**: PASS. The plan avoids new roles, permission engines, policy frameworks, public directory features, generic repositories, MediatR, CQRS, and schema changes.

### Post-Design Re-check

The Phase 0 and Phase 1 artifacts preserve all constitution gates. The plan uses existing Phase 8 scope definitions, extends current employee service/repository boundaries, and creates no migration. No complexity exceptions are required.

## Technical Approach

### 1. Requester-Aware Employee Service Contract

Update employee service methods so all employee endpoint operations that need scope receive the requester employee ID:

- list employees
- get employee detail
- create employee
- update employee
- delete employee
- role assignment remains requester-aware and system-administrator-only, with last-active-SystemAdministrator demotion protection

Read methods that can fail for authorization should return `Result<T>` instead of nullable or raw payloads so the service can return `FORBIDDEN`, `UNAUTHORIZED`, or existing `NOT_FOUND` consistently. Controller changes should be limited to extracting `employee_id` from the current claims principal and mapping service results.

### 2. List Scope Rules

Employee list behavior:

- `Employee`: return `403 Forbidden`, never self-only list.
- `Manager`: list active direct and indirect reports only; exclude manager self, peers, unrelated employees, soft-deleted reports, and terminated reports.
- `HRAdministrator` / `SystemAdministrator`: list organization-wide records including active, suspended, terminated, and soft-deleted employees, subject to existing `status`, `page`, and `pageSize` query parameters.

Scope filtering must happen before pagination so item counts and pages do not reveal out-of-scope employees.

### 3. Detail Scope Rules

Employee detail behavior:

- `Employee`: self only.
- `Manager`: self plus active direct and indirect reports only.
- `HRAdministrator` / `SystemAdministrator`: any existing employee, including terminated and soft-deleted records.
- Missing employee: preserve existing not-found behavior.
- Existing out-of-scope employee: return `403 Forbidden`.

Repository detail methods need a clear distinction between "missing record" and "existing but out of permitted scope" where the service must choose `404` or `403`.

### 4. Write Scope Rules

Create, update, and delete behavior:

- `Employee`: forbidden.
- `Manager`: forbidden.
- `HRAdministrator`: allowed for non-`SystemAdministrator` employee records only, subject to existing employee business rules.
- `SystemAdministrator`: allowed, subject to existing employee business rules and last-active-system-administrator protection.

The service must validate authorization before mutating state or updating Identity user data. Existing update/delete transactional behavior remains in place after authorization passes.

### 5. Protected SystemAdministrator Records and Last-Admin Guard

Phase 9 adds these write guards:

- HR administrators cannot update or delete `SystemAdministrator` employee records.
- System administrator update/delete/termination/status changes that would leave zero active `SystemAdministrator` employees are rejected before mutation.
- System administrator role assignment/demotion from `SystemAdministrator` to any other role is rejected before mutation when the target is an active `SystemAdministrator` and the demotion would leave zero active `SystemAdministrator` employees.
- Demoting a `SystemAdministrator` may be allowed only when at least one other active `SystemAdministrator` remains and all other role-assignment rules pass.
- The active system administrator count is derived from existing data: `Role == SystemAdministrator`, `Status == Active`, and `IsDeleted == false`.

Role assignment remains restricted to system administrators. HR administrators, managers, and normal employees remain forbidden from assigning roles.

### 6. Repository Query Support

Repository changes should stay narrow and query-oriented:

- scoped employee page by allowed employee IDs
- organization-wide employee page including soft-deleted rows
- detail lookup including soft-deleted rows for HR/System historical access
- existence lookup that includes soft-deleted rows to distinguish missing from forbidden where needed
- active system administrator count or guard helper used by update, delete, status-change, termination, and role-demotion checks

No new tables, columns, relationships, or indexes are planned.

### 7. Documentation Update

Update stale employee sections in `API_LIFECYCLE_TESTING_GUIDE.md` during implementation:

- remove wording that employee endpoints are merely "authenticated user" operations
- document normal employee `GET /api/employees` as `403`
- document manager team-only list/detail
- document HR/System organization-wide historical access
- document HR/System-only create/update/delete
- document SystemAdministrator-only role assignment
- document last-active-SystemAdministrator demotion rejection

Avoid Phase 12 full lifecycle retest rewrite; Phase 9 only updates stale employee-access wording.

## Files and Modules Likely to Change

### Existing Files

```text
HR.API/
|-- Controllers/
|   `-- EmployeesController.cs

HR.Application/
|-- Employees/
|   `-- IEmployeeService.cs

HR.Infrastructure/
|-- Employees/
|   `-- EmployeeService.cs
`-- Repositories/
    |-- IEmployeeRepository.cs
    `-- EmployeeRepository.cs

HR.Tests/
|-- Employees/
|   |-- EmployeeAccessScopeTests.cs
|   `-- EmployeeServiceBusinessRuleTests.cs
|-- Authorization/
|   `-- EmployeeRoleControllerTests.cs
|-- Compatibility/
|   `-- ErrorResponseParityTests.cs
`-- TestInfrastructure/
    `-- SqliteTestEnvironment.cs

API_LIFECYCLE_TESTING_GUIDE.md
AGENTS.md
```

### New Files

```text
specs/009-employee-access-scope-hardening/
|-- research.md
|-- data-model.md
|-- quickstart.md
`-- contracts/
    `-- phase9-employee-access-contract.md
```

Potential new test file:

```text
HR.Tests/Employees/EmployeeAccessScopeTests.cs
```

### Files Expected to Remain Unchanged

```text
HR.Domain/Entities/Employee.cs
HR.Domain/Enums/EmployeeRole.cs
HR.Domain/Enums/EmployeeStatus.cs
HR.Infrastructure/Data/Migrations/
HR.API/Controllers/VacationRequestsController.cs
HR.API/Controllers/TripsController.cs
HR.API/Controllers/CompensationController.cs
HR.API/Controllers/EmployeeDocumentsController.cs
HR.API/Controllers/DashboardController.cs
HR.API/Controllers/AuditLogsController.cs
```

## Data Model Changes

See [data-model.md](./data-model.md).

Expected schema changes: none.

Phase 9 uses existing fields:

- `Employee.Id`
- `Employee.ManagerId`
- `Employee.Role`
- `Employee.Status`
- `Employee.IsDeleted`
- `Employee.TerminatedAt`

The active system administrator guard is a derived rule over existing rows.

## Internal and External Contracts

See [phase9-employee-access-contract.md](./contracts/phase9-employee-access-contract.md).

## API and Route Changes

Existing employee routes remain stable:

- `GET /api/employees`
- `GET /api/employees/{id}`
- `POST /api/employees`
- `PUT /api/employees/{id}`
- `DELETE /api/employees/{id}`
- `PUT /api/employees/{id}/role`

Phase 9 changes authorization outcomes and filtering only. It does not add routes or change success DTO shapes.

## Validation and Error Handling

- Missing or invalid `employee_id` claim maps to existing structured `401 Unauthorized`.
- Authenticated but out-of-scope access maps to `403 Forbidden` with `{ code, message }`.
- Missing employee IDs preserve existing `404 NOT_FOUND`.
- Existing business-rule, validation, and conflict behavior remains unchanged after authorization passes.
- Last-active-system-administrator violations, including role demotion from `SystemAdministrator` to any other role, should be a business-rule style failure unless the implementation finds an existing more specific compatibility code. Do not normalize existing error codes.
- Authorization and last-active-system-administrator checks must occur before state mutation, Identity email updates, direct-report reassignment, pending vacation rejection, soft deletion, status transition side effects, or role changes.

## Testing and Check Strategy

### Automated Checks

1. `dotnet restore .\HR.slnx`
2. `dotnet build .\HR.slnx -c Release`
3. `dotnet test .\HR.slnx -c Release --no-build`
4. Focused employee access tests for:
   - Employee list forbidden
   - Employee self detail allowed
   - Employee other detail forbidden
   - Manager list direct/indirect active reports only
   - Manager detail self/team allowed and outside-team denied
   - Manager no-team list returns empty page
   - HR/System list includes active, suspended, terminated, and soft-deleted records
   - HR/System detail includes terminated and soft-deleted records
   - Employee/Manager create/update/delete forbidden before mutation
   - HR update/delete SystemAdministrator forbidden
   - SystemAdministrator update/delete SystemAdministrator allowed except last-active removal
   - Role assignment remains SystemAdministrator-only
   - Last-active SystemAdministrator role demotion rejected before mutation
   - SystemAdministrator role demotion allowed only when another active SystemAdministrator remains
5. EF check:
   `dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj`

### Static Checks

```powershell
rg -n "GetEmployeesAsync|GetEmployeeByIdAsync|CreateEmployeeAsync|UpdateEmployeeAsync|DeleteEmployeeAsync" .\HR.API .\HR.Application .\HR.Infrastructure .\HR.Tests
rg -n "HR.Infrastructure" .\HR.Application
git status --short .\HR.Infrastructure\Data\Migrations
git diff --check
```

Expected results:

- All employee service call sites are updated for requester-aware signatures.
- `HR.Application` still does not reference `HR.Infrastructure`.
- No new migration files are created.
- `git diff --check` reports no whitespace errors.

## Manual Smoke Validation

Use [quickstart.md](./quickstart.md) after implementation.

Smoke coverage:

- login as normal employee, manager, HR administrator, and system administrator
- employee list/detail access matrix
- HR/System historical employee access
- create/update/delete write restrictions
- protected `SystemAdministrator` records
- last-active-system-administrator guard, including role demotion
- no vacation/trip behavior changes

## Migration and Backfill Strategy

No migration is planned.

The current data model already represents every Phase 9 rule:

- roles via `Employee.Role`
- manager hierarchy via `Employee.ManagerId`
- active/terminated state via `Employee.Status`
- soft deletion via `Employee.IsDeleted`

If implementation discovers a schema blocker, stop before creating any migration and report the required business rule, affected table/columns/indexes, existing-data impact, proposed migration name, and tests proving the need.

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Employee list leaks out-of-scope counts or records | Apply role/scope filtering before pagination and cover counts/items in tests. |
| Manager list accidentally includes manager self | Treat manager list as team reports only; self is allowed only for detail. |
| HR/System cannot access historical deleted records | Add explicit repository methods that include soft-deleted rows for organization-scope list/detail. |
| HR administrator disables a system administrator | Block HR update/delete attempts against `SystemAdministrator` targets. |
| System administrator locks out administration | Reject update/delete/termination/status changes and role demotion that would leave zero active system administrators. |
| Controller-only authorization creeps in | Keep business scope decisions in `EmployeeService`; controllers pass requester ID only. |
| Interface signature changes break tests/fakes | Update controller tests and compatibility test doubles as part of implementation. |
| Phase 10/11 work is pulled into Phase 9 | Keep vacation and trip files out of scope except static verification that they did not change. |

## Dependencies on Previous Phases

- **Phase 5**: Employee business rules, soft deletion, status transitions, duplicate email/number checks, manager-cycle prevention, and employee-number immutability.
- **Phase 6**: `AddApplication()` / `AddInfrastructure(configuration)` registration ownership and clean host composition.
- **Phase 7**: Single employee role model and SystemAdministrator role assignment endpoint.
- **Phase 8**: Reusable employee access context, self/team/organization scope definitions, and direct-plus-indirect manager team scope.

## Out of Scope

- Vacation request scope hardening.
- Trip ownership/scope hardening.
- Swagger/OpenAPI response documentation pass.
- New routes, public employee directory, or employee self-service edit workflow.
- Authentication redesign, JWT, token refresh, SSO, or public setup endpoints.
- New role names, multi-role employees, temporary grants, or permission-union behavior.
- Compensation, document, dashboard, attendance, audit-log, or bootstrap behavior changes.
- Database schema changes unless separately approved after blocker analysis.

## Project Structure

### Documentation (this feature)

```text
specs/009-employee-access-scope-hardening/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- phase9-employee-access-contract.md
|-- checklists/
|   |-- requirements.md
|   `-- employee-access.md
`-- tasks.md                 # generated later by /speckit-tasks
```

### Source Code (repository root)

```text
HR.API/
`-- Controllers/
    `-- EmployeesController.cs

HR.Application/
|-- Employees/
|   `-- IEmployeeService.cs
`-- DTOs/
    `-- Employees/

HR.Infrastructure/
|-- Employees/
|   `-- EmployeeService.cs
`-- Repositories/
    |-- IEmployeeRepository.cs
    `-- EmployeeRepository.cs

HR.Tests/
|-- Employees/
|-- Authorization/
|-- Compatibility/
`-- TestInfrastructure/
```

**Structure Decision**: Continue the existing five-project layered solution. Phase 9 is a targeted employee access hardening change and should not add architecture layers or new source projects.

## Complexity Tracking

No constitution violations or complexity exceptions are required.
