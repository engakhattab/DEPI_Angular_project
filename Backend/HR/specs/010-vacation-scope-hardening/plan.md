# Implementation Plan: Phase 10 - Vacation Request Scope Hardening

**Branch**: `010-vacation-scope-hardening` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/010-vacation-scope-hardening/spec.md`

## Summary

Harden vacation request access using the completed Phase 8 authorization scope foundation and the Phase 9 requester-aware service pattern. Employees may list/view/create/delete only their own permitted vacation requests. Managers may list active direct/indirect team requests by default, use an explicit self `employeeId` filter for their own list, view self/team details, create/delete only their own pending requests, and review active team requests only. HR administrators and system administrators retain organization-wide vacation visibility, may create requests on behalf of eligible employees, may delete pending requests, and may review non-self requests.

The implementation should keep existing routes, request/response DTO compatibility, cookie authentication, claims, pagination envelope, structured errors, and existing vacation validation rules. Controllers should remain thin by extracting the current employee ID claim and passing it to requester-aware service methods. Authorization and scope checks must live in the service layer and run before vacation domain-rule validation for existing out-of-scope targets.

Persisted creator tracking is required by the Phase 10 spec, but the current `VacationRequest` entity has no creator column. This plan documents the required migration/backfill strategy and marks it as an explicit approval gate. No migration is created during planning.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core 8, ASP.NET Core Identity, Entity Framework Core 8, SQL Server provider, System.Text.Json, Swashbuckle

**Storage**: Existing SQL Server database through `ApplicationDbContext`. Scope hardening can be implemented against current vacation/employee tables. Persisted creator tracking requires a separately approved nullable `VacationRequests.CreatedByEmployeeId` migration.

**Testing**: xUnit, SQLite integration tests through `SqliteTestEnvironment`, controller tests for claim propagation and structured response compatibility, focused vacation access matrix tests, repository paging/filtering tests, EF pending-model check.

**Target Platform**: Windows or Linux server hosted with Kestrel or IIS

**Project Type**: Layered ASP.NET Core Web API

**Performance Goals**: Vacation request list scope filtering must happen before pagination and total-count calculation. Manager team filtering should reuse Phase 8 direct/indirect report IDs and remain suitable for current project scale.

**Constraints**: Preserve cookie-based sessions, existing login/current-user behavior, current vacation routes, request DTO names, successful response compatibility, pagination envelope, structured `{ code, message }` error shape, existing vacation business-rule error codes, Phase 8 scope behavior, Phase 9 employee hardening, and Phase 6 DI ownership. Do not change employee, trip, compensation, document, attendance, dashboard, audit, bootstrap, or Swagger behavior. Do not create migrations without separate approval.

**Scale/Scope**: One focused vacation request hardening slice for `VacationRequestsController`, `IVacationRequestService`, `VacationRequestService`, `IVacationRequestRepository`/`VacationRequestRepository`, vacation access tests, and narrowly scoped lifecycle documentation wording if current docs describe stale vacation access.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Layered Architecture (I)**: PASS. Controller changes remain thin HTTP adaptation. Service contracts stay in `HR.Application`. Service implementation, EF-backed repository changes, and optional approved entity configuration changes stay in `HR.Infrastructure`/`HR.Domain` according to existing ownership.
- **Cookie-Based Session Authentication (II)**: PASS. Phase 10 keeps cookie auth and existing claims. No JWT, token refresh, SSO, or auth redesign is planned.
- **Service Layer Separation (III)**: PASS. Vacation role, owner, team, organization, and self-review checks are planned in `VacationRequestService`, not only in controller attributes. Controllers only extract requester employee ID and map service results.
- **Domain Integrity (IV)**: PASS. Existing vacation rules remain active: overlap, balance, active employee status, future dates, minimum notice period, transition rules, same-status idempotence, and self-approval prevention.
- **Global Error Handling (V)**: PASS. Expected access failures use existing `ServiceError.Forbidden()` and structured `{ code, message }` mapping. Existing error codes are not renamed.
- **Data Access Abstraction (VI)**: PASS. Vacation service continues to use repositories and unit of work. New query needs belong in `IVacationRequestRepository`/`VacationRequestRepository`; services do not directly reference `ApplicationDbContext`.
- **Simplicity & YAGNI (VII)**: PASS. The plan avoids new roles, permission engines, policy frameworks, generic repositories, MediatR, CQRS, new vacation workflows, and unrelated phase work. The only potential schema change is the explicitly required creator-tracking column, gated for approval.

### Post-Design Re-check

The Phase 0 and Phase 1 artifacts preserve all constitution gates. The design uses existing Phase 8 access services, extends current vacation service/repository boundaries, and creates no migration during planning. The creator-tracking schema need is documented as an approval gate, not executed here. No complexity exceptions are required.

## Technical Approach

### 1. Requester-Aware Vacation Service Contract

Update vacation service methods so all endpoint operations receive the requester employee ID:

- list vacation requests
- get vacation request detail
- create vacation request
- update vacation request status
- delete vacation request

Read methods that can fail for authorization should return `Result<T>` rather than nullable or raw payloads so the service can return existing structured `UNAUTHORIZED`, `FORBIDDEN`, or `NOT_FOUND` failures consistently. Controller changes should be limited to extracting `employee_id` from the claims principal and mapping `Result` failures.

Expected contract shape:

```csharp
Task<Result<PagedList<VacationRequestResponse>>> GetVacationRequestsAsync(
    Guid requesterEmployeeId,
    VacationRequestStatus? status,
    Guid? employeeId,
    int page,
    int pageSize,
    CancellationToken ct);

