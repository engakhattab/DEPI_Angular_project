# Implementation Plan: Phase 11 - Trip Ownership and Scope Hardening

**Branch**: `011-trip-ownership-scope-hardening` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/011-trip-ownership-scope-hardening/spec.md`

## Summary

Harden trip list, detail, create, and delete operations using the completed Phase 8 authorization scope foundation. Employees may access and create only their own trips. Managers may access and create trips for themselves and active direct/indirect reports. HR administrators and system administrators retain organization-wide trip access for eligible employees.

The implementation should keep existing trip routes, cookie authentication, claims, pagination envelope, structured errors, current hard-delete behavior, existing trip validation rules, and successful response compatibility. Controllers should remain thin by extracting the current employee ID claim and passing it to requester-aware service methods. Authorization and scope checks must live in the service layer and run before mutation or out-of-scope domain validation.

Persisted requester tracking is required by the Phase 11 spec, but the current trip schema has only one employee reference. Phase 11 treats existing `RequestedByEmployeeId` values as traveler data for compatibility and requires a separate nullable requester reference after explicit migration approval. No migration is created during planning.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core 8, ASP.NET Core Identity, Entity Framework Core 8, SQL Server provider, System.Text.Json, Swashbuckle

**Storage**: Existing SQL Server database through `ApplicationDbContext`. Scope hardening can use existing trip and employee tables for traveler ownership. Persisted requester tracking requires a separately approved nullable `Trips.RequesterEmployeeId` migration.

**Testing**: xUnit, SQLite integration tests through existing test infrastructure, controller tests for claim propagation and response compatibility, focused trip access matrix tests, repository scope/paging tests, EF pending-model check

**Target Platform**: Windows or Linux server hosted with Kestrel or IIS

**Project Type**: Layered ASP.NET Core Web API

**Performance Goals**: Trip list scope filtering must happen before pagination and total-count calculation. Manager team filtering should reuse Phase 8 visible employee IDs and remain suitable for current project scale.

**Constraints**: Preserve cookie-based sessions, existing login/current-user behavior, current trip routes, existing `requestedByEmployeeId` request field, successful response compatibility, pagination envelope, structured `{ code, message }` error shape, existing trip validation error conventions, Phase 8 scope behavior, Phase 9 employee hardening, Phase 10 vacation behavior, and Phase 6 DI ownership. Do not change employee, vacation, compensation, document, attendance, dashboard, audit, bootstrap, Swagger, or frontend behavior. Do not create migrations without separate explicit approval.

**Scale/Scope**: One focused trip ownership and access hardening slice for `TripsController`, `ITripService`, `TripService`, `ITripRepository`/`TripRepository`, `Trip` entity planning, trip DTO compatibility, trip access tests, and narrowly scoped manual validation documentation.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Layered Architecture (I)**: PASS. Controller changes remain thin HTTP adaptation. Service contracts stay in `HR.Application`. EF-backed repository/service implementation and any approved entity configuration changes stay in `HR.Infrastructure`/`HR.Domain`.
- **Cookie-Based Session Authentication (II)**: PASS. Phase 11 keeps cookie auth and existing claims. No JWT, token refresh, SSO, or auth redesign is planned.
- **Service Layer Separation (III)**: PASS. Trip role, owner, team, organization, requester, and traveler checks are planned in `TripService`, not controller business logic. Controllers only extract requester employee ID and map service results.
- **Domain Integrity (IV)**: PASS. Existing trip rules remain active: active/non-deleted target employee eligibility, future trip date, working-day trip date, required fields, and generated codes.
- **Global Error Handling (V)**: PASS. Expected access failures use existing `ServiceError` and structured `{ code, message }` mapping. Existing error codes are not renamed.
- **Data Access Abstraction (VI)**: PASS. Trip service continues to use repositories and unit of work. New query needs belong in `ITripRepository`/`TripRepository`; services do not directly reference `ApplicationDbContext`.
- **Simplicity & YAGNI (VII)**: PASS. The plan avoids new roles, permission engines, policy frameworks, generic repositories, trip workflow/status features, soft-delete conversion, audit expansion, and unrelated phase work. The only schema change is the explicitly required requester-tracking column, gated for approval.

### Post-Design Re-check

The Phase 0 and Phase 1 artifacts preserve all constitution gates. The design uses the existing Phase 8 access service, extends current trip service/repository boundaries, keeps successful DTO compatibility, and creates no migration during planning. The requester-tracking schema need is documented as an approval gate, not executed here. No complexity exceptions are required.

## Technical Approach

### 1. Requester-Aware Trip Service Contract

Update trip service methods so all endpoint operations receive the requester employee ID:

- list trips
- get trip detail
- create trip
- delete trip

Read methods that can fail for authorization should return `Result<T>` rather than nullable or raw payloads so the service can return existing structured `UNAUTHORIZED`, `FORBIDDEN`, or `NOT_FOUND` failures consistently. Controller changes should be limited to extracting `employee_id` from the claims principal and mapping `Result` failures.

Expected contract shape:

```csharp
Task<Result<PagedList<TripResponse>>> GetTripsAsync(
    Guid requesterEmployeeId,
    Guid? travelerEmployeeId,
    int page,
    int pageSize,
    CancellationToken ct);

