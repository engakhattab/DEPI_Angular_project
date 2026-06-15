# Implementation Plan: Phase 8 - Authorization Scope Foundation

**Branch**: `008-authorization-scope-foundation` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/008-authorization-scope-foundation/spec.md`

## Summary

Create and verify the reusable authorization scope foundation that later employee, vacation, and trip hardening phases will use. Phase 8 builds on the existing Phase 7 RBAC work and current `IEmployeeAccessService` instead of introducing a new authorization system.

The plan is intentionally narrow: refine or confirm shared current-employee, role, self, team, organization, and visible-employee decisions; add focused tests around those decisions; and preserve all public endpoint behavior until Phases 9, 10, and 11.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core 8 cookie authentication and authorization, ASP.NET Core Identity, Entity Framework Core 8.0.20, SQL Server provider, xUnit, SQLite integration test infrastructure, existing `HR.Shared.Results` and `HR.Shared.Pagination`

**Storage**: Existing SQL Server schema through `ApplicationDbContext`; Phase 8 should use existing `Employees.Role`, `Employees.ManagerId`, `Employees.Status`, and `Employees.IsDeleted`. No new table, column, index, or EF migration is planned.

**Testing**: xUnit service-level and integration tests using SQLite test infrastructure; static boundary checks for no API route/contract/migration changes; EF pending-model check after implementation.

**Target Platform**: ASP.NET Core Web API hosted on Windows or Linux with Kestrel or IIS

**Project Type**: Layered ASP.NET Core Web API using five projects: `HR.API`, `HR.Application`, `HR.Infrastructure`, `HR.Domain`, and `HR.Shared`

**Performance Goals**: Scope checks should remain suitable for normal interactive API use. Manager visible-employee resolution should remain bounded to existing employee hierarchy traversal and should not add endpoint-wide eager loading or controller-side filtering in Phase 8.

**Constraints**: Preserve cookie authentication, existing claims, role names, routes, response JSON, cookies, status codes, error codes, Phase 7 modules, Phase 6 DI ownership, repository abstraction, cancellation token forwarding, and current endpoint behavior. Do not implement employee/vacation/trip endpoint hardening in Phase 8. Do not create migrations unless separately approved after proving the existing model cannot represent required scope decisions.

**Scale/Scope**: Shared authorization service contract refinement and focused tests only. No frontend changes, no endpoint-specific hardening, no new database schema, no new authentication mechanism.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Layered Architecture (I)**: PASS. Shared service contracts remain in `HR.Application`; implementation remains in `HR.Infrastructure`; role enum and employee hierarchy remain in `HR.Domain`; HTTP adapters stay in `HR.API`.
- **Cookie-Based Session Authentication (II)**: PASS. Phase 8 preserves cookie auth, existing claims, and session validation. It does not introduce JWT, token refresh, SSO, or setup endpoints.
- **Service Layer Separation (III)**: PASS. Business-scope decisions are service-layer decisions. Controller attributes are explicitly insufficient for Phase 8 scope requirements.
- **Domain Integrity (IV)**: PASS. Phase 8 does not alter Phase 5 or Phase 7 domain rules. It clarifies deleted, terminated, suspended, self, manager, and organization eligibility.
- **Global Error Handling (V)**: PASS. No public response mapping changes are planned. Existing `{ code, message }` error behavior must remain compatible.
- **Data Access Abstraction (VI)**: PASS. The infrastructure implementation uses `IEmployeeRepository`; services must not reference `ApplicationDbContext` directly outside repositories.
- **Simplicity & YAGNI (VII)**: PASS. Phase 8 extends the existing `IEmployeeAccessService` instead of introducing policy engines, MediatR, CQRS, generic repositories, or a new auth framework.

### Post-Design Re-check

The Phase 0 and Phase 1 artifacts preserve all gates. The design uses existing role and manager data, adds no schema, keeps implementation in Infrastructure, and does not change public API behavior.

## Current Code Findings

- `HR.Application/Authorization/IEmployeeAccessService.cs` already defines `GetCurrentAsync`, `HasAnyRoleAsync`, `CanAccessEmployeeAsync`, and `GetVisibleEmployeeIdsAsync`.
- `HR.Infrastructure/Authorization/EmployeeAccessService.cs` already resolves employees through `IEmployeeRepository`, denies deleted or terminated requesters for role/scope checks, treats suspended users as not active but still role-eligible, grants organization scope to `HRAdministrator` and `SystemAdministrator`, and uses direct-plus-indirect report IDs for manager scope.
- `HR.Infrastructure/Repositories/EmployeeRepository.cs` already supports direct-plus-indirect report traversal with active, non-deleted employees only.
- `HR.Domain/Enums/EmployeeRole.cs` already uses the required role names: `Employee`, `Manager`, `HRAdministrator`, `SystemAdministrator`.
- `HR.API/Extensions/ClaimsPrincipalExtensions.cs` already centralizes parsing of the `employee_id` claim for controllers.
- Existing tests cover several Phase 8 behaviors, including manager direct/indirect access, visible report sets, role matrix checks, and deleted/terminated role rejection.
- The current access contract does not expose every named decision from the Phase 8 spec as a first-class method, especially explicit self, manager-of-target, team-data, and organization-scope decisions. Phase 8 should close that contract clarity gap without changing endpoint behavior.

## Technical Approach

### 1. Preserve the Existing Foundation

Keep `IEmployeeAccessService` as the single shared application-level contract for business scope decisions. Do not create a parallel authorization helper or controller-only policy abstraction.

Continue to resolve data through `IEmployeeRepository` in `HR.Infrastructure`. Do not move infrastructure-backed implementation into `HR.Application`, and do not inject `ApplicationDbContext` directly into services for scope checks.

### 2. Make Required Scope Decisions Explicit

Refine the shared contract so each Phase 8 required decision can be referenced by later phases without rediscovering local rules.

Planned contract surface:

- `GetCurrentAsync(Guid employeeId, CancellationToken ct)`
- `IsSelf(Guid requesterEmployeeId, Guid targetEmployeeId)`
- `IsManagerOfAsync(Guid requesterEmployeeId, Guid targetEmployeeId, CancellationToken ct)`
- `CanAccessEmployeeAsync(Guid requesterEmployeeId, Guid targetEmployeeId, CancellationToken ct)`
- `CanAccessTeamDataAsync(Guid requesterEmployeeId, Guid targetEmployeeId, CancellationToken ct)`
- `HasAnyRoleAsync(Guid employeeId, CancellationToken ct, params EmployeeRole[] roles)`
- `IsHRAdministratorAsync(Guid employeeId, CancellationToken ct)`
- `IsSystemAdministratorAsync(Guid employeeId, CancellationToken ct)`
- `HasOrganizationScopeAsync(Guid employeeId, CancellationToken ct)`
- `GetVisibleEmployeeIdsAsync(Guid requesterEmployeeId, CancellationToken ct)`

If implementation keeps some methods as default interface helpers or delegates to existing methods, the public decision names still need to be clear enough for Phases 9, 10, and 11.

### 3. Eligibility Rules

Current employee context must identify employee ID, role, active state, soft-delete state, and terminated state.

Authorization decisions must use these rules:

- Missing employee record: unauthorized or not allowed, depending on the decision return type.
- Deleted requester: not allowed.
- Terminated requester: not allowed.
- Suspended requester: scope-eligible in the shared foundation unless a later endpoint-specific phase forbids the action.
- Active, non-deleted direct and indirect reports are manager-team targets.
- Suspended, deleted, and terminated reports are excluded from manager visible-team data unless a later endpoint-specific phase explicitly documents an exception.
- `HRAdministrator` and `SystemAdministrator` have organization scope when not deleted or terminated, including suspended administrators in Phase 8.

### 4. Team Scope

Use direct plus indirect reports. This matches the Phase 8 clarification, roadmap preference, existing repository traversal, and current manager scope tests.

Department mismatch must not grant or deny team scope. The manager relationship remains the source of truth.

### 5. Public Behavior Boundary

Phase 8 must not:

- change employee list/detail/create behavior
- change vacation list/detail/create/review/delete behavior
- change trip list/detail/create/delete behavior
- change routes, response JSON, cookies, claims, status codes, or error codes
- add new database schema or migration
- add Phase 9, Phase 10, Phase 11, Phase 12, or Phase 13 behavior

### 6. Focused Validation

Add or refine focused tests around the shared access service:

- current employee context for active, suspended, terminated, deleted, and missing employees
- self decision for same and different employee IDs
- normal employee self-only access
- manager direct report, indirect report, peer, unrelated employee, own manager, deleted report, terminated report, and suspended report
- organization scope for HR administrator and System administrator
- no organization scope for Employee and Manager
- suspended administrator remains organization-scope eligible in Phase 8
- visible employee IDs for employee, manager, HR administrator, and System administrator
- no endpoint-specific route/contract/migration changes

## Files and Modules Likely to Change

### Existing Files

```text
HR.Application/
|-- Authorization/IEmployeeAccessService.cs