Task<Result<VacationRequestResponse>> GetVacationRequestByIdAsync(
    Guid requesterEmployeeId,
    Guid id,
    CancellationToken ct);
```

Create, status update, and delete should also include `requesterEmployeeId`. Existing tests and fakes must be updated rather than weakening the interface.

### 2. List Scope Rules

Vacation list behavior:

- `Employee`: list own requests only.
- `Manager`: unfiltered list returns active direct and indirect team-owned requests only; manager-owned requests are excluded by default.
- `Manager` with `employeeId` equal to self: returns only the manager's own vacation requests.
- `Manager` with `employeeId` for active direct/indirect report: returns that report's vacation requests.
- `Manager` with `employeeId` outside self/team scope: returns a successful empty page without probing target existence.
- `HRAdministrator` / `SystemAdministrator`: list organization-wide requests, subject to existing `status`, `employeeId`, `page`, and `pageSize` query parameters.

Scope filtering must happen before pagination and total-count calculation. The repository should support filtering by an allowed owner ID set plus existing status/employee filters.

### 3. Detail Scope Rules

Vacation detail behavior:

- `Employee`: own request only.
- `Manager`: own request and active direct/indirect report requests only.
- `HRAdministrator` / `SystemAdministrator`: any existing vacation request.
- Missing vacation request: preserve existing `404 NOT_FOUND`.
- Existing out-of-scope vacation request: return `403 FORBIDDEN`.

The service must distinguish missing records from existing out-of-scope records without returning out-of-scope data.

### 4. Create Scope Rules

Vacation creation behavior:

- `Employee`: may target only self.
- `Manager`: may target only self; manager cannot create requests for team members in Phase 10.
- `HRAdministrator` / `SystemAdministrator`: may target any employee who passes existing vacation eligibility and validation rules.

For Employee/Manager attempts to target another employee, return `403 Forbidden` before checking whether that target employee exists or whether vacation domain rules pass. For HR/System, run existing target employee eligibility and vacation validation after organization-scope authorization passes.

After approved creator-tracking storage exists, creation must set `CreatedByEmployeeId = requesterEmployeeId` for new rows. Until that migration is explicitly approved and implemented, scope hardening must not fake persisted creator metadata.

### 5. Review Scope Rules

Vacation status update behavior:

- `Employee`: forbidden from reviewing vacation requests.
- `Manager`: may approve/reject active direct and indirect team requests only.
- `HRAdministrator` / `SystemAdministrator`: may approve/reject any non-self request.
- Self-review is forbidden for all roles.

Ordering:

1. Resolve requester and target request.
2. Return `404` if the target request is missing.
3. Return `403` if the target exists but requester lacks review scope.
4. Reject self-review before transition or balance mutation.
5. Preserve same-status idempotent success with no duplicate side effects or timestamp changes.
6. Preserve transition rules, reviewer metadata, balance deduction, and refund behavior after authorization passes.

### 6. Delete Scope Rules

Vacation delete behavior:

- `Employee`: may delete own pending requests only.
- `Manager`: may delete own pending requests only; no team delete.
- `HRAdministrator` / `SystemAdministrator`: may delete any pending request.
- Missing vacation request: preserve existing `404 NOT_FOUND`.
- Existing out-of-scope vacation request: return `403 FORBIDDEN` before pending-status validation.
- Existing in-scope non-pending request: preserve existing business-rule failure.

Phase 10 preserves hard-delete behavior for pending vacation requests because current behavior removes pending rows. It does not introduce a new cancel state.

### 7. Repository Query Support

Repository changes should stay narrow and query-oriented:

- paged vacation requests by allowed owner IDs, optional status, optional requested owner filter, and page/pageSize
- detail lookup including owner/reviewer data for read responses
- tracked lookup including owner/reviewer data for review and delete mutations
- existing overlap and pending lookups remain unchanged

No repository should expose raw `ApplicationDbContext` to services.

### 8. Creator Tracking Migration Gate

The current schema cannot persist `CreatedByEmployeeId`. Phase 10 planning therefore records a required schema change, but implementation must stop for explicit approval before creating a migration.

Proposed approved migration shape:

- migration name: `AddVacationRequestCreatedByEmployee`
- table: `VacationRequests`
- column: nullable `CreatedByEmployeeId` (`uniqueidentifier`, nullable)
- relationship: optional FK to `Employees.Id`
- delete behavior: restrict/no action to avoid deleting historical vacation request creator references
- index: `IX_VacationRequests_CreatedByEmployeeId`

Backfill strategy:

- existing rows must remain valid
- do not invent verified historical creator data
- keep existing rows null unless a reliable source is approved
- new rows after approved migration and implementation must set creator to the authenticated requester
- queries/responses must handle null creator safely
- a later non-null constraint would require a separate approved migration after reliable backfill is possible

If the user does not approve this migration before implementation, implement scope hardening and leave creator tracking as a documented blocked task rather than creating an unapproved schema change.

### 9. Documentation Update

Update only stale vacation sections in `API_LIFECYCLE_TESTING_GUIDE.md` or handoff documentation during implementation if they state or imply broad vacation access. Full lifecycle retest rewrite belongs to Phase 12.

Document:

- employee own-only vacation list/detail/create/delete
- manager team-only unfiltered list, explicit self filter, self/team detail, self-only create/delete, team-only review, no self-review
- HR/System organization-wide list/detail/create/delete/review with self-review blocked
- pending-only delete remains
- no migration unless creator tracking is separately approved

## Files and Modules Likely to Change

### Existing Files

```text
HR.API/
|-- Controllers/
|   `-- VacationRequestsController.cs

HR.Application/
|-- VacationRequests/
|   `-- IVacationRequestService.cs
`-- DTOs/
    `-- VacationRequests/