Task<Result<TripResponse>> GetTripByIdAsync(
    Guid requesterEmployeeId,
    Guid id,
    CancellationToken ct);
```

Create and delete should also include `requesterEmployeeId`. Existing tests and fakes must be updated rather than weakening the interface.

### 2. List Scope Rules

Trip list behavior:

- `Employee`: own trips only.
- `Manager`: own trips plus active direct and indirect team trips.
- `HRAdministrator` / `SystemAdministrator`: organization-wide trips.
- Optional additive `travelerEmployeeId` filter narrows results within the requester's allowed scope.
- Out-of-scope `travelerEmployeeId` filter returns a successful empty page and does not reveal whether that employee exists.

Scope filtering must happen before pagination and total-count calculation. The repository should support filtering by an allowed traveler ID set plus the optional traveler filter.

### 3. Detail Scope Rules

Trip detail behavior:

- `Employee`: own trip only.
- `Manager`: own trip and active direct/indirect report trips only.
- `HRAdministrator` / `SystemAdministrator`: any existing trip.
- Missing trip ID: preserve existing `404 NOT_FOUND`.
- Existing out-of-scope trip ID: return `403 FORBIDDEN` with no trip data.

The service must distinguish missing records from existing out-of-scope records without returning out-of-scope data.

### 4. Create Scope Rules

Trip creation behavior:

- The existing request body field `requestedByEmployeeId` remains accepted and is interpreted as the target traveler ID for compatibility.
- The authenticated requester is never trusted from the request body.
- `Employee`: may target only self.
- `Manager`: may target self or active direct/indirect reports.
- `HRAdministrator` / `SystemAdministrator`: may target any employee who passes existing trip eligibility and validation rules.

For Employee/Manager attempts to target another employee outside permitted scope, return `403 Forbidden` before creating trip data. For HR/System, run existing target employee eligibility and trip validation after organization-scope authorization passes.

After approved requester-tracking storage exists, creation must set `RequesterEmployeeId = requesterEmployeeId` for new rows while preserving the existing traveler assignment in `RequestedByEmployeeId`. Until that migration is explicitly approved and implemented, scope hardening must not fake persisted requester metadata.

### 5. Delete Scope Rules

Trip delete behavior:

- `Employee`: may hard-delete own trips only after authorization succeeds.
- `Manager`: may hard-delete own or active team trips only after authorization succeeds.
- `HRAdministrator` / `SystemAdministrator`: may hard-delete any trip after authorization succeeds.
- Missing trip ID: preserve existing `404 NOT_FOUND`.
- Existing out-of-scope trip ID: return `403 FORBIDDEN` before mutation.

Phase 11 preserves current hard-delete behavior for in-scope trips. It does not introduce trip cancellation, soft deletion, audit behavior, or approval workflow.

### 6. Repository Query Support

Repository changes should stay narrow and query-oriented:

- paged trips by allowed traveler IDs, optional traveler filter, and page/pageSize
- detail lookup including traveler and requester data for read responses after approved requester storage exists
- tracked lookup including traveler and requester data for delete mutations
- existing add/remove behavior remains unchanged except for setting requester metadata after approved migration

No repository should expose raw `ApplicationDbContext` to services.

### 7. Requester Tracking Migration Gate

The current schema cannot persist a real requester separate from the traveler. Phase 11 planning therefore records a required schema change, but implementation must stop for explicit approval before creating a migration.

Proposed approved migration shape:

- migration name: `AddTripRequesterEmployee`
- table: `Trips`
- column: nullable `RequesterEmployeeId` (`uniqueidentifier`, nullable)
- relationship: optional FK to `Employees.Id`
- delete behavior: restrict/no action to avoid deleting historical trip requester references
- index: `IX_Trips_RequesterEmployeeId`

Compatibility/backfill strategy:

- keep existing `Trips.RequestedByEmployeeId` and `TripResponse.RequestedByEmployeeId` as compatibility traveler data
- do not rename or repurpose public response fields in Phase 11
- existing rows remain valid with `RequesterEmployeeId = null`
- do not invent verified historical requester data
- new rows after approved migration and implementation set requester to the authenticated employee
- queries and responses handle null requester safely
- a later non-null constraint requires a separate approved migration after reliable backfill is possible

If the user does not approve this migration before implementation, implementation must stop at the migration gate because persisted requester storage is a Phase 11 requirement.

### 8. Documentation Update

Update only stale trip sections in manual validation or handoff documentation during implementation if they state or imply broad trip access. Full lifecycle retest rewrite belongs to Phase 12, and Swagger/OpenAPI documentation pass belongs to Phase 13.

Document:

- employee own-only trip list/detail/create/delete
- manager own/team trip list/detail/create/delete
- HR/System organization-wide list/detail/create/delete
- existing `requestedByEmployeeId` request field now means target traveler
- requester storage requires explicit migration approval
- current hard-delete behavior remains after authorization

## Files and Modules Likely to Change

### Existing Files

```text
HR.API/
|-- Controllers/
|   `-- TripsController.cs

HR.Application/
|-- Transportation/
|   `-- ITripService.cs
`-- DTOs/
    `-- Transportation/