HR.Infrastructure/
|-- Authorization/EmployeeAccessService.cs
|-- Repositories/IEmployeeRepository.cs
`-- Repositories/EmployeeRepository.cs

HR.Tests/
|-- Authorization/AccessMatrixTests.cs
|-- Authorization/ManagerScopeTests.cs
`-- DependencyInjection/Phase7BoundaryTests.cs
```

### Existing Files Expected to Remain Behaviorally Stable

```text
HR.API/
|-- Program.cs
|-- Extensions/ClaimsPrincipalExtensions.cs
`-- Controllers/

HR.Domain/
|-- Entities/Employee.cs
`-- Enums/EmployeeRole.cs

HR.Infrastructure/
`-- Data/Migrations/
```

`Program.cs` should only change if a test reveals a real Phase 8 composition defect. No route, cookie, claim, status-code, or response-shape changes are planned.

### New Test Files, If Helpful

```text
HR.Tests/
`-- Authorization/EmployeeAccessFoundationTests.cs
```

No new production folders are planned.

## Data Model Changes

See [data-model.md](./data-model.md).

Expected schema changes: none.

Phase 8 uses existing data:

- `Employee.Id`
- `Employee.Role`
- `Employee.Status`
- `Employee.IsDeleted`
- `Employee.ManagerId`
- `Employee.DirectReports`

No migration or backfill is planned. If implementation discovers a schema gap, stop before creating a migration and report the business rule, why code-only is insufficient, proposed schema, backfill impact, and tests proving the need.

## Internal Contracts

See [authorization-scope-contract.md](./contracts/authorization-scope-contract.md).

Phase 8 has no new external HTTP API contract.

## API and Route Changes

None planned.

Existing endpoints remain unchanged until later phases:

- Phase 9: employee endpoint hardening
- Phase 10: vacation request scope hardening
- Phase 11: trip ownership and scope hardening
- Phase 12: lifecycle documentation rewrite
- Phase 13: Swagger/OpenAPI response documentation

## Validation and Error Handling

- Preserve `GlobalExceptionMiddleware` and existing structured error behavior.
- Service-level boolean decisions should return `false` for denied scope rather than throwing.
- `GetCurrentAsync` should continue using `Result<EmployeeAccessContext>` and return an unauthorized-compatible failure for missing current employee records.
- Do not rename existing error codes.
- Do not introduce public error behavior changes in Phase 8.

## Testing and Check Strategy

### Automated Checks

1. `dotnet restore .\HR.slnx`
2. `dotnet build .\HR.slnx -c Release`
3. `dotnet test .\HR.slnx -c Release --no-build --filter "FullyQualifiedName~Authorization"`
4. `dotnet test .\HR.slnx -c Release --no-build`
5. `dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj`

### Static Checks

```powershell
rg -n "HR.Infrastructure" .\HR.Application
rg -n "ApplicationDbContext" .\HR.Infrastructure\Authorization .\HR.Infrastructure\Employees .\HR.Infrastructure\VacationRequests .\HR.Infrastructure\Transportation
rg -n "GetEmployeesAsync|GetEmployeeByIdAsync|CreateEmployeeAsync|VacationRequest|Trip" .\HR.Tests\Authorization
git status --short .\HR.Infrastructure\Data\Migrations
git diff -- .\HR.API\Controllers .\HR.API\Program.cs
```

Expected results:

- `HR.Application` still does not reference `HR.Infrastructure`.
- Authorization implementation uses repository abstractions, not direct DbContext access.
- Phase 8 authorization tests do not implement employee/vacation/trip endpoint hardening by accident.
- No new migration files exist.
- `Program.cs` and controllers have no behavior-changing diffs unless a real defect was explicitly approved.

## Manual Smoke Validation

See [quickstart.md](./quickstart.md).

Manual validation is mostly optional for Phase 8 because the work is shared-service focused. If performed, it should verify that representative existing endpoints still behave as before and that no employee/vacation/trip endpoint hardening appeared early.

## Migration and Backfill Strategy

No migration is planned.

Existing rows remain valid because Phase 8 uses:

- existing role values from `Employees.Role`
- existing manager hierarchy from `Employees.ManagerId`
- existing status/deletion state from `Employees.Status` and `Employees.IsDeleted`

Any future schema requirement must follow the roadmap migration policy and stop for approval before migration creation.

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Endpoint hardening slips into Phase 8 | Keep tasks scoped to access service and tests; add static diffs against controllers and service contracts for employee/vacation/trip endpoints. |
| Duplicate scope logic appears in controllers | Keep `IEmployeeAccessService` as the application-level contract and document controller attributes as insufficient for business scope. |
| Suspended requester behavior conflicts with suspended target behavior | Tests must distinguish suspended requester eligibility from exclusion of suspended team targets. |
| Manager hierarchy traversal changes behavior | Reuse existing `GetDirectAndIndirectReportIdsAsync` and cover direct, indirect, peer, unrelated, deleted, terminated, and suspended cases. |
| Future phases redefine scope differently | Include explicit internal contract and data model docs that Phases 9, 10, and 11 must reference. |
| Schema changes are introduced unnecessarily | Record no-migration gate and run EF pending-model checks after implementation. |

## Dependencies on Previous Phases

- **Phase 2**: Cookie/session authentication and employee claims.
- **Phase 4**: Repository abstraction and entity configurations.
- **Phase 5**: Employee status, soft deletion, terminated session revocation, and business-rule discipline.
- **Phase 6**: `AddApplication()` and `AddInfrastructure(configuration)` registration ownership.
- **Phase 7**: Employee roles, RBAC policies, `IEmployeeAccessService`, dashboard/attendance/compensation/document/audit usage of access decisions.

## Out of Scope

- Employee list/detail/create/role hardening for Phase 9.
- Vacation request listing/detail/create/review/delete hardening for Phase 10.
- Trip listing/detail/create/delete requester/traveler hardening for Phase 11.
- Lifecycle guide rewrite for Phase 12.
- Swagger response documentation pass for Phase 13.
- Authentication redesign, JWT, SSO, role hierarchy redesign, multi-role users, permission union behavior, public setup endpoints, or frontend work.
- Database schema changes without a separate approved migration decision.

## Project Structure

### Documentation (this feature)

```text
specs/008-authorization-scope-foundation/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- authorization-scope-contract.md
|-- checklists/
|   `-- requirements.md
`-- tasks.md                 # generated later by /speckit-tasks
```

### Source Code (repository root)

```text
HR.API/
|-- Controllers/             # unchanged in Phase 8 unless a real defect is approved
|-- Extensions/
|   |-- AuthorizationPolicyExtensions.cs
|   `-- ClaimsPrincipalExtensions.cs
`-- Program.cs               # host composition remains stable

HR.Application/
`-- Authorization/
    `-- IEmployeeAccessService.cs

HR.Infrastructure/
|-- Authorization/
|   `-- EmployeeAccessService.cs
`-- Repositories/
    |-- IEmployeeRepository.cs
    `-- EmployeeRepository.cs

HR.Domain/
|-- Entities/
|   `-- Employee.cs
`-- Enums/
    |-- EmployeeRole.cs
    `-- EmployeeStatus.cs

HR.Tests/
|-- Authorization/
`-- DependencyInjection/
```

**Structure Decision**: Continue the existing five-project layered solution. Phase 8 refines and validates the existing authorization foundation instead of creating a new authorization subsystem.

## Complexity Tracking

No constitution violations or complexity exceptions are required.