HR.Infrastructure/
|-- VacationRequests/
|   `-- VacationRequestService.cs
`-- Repositories/
    |-- IVacationRequestRepository.cs
    `-- VacationRequestRepository.cs

HR.Tests/
|-- VacationRequests/
|-- Repositories/
|-- Authorization/
`-- TestInfrastructure/

API_LIFECYCLE_TESTING_GUIDE.md
AGENTS.md
```

### Potential New Files

```text
HR.Tests/VacationRequests/VacationRequestAccessScopeTests.cs
```

Only if creator tracking migration is separately approved:

```text
HR.Domain/Entities/VacationRequest.cs
HR.Infrastructure/Data/Configurations/VacationRequestConfiguration.cs
HR.Infrastructure/Data/Migrations/*_AddVacationRequestCreatedByEmployee.cs
```

### Files Expected to Remain Unchanged

```text
HR.API/Controllers/EmployeesController.cs
HR.API/Controllers/TripsController.cs
HR.API/Controllers/CompensationController.cs
HR.API/Controllers/EmployeeDocumentsController.cs
HR.API/Controllers/DashboardController.cs
HR.API/Controllers/AuditLogsController.cs
HR.Application/Authorization/IEmployeeAccessService.cs
HR.Infrastructure/Authorization/EmployeeAccessService.cs
HR.Infrastructure/Data/Migrations/   # unchanged unless migration is separately approved
```

## Data Model Changes

See [data-model.md](./data-model.md).

Expected schema changes during planning: none.

Required but approval-gated schema change:

- nullable `VacationRequest.CreatedByEmployeeId`
- optional creator navigation to `Employee`
- index and FK configuration

Current scope hardening uses existing fields:

- `VacationRequest.Id`
- `VacationRequest.EmployeeId`
- `VacationRequest.Status`
- `VacationRequest.StartDate`
- `VacationRequest.EndDate`
- `VacationRequest.WorkingDayCount`
- `VacationRequest.ReviewedByEmployeeId`
- `VacationRequest.CreatedAt`
- `Employee.Id`
- `Employee.ManagerId`
- `Employee.Role`
- `Employee.Status`
- `Employee.IsDeleted`

## Internal and External Contracts

See [phase10-vacation-scope-contract.md](./contracts/phase10-vacation-scope-contract.md).

## API and Route Changes

Existing vacation routes remain stable:

- `GET /api/vacationrequests`
- `GET /api/vacationrequests/{id}`
- `POST /api/vacationrequests`
- `PUT /api/vacationrequests/{id}/status`
- `DELETE /api/vacationrequests/{id}`

Phase 10 changes authorization outcomes and filtering only. It does not add routes or intentionally change successful response DTO fields. If creator metadata is later added to responses, it must be additive and approved with the migration-backed creator-tracking work.

## Validation and Error Handling

- Missing or invalid `employee_id` claim maps to existing structured `401 Unauthorized`.
- Authenticated but out-of-scope access maps to `403 Forbidden` with `{ code, message }`.
- Missing vacation request IDs preserve existing `404 NOT_FOUND`.
- List filters that narrow outside allowed scope return an empty page rather than probing target existence.
- Existing business-rule, validation, and conflict behavior remains unchanged after authorization passes.
- Authorization checks for existing out-of-scope targets must occur before pending-status, transition, balance, overlap, notice, or target eligibility validation.
- Existing error codes must not be normalized as part of Phase 10.

## Testing and Check Strategy

### Automated Checks

1. `dotnet restore .\HR.slnx`
2. `dotnet build .\HR.slnx -c Release`
3. `dotnet test .\HR.slnx -c Release --no-build`
4. Focused vacation access tests for:
   - Employee list returns own requests only
   - Employee detail own allowed and other denied
   - Employee create self allowed and other forbidden before target lookup
   - Employee review forbidden
   - Employee delete own pending allowed and other denied
   - Manager unfiltered list returns active direct/indirect team requests only and excludes manager self
   - Manager `employeeId` self filter returns own requests only
   - Manager list filter for team member returns that team member's requests
   - Manager list filter outside self/team returns empty page
   - Manager detail self/team allowed and outside-team denied
   - Manager create self allowed and team/outside employee forbidden
   - Manager review team request allowed
   - Manager review self and outside-team request denied before domain validation
   - Manager delete own pending allowed and team delete denied
   - HR/System list and detail organization-wide
   - HR/System create on behalf of eligible employee allowed
   - HR/System self-review denied
   - HR/System delete any pending request allowed
   - Missing request ID returns 404 while existing out-of-scope request returns 403
   - Existing out-of-scope request returns 403 before pending/transition/balance validation
   - Same-status vacation review remains idempotent no-op with no duplicate side effects
   - Existing overlap, notice, balance, transition, and delete business-rule tests still pass
5. EF check:
   `dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj`

### Static Checks

```powershell
rg -n "GetVacationRequestsAsync|GetVacationRequestByIdAsync|CreateVacationRequestAsync|UpdateVacationStatusAsync|DeleteVacationRequestAsync" .\HR.API .\HR.Application .\HR.Infrastructure .\HR.Tests
rg -n "HR.Infrastructure" .\HR.Application
git status --short .\HR.Infrastructure\Data\Migrations
git diff --check
```

Expected results:

- All vacation service call sites are updated for requester-aware signatures.
- `HR.Application` still does not reference `HR.Infrastructure`.
- No new migration files exist unless the migration was separately approved.
- `git diff --check` reports no whitespace errors.

## Manual Smoke Validation

Use [quickstart.md](./quickstart.md) after implementation.

Smoke coverage:

- login as normal employee, manager, HR administrator, and system administrator
- vacation list/detail/create/review/delete access matrix
- manager self-filter behavior
- self-review blocked for all elevated roles
- pending-only delete still enforced
- existing vacation validations still work after authorization passes
- no employee/trip behavior changes

## Migration and Backfill Strategy

No migration is created by this planning step.

Phase 10 proves that persisted creator tracking cannot be represented by the current schema because `VacationRequest` has owner and reviewer fields but no creator field. The approved design is nullable creator metadata:

- `CreatedByEmployeeId` nullable for existing rows
- FK to `Employees.Id`
- index for query/reporting support
- new rows set creator to authenticated requester after migration is approved and applied
- existing rows are not assigned fake creator values
- null creator rows remain compatible with reads and responses

Implementation must stop before creating the migration and ask for approval with:

1. business rule requiring the schema change
2. affected table/column/index/FK
3. existing data impact
4. migration name
5. tests proving new rows record creator and existing null creator rows remain safe

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Vacation list leaks out-of-scope counts or rows | Apply role/scope filtering before pagination and cover counts/items in tests. |
| Manager list accidentally includes manager self | Treat unfiltered manager list as team-only and add explicit self-filter tests. |
| Employee/Manager create can probe employee existence | Reject non-self create attempts before target lookup for Employee/Manager roles. |
| Out-of-scope review/delete reveals target state | Return 403 before pending/transition/balance validation for existing out-of-scope targets. |
| Self-review regression | Keep self-review forbidden after target exists and before transition/balance mutation. |
| Same-status idempotence regresses | Preserve no-op success with no duplicate side effects or timestamp changes. |
| Creator tracking implemented without approval | Treat `CreatedByEmployeeId` as migration-gated; stop before migration creation. |
| Controller-only authorization creeps in | Keep business scope decisions in `VacationRequestService`; controllers pass requester ID only. |
| Phase 11 trip work pulled in | Keep trip files out of scope except static verification that they did not change. |

## Dependencies on Previous Phases

- **Phase 5**: Vacation business rules, status transitions, same-status idempotence, overlap, notice, balance, and self-approval prevention.
- **Phase 6**: `AddApplication()` / `AddInfrastructure(configuration)` registration ownership and clean host composition.
- **Phase 7**: Single employee role model and role-protected HR operational modules.
- **Phase 8**: Reusable employee access context, self/team/organization scope definitions, and direct-plus-indirect manager team scope.
- **Phase 9**: Employee endpoint hardening and missing-vs-out-of-scope convention.

## Out of Scope

- Employee access hardening.
- Trip ownership/scope hardening.
- Swagger/OpenAPI response documentation pass.
- New vacation types, accrual/carryover/payroll rules, notification workflow, or frontend UI.
- Changing working day calculations or vacation balance algorithms unless a direct Phase 10 scope test exposes a real defect that must be reported before patching.
- Authentication redesign, JWT, token refresh, SSO, or public setup endpoints.
- Compensation, document, dashboard, attendance, audit-log, or bootstrap behavior changes.
- Database schema changes unless separately approved after creator-tracking migration analysis.

## Project Structure

### Documentation (this feature)

```text
specs/010-vacation-scope-hardening/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- phase10-vacation-scope-contract.md
|-- checklists/
|   |-- requirements.md
|   `-- vacation-scope.md
`-- tasks.md                 # generated later by /speckit-tasks
```

### Source Code (repository root)

```text
HR.API/
`-- Controllers/
    `-- VacationRequestsController.cs

HR.Application/
|-- VacationRequests/
|   `-- IVacationRequestService.cs
`-- DTOs/
    `-- VacationRequests/

HR.Infrastructure/
|-- VacationRequests/
|   `-- VacationRequestService.cs
`-- Repositories/
    |-- IVacationRequestRepository.cs
    `-- VacationRequestRepository.cs

HR.Tests/
|-- VacationRequests/
|-- Repositories/
|-- Authorization/
`-- TestInfrastructure/
```

**Structure Decision**: Continue the existing five-project layered solution. Phase 10 is a targeted vacation request access hardening change and should not add architecture layers or new source projects.

## Complexity Tracking

No constitution violations or complexity exceptions are required.