HR.Domain/
`-- Entities/
    `-- Trip.cs                  # only after explicit migration approval

HR.Infrastructure/
|-- Transportation/
|   `-- TripService.cs
|-- Repositories/
|   |-- ITripRepository.cs
|   `-- TripRepository.cs
`-- Data/
    `-- Configurations/
        `-- TripConfiguration.cs # only after explicit migration approval

HR.Tests/
|-- Transportation/
|-- Repositories/
|-- Authorization/
`-- TestInfrastructure/

API_LIFECYCLE_TESTING_GUIDE.md   # narrow trip wording only if stale
AGENTS.md
```

### Potential New Files

```text
HR.Tests/Transportation/TripAccessScopeTests.cs
```

Only if requester tracking migration is separately approved:

```text
HR.Infrastructure/Data/Migrations/*_AddTripRequesterEmployee.cs
```

### Files Expected to Remain Unchanged

```text
HR.API/Controllers/EmployeesController.cs
HR.API/Controllers/VacationRequestsController.cs
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

- nullable `Trip.RequesterEmployeeId`
- optional requester navigation to `Employee`
- index and FK configuration

Current scope hardening uses existing fields:

- `Trip.Id`
- `Trip.RequestedByEmployeeId` as compatibility traveler ID
- `Trip.RequestedBy` as compatibility traveler navigation
- `Trip.TripDate`
- `Trip.CreatedAt`
- `Trip.TripCode`
- `Trip.RequestCode`
- `Employee.Id`
- `Employee.ManagerId`
- `Employee.Role`
- `Employee.Status`
- `Employee.IsDeleted`

## Internal and External Contracts

See [phase11-trip-ownership-scope-contract.md](./contracts/phase11-trip-ownership-scope-contract.md).

## API and Route Changes

Existing trip routes remain stable:

- `GET /api/trips`
- `GET /api/trips/{id}`
- `POST /api/trips`
- `DELETE /api/trips/{id}`

Phase 11 changes authorization outcomes and filtering only. It may add an optional `travelerEmployeeId` query filter to `GET /api/trips`; existing clients that omit it remain compatible. It does not add routes or change the existing create request field. If requester/traveler metadata is added to responses after approved migration, it must be additive.

## Validation and Error Handling

- Missing or invalid `employee_id` claim maps to existing structured `401 Unauthorized`.
- Authenticated but out-of-scope access maps to `403 Forbidden` with `{ code, message }`.
- Missing trip IDs preserve existing `404 NOT_FOUND`.
- List filters that narrow outside allowed scope return an empty page rather than probing target existence.
- Existing trip business-rule and validation behavior remains unchanged after authorization passes.
- Authorization checks for existing out-of-scope targets must occur before mutation or target eligibility/date validation.
- Existing error codes must not be normalized as part of Phase 11.

## Testing and Check Strategy

### Automated Checks

1. `dotnet restore .\HR.slnx`
2. `dotnet build .\HR.slnx -c Release`
3. `dotnet test .\HR.slnx -c Release --no-build`
4. Focused trip access tests for:
   - Employee list returns own trips only
   - Employee detail own allowed and other denied
   - Employee create self allowed and other forbidden
   - Employee delete own allowed and other denied
   - Manager list returns own plus active direct/indirect team trips only
   - Manager list excludes peer, unrelated, soft-deleted report, and terminated report trips
   - Manager detail self/team allowed and outside-team denied
   - Manager create self/team allowed and outside-scope employee forbidden
   - Manager delete own/team allowed and outside-scope denied
   - HR/System list and detail organization-wide
   - HR/System create on behalf of eligible employee allowed
   - HR/System delete any existing trip allowed
   - Missing trip ID returns 404 while existing out-of-scope trip returns 403
   - Out-of-scope list filter returns empty page with normal pagination shape
   - Suspended requester behavior matches Phase 8 decision
   - Existing future-date, working-day, active employee, and code-generation tests still pass
   - New trip rows set requester metadata only after approved migration exists
   - Existing rows with null requester metadata remain readable after approved migration exists
5. EF check:
   `dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj`

### Static Checks

```powershell
rg -n "GetTripsAsync|GetTripByIdAsync|CreateTripAsync|DeleteTripAsync" .\HR.API .\HR.Application .\HR.Infrastructure .\HR.Tests
rg -n "HR.Infrastructure" .\HR.Application
git status --short .\HR.Infrastructure\Data\Migrations
git diff --check
```

Expected results:

- All trip service call sites are updated for requester-aware signatures.
- `HR.Application` still does not reference `HR.Infrastructure`.
- No new migration files exist unless the requester migration was separately approved.
- `git diff --check` reports no whitespace errors.

## Manual Smoke Validation

Use [quickstart.md](./quickstart.md) after implementation.

Smoke coverage:

- login as normal employee, manager, HR administrator, and system administrator
- trip list/detail/create/delete access matrix
- out-of-scope list filter empty-page behavior
- missing-vs-out-of-scope detail/delete behavior
- existing trip date and working-day validations still work after authorization passes
- no employee/vacation behavior changes

## Migration and Backfill Strategy

No migration is created by this planning step.

Phase 11 proves that persisted requester tracking cannot be represented by the current schema because `Trip` currently has one employee reference. The approved design is nullable requester metadata:

- `RequesterEmployeeId` nullable for existing rows
- FK to `Employees.Id`
- index for query/reporting support
- new rows set requester to authenticated employee after migration is approved and applied
- existing `RequestedByEmployeeId` values remain traveler metadata
- existing rows are not assigned fake requester values
- null requester rows remain compatible with reads and responses

Implementation must stop before creating the migration and ask for approval with:

1. business rule requiring the schema change
2. affected table/column/index/FK
3. existing data impact
4. migration name
5. tests proving new rows record requester and existing null requester rows remain safe

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Trip list leaks out-of-scope counts or rows | Apply role/scope filtering before pagination and cover counts/items in tests. |
| Existing `requestedByEmployeeId` is confused with authenticated requester | Document it as compatibility traveler data and store authenticated requester separately after approved migration. |
| Employee/Manager create can impersonate another traveler | Reject outside-scope target traveler before creation and cover with role tests. |
| Out-of-scope delete mutates or reveals target details | Return 403 for existing out-of-scope trips before `Remove` or domain validation. |
| Historical requester data is fabricated | Keep requester nullable for existing rows and do not backfill without reliable approved source. |
| Controller-only authorization creeps in | Keep business scope decisions in `TripService`; controllers pass requester ID only. |
| Phase 10 vacation behavior regresses | Keep vacation files out of scope except validation that they did not change. |
| Migration is created without approval | Treat `RequesterEmployeeId` as migration-gated; stop before migration creation. |

## Dependencies on Previous Phases

- **Phase 6**: `AddApplication()` / `AddInfrastructure(configuration)` registration ownership and clean host composition.
- **Phase 7**: Single employee role model and role-protected HR operational modules.
- **Phase 8**: Reusable employee access context, self/team/organization scope definitions, and direct-plus-indirect manager team scope.
- **Phase 9**: Employee endpoint hardening and missing-vs-out-of-scope convention.
- **Phase 10**: Vacation scope hardening and migration-gated creator-tracking precedent.

## Out of Scope

- Employee access hardening.
- Vacation request access, creator tracking, or review rules.
- Swagger/OpenAPI response documentation pass.
- Lifecycle documentation full retest.
- New trip approval, rejection, cancellation, billing, notification, scheduling, or audit workflow.
- Authentication redesign, JWT, token refresh, SSO, or public setup endpoints.
- Compensation, document, dashboard, attendance, audit-log, bootstrap, or frontend behavior changes.
- Database schema changes unless separately approved after requester-tracking migration analysis.

## Project Structure

### Documentation (this feature)

```text
specs/011-trip-ownership-scope-hardening/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- phase11-trip-ownership-scope-contract.md
|-- checklists/
|   |-- requirements.md
|   `-- trip-ownership.md
`-- tasks.md                 # generated later by /speckit-tasks
```

### Source Code (repository root)

```text
HR.API/
`-- Controllers/
    `-- TripsController.cs

HR.Application/
|-- Transportation/
|   `-- ITripService.cs
`-- DTOs/
    `-- Transportation/

HR.Infrastructure/
|-- Transportation/
|   `-- TripService.cs
`-- Repositories/
    |-- ITripRepository.cs
    `-- TripRepository.cs

HR.Domain/
`-- Entities/
    `-- Trip.cs

HR.Tests/
|-- Transportation/
|-- Repositories/
|-- Authorization/
`-- TestInfrastructure/
```

**Structure Decision**: Continue the existing five-project layered solution. Phase 11 is a targeted trip access hardening change and should not add architecture layers or new source projects.

## Complexity Tracking

No constitution violations or complexity exceptions are required.
