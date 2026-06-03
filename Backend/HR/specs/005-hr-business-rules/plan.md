# Implementation Plan: Phase 5 - HR Business Logic Improvements

**Branch**: `005-hr-business-rules` | **Date**: 2026-06-03 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/005-hr-business-rules/spec.md`

## Summary

Activate the Phase 5 HR business rules on top of the Phase 4 repository and entity-configuration boundaries. Enforce vacation overlap, balance, working-day notice, reviewer, and status-transition rules; preserve employee history through termination and soft deletion; immediately deny access for terminated or removed employees; add trip employee ownership and working-day validation; and return department employee counts. Keep existing routes, cookie authentication, claims, pagination envelopes, structured error payloads, and Phase 4 repository/service layering intact except for the explicitly approved Phase 5 request/response field additions.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core 8, Entity Framework Core 8.0.20, ASP.NET Core Identity, Swashbuckle

**Storage**: Existing SQL Server schema through `ApplicationDbContext`; one new EF Core migration for Phase 5 columns and relationships; SQLite in-memory storage for focused automated tests

**Testing**: xUnit, `dotnet build`, `dotnet test`, EF model/migration checks, repository/service regression tests, authenticated manual API checks

**Target Platform**: Windows or Linux server hosted with Kestrel or IIS

**Project Type**: Layered ASP.NET Core Web API

**Performance Goals**: Preserve page normalization with max page size 100; keep vacation overlap, employee-session validation, and department-count queries bounded and index-aware for current HR scale

**Constraints**: Preserve existing routes, cookie-auth mechanism, claims, structured error format, unrelated HTTP statuses, and pagination shape; do not perform Phase 6 DI ownership cleanup; do not introduce Phase 7 RBAC, attendance, compensation, documents, dashboards, general audit logs, or role-gated review rules

**Scale/Scope**: Four domain entities, one login identity association, five infrastructure services, four repository contracts, cookie validation behavior, one migration, API DTO additions, and focused regression coverage for all Phase 5 rules

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Layered Architecture (I)**: PASS. The existing five-project structure remains. Domain entities and enums stay in `HR.Domain`; DTOs and service interfaces stay in `HR.Application`; EF Core, Identity, repositories, and service implementations that need persistence remain in `HR.Infrastructure`; HTTP adapters remain in `HR.API`.
- **Cookie-Based Session Authentication (II)**: PASS. ASP.NET Core cookie authentication remains the only authentication mechanism. Phase 5 adds immediate session rejection through cookie validation and login checks without JWTs or token refresh flows.
- **Service Layer Separation (III)**: PASS. Controllers remain thin adapters. New rule evaluation belongs in service implementations and small reusable rule helpers, with writes returning `Result<T>` or `Result` and reads returning DTOs or `PagedList<T>`.
- **Domain Integrity (IV)**: PASS. Phase 5 is the activation phase for the previously staged business rules. Vacation, employee lifecycle, duplicate-email, manager-chain, trip, and soft-delete rules are enforced before mutation and before database save.
- **Global Error Handling (V)**: PASS. Expected Phase 5 failures use existing `ServiceError` categories and the `{ "code": "...", "message": "..." }` response shape. Compatibility codes are not renamed in this phase.
- **Data Access Abstraction (VI)**: PASS. Services continue to use tailored repositories and `IUnitOfWork`; no service references `ApplicationDbContext` directly. EF mapping changes remain in per-entity `IEntityTypeConfiguration<T>` classes.
- **Simplicity & YAGNI (VII)**: PASS. The design adds only the fields, helpers, repository methods, and cookie validation needed for Phase 5. It avoids RBAC, a generic repository base class, MediatR, CQRS, general audit logging, and Phase 6 DI restructuring.

### Post-Design Re-check

The design artifacts preserve the gates. The only `Program.cs` change expected is cookie-validation wiring required for immediate access revocation; service/repository registrations remain in the existing `HR.Infrastructure.DependencyInjection` extension until Phase 6.

## Technical Approach

### 1. Add Phase 5 Persistent State With One Migration

Add one EF Core migration for the Phase 5 schema additions. Do not edit existing migration files.

Expected model additions:

- `Employee.VacationBalanceDays` with default value `21`
- `Employee.IsDeleted` with default value `false`
- `Employee.TerminatedAt` nullable `DateTimeOffset`
- `VacationRequest.WorkingDayCount` to preserve the approved/restored balance amount
- `VacationRequest.ReviewedByEmployeeId`, `ReviewedBy`, and `ReviewedAt`
- Nullable `Trip.RequestedByEmployeeId` and `RequestedBy` for existing-row compatibility

Keep existing enum values for `EmployeeStatus` and `VacationRequestStatus`. Do not introduce roles or advanced audit entities.

Trip requester migration strategy: existing trip rows must not break the migration. Do not invent fake requester data for existing trips. Prefer a nullable `RequestedByEmployeeId` column for existing rows unless the current data model provides a reliable requester source. New Phase 5 trip creation must require a requester through the API/service layer. Repository queries, DTO mapping, compatibility checks, and manual regression must handle existing rows with null requester safely. A non-null database constraint requires a separate approved migration after reliable backfill is possible.

### 2. Centralize Date and Working-Day Rules

Add a small reusable working-day helper under `HR.Infrastructure/BusinessRules/` for Sunday-through-Thursday business days. Use it for:

- Vacation requested working-day count
- Three-full-working-day notice validation, excluding submission day and start day
- Trip non-working-day rejection

Inject .NET 8 `TimeProvider` through infrastructure DI so tests can validate past-date and notice behavior deterministically. Use the existing UTC-oriented time style from the project unless a later feature explicitly introduces business-time-zone configuration.

### 3. Enforce Vacation Submission and Review Rules

Extend `VacationRequestService` and `IVacationRequestRepository` so creation checks:

- Employee exists, is active, and is not soft-deleted
- Start date is not in the past
- End date is not before start date
- At least three full working days exist between submission date and vacation start
- Requested working-day count is within the employee's available balance
- No pending or approved request overlaps the requested date range

Extend status updates so the service receives the authenticated reviewer employee id from the API layer. It must reject self-review, allow any authenticated non-requester, enforce the transition table, record reviewer audit details, deduct balance exactly once on first approval, and restore the same stored `WorkingDayCount` exactly once when an approved request becomes rejected. Hard deletion is allowed only for pending requests.

Same-status vacation updates are idempotent no-op successes. They do not update reviewer fields, `UpdatedAt`, or balances again, and they are not rejected merely because the requested status is already current. Invalid transitions to a different status remain rejected.

### 4. Enforce Employee Lifecycle Rules

Extend `EmployeeService` and `IEmployeeRepository` so employee create/update/delete checks:

- Employee number remains immutable after creation
- Active employee duplicate email is rejected case-insensitively, excluding soft-deleted profiles
- Employee status follows the `Active -> Suspended/Terminated`, `Suspended -> Active/Terminated`, and terminal `Terminated` state machine
- Same-status employee updates are idempotent no-op successes that do not update `TerminatedAt`, reject pending vacations again, or repeat access-revocation work
- Direct and indirect circular manager chains are rejected
- Cross-department manager assignments are allowed and recorded with an infrastructure log warning
- Termination records `TerminatedAt`, rejects pending vacation requests, and revokes access immediately
- Deletion becomes soft deletion: retain the employee and associated Identity user, set `IsDeleted = true`, set status to `Terminated`, set `TerminatedAt`, clear direct reports as needed, and reject pending vacation requests

Normal employee list/detail results exclude `IsDeleted` employees. Terminated employees that are not soft-deleted remain visible in normal employee results.

### 5. Reject Access for Terminated or Soft-Deleted Employees

Update `AuthService.ValidateCredentialsAsync` to deny sign-in when the linked employee is terminated or soft-deleted. Add a narrow session-validation service used from cookie `OnValidatePrincipal` to reject existing authenticated sessions immediately when the `employee_id` claim no longer maps to an active, non-deleted employee.

Keep existing login route, cookie settings, claim names, `/api/auth/me`, JSON `401`/`403` behavior, and compatibility error codes.

### 6. Add Trip Employee Ownership and Date Rules

Extend trip DTOs, entity mapping, repository loading, and `TripService` so every new trip identifies `RequestedByEmployeeId`. Creation must reject missing, suspended, terminated, or soft-deleted employees and reject dates in the past or on Friday/Saturday. Existing trip list/detail routes remain unchanged, return the added nullable requester fields, and safely handle pre-Phase-5 rows with no requester.

### 7. Return Department Employee Counts

Extend `DepartmentResponse` with `EmployeeCount`. Repository/service projections should count current non-deleted employees. Terminated-but-not-soft-deleted employees remain counted because the spec only excludes soft-deleted profiles.

## Files and Modules Likely to Change

### Existing Files

```text
HR.Domain/
|-- Entities/Employee.cs
|-- Entities/VacationRequest.cs
`-- Entities/Trip.cs

HR.Application/
|-- Auth/IAuthService.cs
|-- DTOs/Departments/DepartmentResponse.cs
|-- DTOs/Employees/EmployeeResponse.cs
|-- DTOs/Transportation/TripCreateRequest.cs
|-- DTOs/Transportation/TripResponse.cs
|-- DTOs/VacationRequests/VacationRequestResponse.cs
|-- DTOs/VacationRequests/VacationRequestStatusUpdateRequest.cs
|-- Employees/IEmployeeService.cs
|-- Transportation/ITripService.cs
`-- VacationRequests/IVacationRequestService.cs

HR.API/
|-- Controllers/AuthController.cs
|-- Controllers/VacationRequestsController.cs
`-- Program.cs

HR.Infrastructure/
|-- Auth/AuthService.cs
|-- Data/Configurations/EmployeeConfiguration.cs
|-- Data/Configurations/VacationRequestConfiguration.cs
|-- Data/Configurations/TripConfiguration.cs
|-- Departments/DepartmentService.cs
|-- Employees/EmployeeService.cs
|-- Repositories/DepartmentRepository.cs
|-- Repositories/EmployeeRepository.cs
|-- Repositories/IDepartmentRepository.cs
|-- Repositories/IEmployeeRepository.cs
|-- Repositories/ITripRepository.cs
|-- Repositories/IVacationRequestRepository.cs
|-- Repositories/TripRepository.cs
|-- Repositories/VacationRequestRepository.cs
|-- Transportation/TripService.cs
|-- VacationRequests/VacationRequestService.cs
`-- DependencyInjection.cs

HR.Tests/
|-- Auth/
|-- Employees/
|-- Repositories/
|-- Transportation/
`-- VacationRequests/
```

### New Files

```text
HR.Infrastructure/
|-- Auth/
|   |-- EmployeeSessionValidator.cs
|   `-- IEmployeeSessionValidator.cs
|-- BusinessRules/
|   `-- WorkingDayCalendar.cs
`-- Data/Migrations/
    `-- <timestamp>_Phase5HrBusinessRules.cs

HR.Tests/
|-- BusinessRules/WorkingDayCalendarTests.cs
|-- Departments/DepartmentServiceBusinessRuleTests.cs
|-- Transportation/TripServiceBusinessRuleTests.cs
`-- VacationRequests/VacationRequestServiceBusinessRuleTests.cs
```

### Files Expected to Remain Unchanged

```text
HR.Domain/Enums/EmployeeStatus.cs
HR.Domain/Enums/VacationRequestStatus.cs
HR.Shared/
HR.Infrastructure/Data/ApplicationDbContext.cs
```

`ApplicationDbContext` should still contain only `base.OnModelCreating(builder)` followed by `ApplyConfigurationsFromAssembly`.

## Data Model Changes

See [data-model.md](./data-model.md). Phase 5 adds persistent fields and relationships, so an EF Core migration is required. Existing historical data should receive defaults for vacation balance and soft-delete state.

## API and Route Changes

No route, verb, cookie, or claim names are changed. Public DTOs receive additive fields required by the spec:

- `EmployeeResponse`: vacation balance, deletion marker, termination time
- `VacationRequestResponse`: working-day count, reviewer id/name, review time
- `VacationRequestStatusUpdateRequest`: reviewer context is supplied from authenticated claims by the API layer, not trusted from the request body
- `TripCreateRequest` and `TripResponse`: requesting employee id and display name
- `DepartmentResponse`: current employee count

See [contracts/api-contract.md](./contracts/api-contract.md).

## Validation and Error Handling

- Preserve the existing structured error response format.
- Use `ServiceError.BusinessRule` for business-rule violations that map to `422`.
- Use `ServiceError.Conflict` for duplicate active-email conflicts.
- Use `ServiceError.NotFound` for missing employee, vacation request, trip, or department references.
- Reject invalid status transitions before mutating tracked entities.
- Keep all writes cancellation-aware.
- Use `IUnitOfWork` for multi-entity updates that must be atomic, especially vacation approval/cancellation and employee termination/removal.

## Testing and Check Strategy

### Automated Checks

1. Run `dotnet restore .\HR.slnx`.
2. Run `dotnet build .\HR.slnx -c Release`.
3. Run `dotnet test .\HR.slnx -c Release --no-build`.
4. Add service tests for vacation overlap, balance, notice, self-review, status transitions, reviewer audit, pending-only deletion, and exact balance restoration.
5. Add employee service tests for status transitions, duplicate active email, circular manager chains, termination side effects, soft deletion, retained Identity user, and normal-result visibility.
6. Add auth tests for denied login and rejected existing sessions for terminated or soft-deleted employees.
7. Add trip service tests for requester existence/status, soft-deleted requester rejection, past-date rejection, and Friday/Saturday rejection.
8. Add department service/repository tests for employee counts excluding soft-deleted profiles.
9. Add EF model/migration tests or assertions for new columns, relationships, defaults, and delete behaviors.
10. Add representative structured-error compatibility tests for Phase 5 failures: `422` validation/domain rule payloads, `409` conflict payloads, and `401` terminated or soft-deleted authentication/session rejection payloads. Preserve existing code and message fields without normalizing compatibility codes.

### Static Checks

```powershell
rg -n "ApplicationDbContext" .\HR.Infrastructure -g "*Service.cs"
rg -n "IsDeleted|TerminatedAt|VacationBalanceDays|ReviewedByEmployeeId|WorkingDayCount|RequestedByEmployeeId" .\HR.Domain .\HR.Infrastructure
rg -n "employee_id" .\HR.API .\HR.Infrastructure
rg -n "AddScoped<" .\HR.API\Program.cs
git diff --check
```

The service search should return no direct `ApplicationDbContext` service dependencies. `Program.cs` should not gain direct `AddScoped<>` registrations.

### Manual Regression

Use [quickstart.md](./quickstart.md) to validate login/session rejection, vacation lifecycle, employee lifecycle, trips, and department counts against an isolated API/database instance.

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Soft-delete filtering hides historical vacation or trip context | Filter normal employee queries explicitly instead of relying on a broad global query filter. |
| Cookie validation adds a database lookup on every authenticated request | Use a narrow no-tracking active-employee existence lookup and keep it scoped to the employee id claim. |
| Vacation balance changes twice on repeated status updates | Enforce the transition table before mutation and store `WorkingDayCount` on the request. |
| Notice-period tests become date-dependent | Inject `TimeProvider` and test with fixed dates. |
| Existing data has duplicate active emails | Surface migration/preflight risk and enforce duplicate guard in service tests before production rollout. |
| Existing trip rows have no reliable requester source | Keep the requester column nullable for existing rows, require requester only for new submissions, and defer any non-null constraint until a separate approved backfill migration. |
| Phase 6 DI cleanup leaks into this work | Add only Phase 5 registrations in the existing infrastructure extension. |

## Dependencies on Previous Phases

- **Phase 0**: Five-project structure, domain entities, DTOs, shared result types.
- **Phase 1**: Global exception middleware, pagination container, structured errors.
- **Phase 2**: Cookie authentication, Identity credential store, session claims.
- **Phase 3**: Thin controllers, service interfaces, cancellation forwarding.
- **Phase 4**: Tailored repositories, unit of work, EF entity configurations, EF-free `HR.Shared`.

## Out of Scope

- Phase 6 project-owned DI registration cleanup.
- Phase 7 RBAC, attendance, salary/compensation, documents, dashboard metrics, and general audit log.
- Manager-only, reporting-chain, HR-role, or RBAC approval authorization.
- Department hierarchy.
- New route families beyond the existing HR controllers.
- JWTs or token refresh behavior.
- Generic repository base classes, MediatR, CQRS, or new third-party libraries.

## Project Structure

### Documentation (this feature)

```text
specs/005-hr-business-rules/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- api-contract.md
|-- checklists/
|   |-- business-rules.md
|   `-- requirements.md
`-- tasks.md                 # generated later by /speckit-tasks
```

### Source Code (repository root)

```text
HR.API/                     # existing controllers and cookie validation wiring
HR.Application/             # service interfaces and additive DTO contracts
HR.Domain/                  # existing entities plus Phase 5 fields
HR.Infrastructure/
|-- Auth/                   # auth service and session validation
|-- BusinessRules/          # working-day helper
|-- Data/Configurations/    # updated entity mapping
|-- Data/Migrations/        # one Phase 5 migration
|-- Repositories/           # extended tailored repositories
|-- Departments/
|-- Employees/
|-- Transportation/
`-- VacationRequests/
HR.Shared/                  # unchanged shared result/pagination utilities
HR.Tests/                   # focused service, repository, auth, and model tests
```

**Structure Decision**: Continue the existing five-project layered solution. Phase 5 extends the current repository/service boundaries and does not move dependency ownership into Phase 6 structure.

## Complexity Tracking

No constitution violations require justification.
