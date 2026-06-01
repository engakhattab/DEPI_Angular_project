# Implementation Plan: Phase 4 - Repository Pattern and Entity Configurations

**Branch**: `004-repository-entity-configurations` | **Date**: 2026-06-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/004-repository-entity-configurations/spec.md`

## Summary

Introduce tailored repository boundaries for departments, vacation requests, trips, and employees; route authentication employee-profile lookups through the employee repository; and extract the existing entity mappings from `ApplicationDbContext.OnModelCreating` into one configuration class per HR entity. Move EF paging execution from `HR.Shared` into one infrastructure-owned helper. Replace the raw employee entity in the internal authentication result with `EmployeeResponse`. Preserve all routes, response JSON, cookies, claims, HTTP statuses, error codes, schema, and Phase 3 runtime-safety guarantees. Add an infrastructure unit-of-work boundary so employee creation and deletion remain atomic without service classes referencing `ApplicationDbContext`.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core 8, Entity Framework Core 8.0.20, ASP.NET Core Identity, Swashbuckle

**Storage**: Existing SQL Server schema through `ApplicationDbContext`; SQLite in-memory storage for focused automated tests

**Testing**: xUnit, `dotnet build`, `dotnet test`, model-parity check, static dependency scans, and authenticated manual API regression checks

**Target Platform**: Windows or Linux server hosted with Kestrel or IIS

**Project Type**: Layered ASP.NET Core Web API

**Performance Goals**: Preserve Phase 3 bounded list queries and page-scoped related-data loading; list responses return no more than 100 records

**Constraints**: Preserve routes, public DTOs, response JSON, cookies, claims, HTTP statuses, error codes, cancellation behavior, schema, and existing migrations; do not add Phase 5 business rules or complete Phase 6 DI cleanup

**Scale/Scope**: Four HR repositories, one narrow Identity read lookup, one unit-of-work boundary, four entity configuration classes, five service refactors, repository registrations, model-parity verification, and focused regression coverage

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Layered Architecture (I)**: PASS. Repository interfaces and implementations remain in `HR.Infrastructure`, which owns data-access concerns. Public HTTP contracts remain unchanged; the internal authentication result and controller mapping receive a narrow DTO-based cleanup.
- **Cookie-Based Session Authentication (II)**: PASS. `UserManager<ApplicationUser>` remains the credential store. Login, logout, `/api/auth/me`, secure cookies, and JSON `401`/`403` behavior remain unchanged.
- **Service Layer Separation (III)**: PASS. Business orchestration remains in service classes. Reads continue returning DTOs or `PagedList<T>` and writes continue returning `Result<T>` or `Result`.
- **Domain Integrity (IV)**: PASS UNDER STAGED ACTIVATION. Existing checks are preserved. Phase 5 rules such as soft deletion, state machines, overlap detection, and circular-manager detection remain deferred and MUST NOT be implemented early.
- **Global Error Handling (V)**: PASS. Expected failures retain the structured error contract. Unexpected exceptions continue flowing to `GlobalExceptionMiddleware`.
- **Data Access Abstraction (VI)**: PASS UNDER PHASE 6 ACTIVATION. Phase 4 removes direct `ApplicationDbContext` usage from services, moves EF paging execution out of `HR.Shared`, registers scoped repositories, and extracts entity configurations. The separate `HR.Application.DependencyInjection` extension and relocation of remaining startup registration activate in Phase 6.
- **Simplicity & YAGNI (VII)**: PASS. Repositories are tailored per entity. No generic repository base class, MediatR, CQRS, new schema, or unrelated abstraction is introduced.

### Post-Design Re-check

The design artifacts preserve the gates. Phase 4 adds only the registrations required to activate repository and unit-of-work boundaries. Full dependency-registration ownership cleanup remains a Phase 6 requirement.

## Technical Approach

### 1. Add Tailored Infrastructure Repositories

Create repository interfaces and implementations in `HR.Infrastructure/Repositories/`. Each repository owns only the queries and mutations required by current workflows:

1. `IDepartmentRepository` / `DepartmentRepository`
2. `IVacationRequestRepository` / `VacationRequestRepository`
3. `ITripRepository` / `TripRepository`
4. `IEmployeeRepository` / `EmployeeRepository`

Do not add a generic repository base class. Keep ordering, filters, eager-loading decisions, tracked write queries, and no-tracking read queries inside repository implementations. Repository contracts are defined in [contracts/repository-contracts.md](./contracts/repository-contracts.md).

### 2. Add Narrow Shared Persistence Boundaries

Add two infrastructure-only boundaries:

- `IIdentityUserLookup` / `IdentityUserLookup`: read-only, cancellation-aware batch and single-user lookup for employee response mapping. Credential validation and credential mutations remain with `UserManager<ApplicationUser>`.
- `IUnitOfWork` / `UnitOfWork`: owns `SaveChangesAsync`, execution-strategy execution, and transaction creation. Add `IDataTransaction` as the small commit/rollback abstraction returned by `IUnitOfWork`.

`IUnitOfWork` resolves the employee transaction ownership decision conservatively: services coordinate business steps, while infrastructure owns persistence-session and transaction details. Employee creation and deletion keep the Phase 3 transaction order and rollback behavior.

### 3. Move Paging Execution Into Infrastructure

Add `HR.Infrastructure/Data/Pagination/PagedQueryExecutor.cs` as the single EF Core paging execution helper. It uses the shared page normalization rules and performs `CountAsync`, `Skip`, `Take`, and `ToListAsync`.

Keep `PagedList<T>` in `HR.Shared` as an EF-free result container with constants and a public normalization helper. Migrate repositories to `PagedQueryExecutor` first. Only after every caller has moved, remove `PagedList<T>.CreateAsync` and remove the EF Core package reference from `HR.Shared`.

Do not duplicate paging execution logic across repositories.

### 4. Refactor Services Incrementally

Refactor and verify services in this order:

1. `DepartmentService`
2. `VacationRequestService`
3. `TripService`
4. `AuthService`
5. `EmployeeService`

After each service refactor, run a build and its focused regression checkpoint. At completion, no `*Service.cs` file references `ApplicationDbContext`.

`AuthService` maps its repository entity to the existing application-layer `EmployeeResponse`. `AuthenticatedEmployee.Employee` changes from a raw entity to `EmployeeResponse`, and `AuthController` stops mapping a raw entity. This is an internal service-contract correction only: login JSON, cookies, claims, HTTP statuses, and error codes remain unchanged.

### 5. Extract Entity Configurations Without Schema Changes

Create one `IEntityTypeConfiguration<T>` class per HR entity:

1. `DepartmentConfiguration`
2. `EmployeeConfiguration`
3. `VacationRequestConfiguration`
4. `TripConfiguration`

Move every existing property rule, unique index, relationship, and delete behavior unchanged. Keep Identity's built-in mappings in `IdentityDbContext`. Replace the HR-specific mapping blocks in `ApplicationDbContext.OnModelCreating` with:

```csharp
base.OnModelCreating(builder);
builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
```

Do not create or modify migrations.

The required `base.OnModelCreating(builder)` call MUST remain first. Assembly scanning MUST remain second. No inline custom `builder.Entity<T>` blocks remain.

### 6. Register Only Required Phase 4 Dependencies

Extend `HR.Infrastructure/DependencyInjection.cs` with scoped registrations for the four repositories, `IIdentityUserLookup`, and `IUnitOfWork`. Keep existing service registrations and `Program.cs` wiring unchanged. The wider split into `AddApplication()` and configuration-driven `AddInfrastructure()` belongs to Phase 6.

## Files and Modules Likely to Change

### Existing Files

```text
HR.Infrastructure/
|-- Auth/AuthService.cs
|-- Departments/DepartmentService.cs
|-- VacationRequests/VacationRequestService.cs
|-- Transportation/TripService.cs
|-- Employees/EmployeeService.cs
|-- Data/ApplicationDbContext.cs
`-- DependencyInjection.cs

HR.Shared/
|-- Pagination/PagedList.cs
`-- HR.Shared.csproj

HR.Application/
`-- Auth/IAuthService.cs

HR.API/
`-- Controllers/AuthController.cs

HR.Tests/
`-- Employees/EmployeeServiceSafetyTests.cs
```

### New Files

```text
HR.Infrastructure/
|-- Data/
|   |-- Configurations/
|   |   |-- DepartmentConfiguration.cs
|   |   |-- EmployeeConfiguration.cs
|   |   |-- VacationRequestConfiguration.cs
|   |   `-- TripConfiguration.cs
|   `-- Pagination/
|       `-- PagedQueryExecutor.cs
`-- Repositories/
    |-- IDepartmentRepository.cs
    |-- DepartmentRepository.cs
    |-- IVacationRequestRepository.cs
    |-- VacationRequestRepository.cs
    |-- ITripRepository.cs
    |-- TripRepository.cs
    |-- IEmployeeRepository.cs
    |-- EmployeeRepository.cs
    |-- IIdentityUserLookup.cs
    |-- IdentityUserLookup.cs
    |-- IUnitOfWork.cs
    |-- IDataTransaction.cs
    `-- UnitOfWork.cs
```

### Files Expected to Remain Unchanged

```text
HR.API/Program.cs
HR.Domain/
HR.Infrastructure/Data/Migrations/
```

## Data Model Changes

No persistent model or schema changes are included. Existing mapping declarations move into per-entity configuration classes without semantic changes. See [data-model.md](./data-model.md).

## API and Route Changes

No API route, verb, request DTO, response DTO, pagination envelope, response JSON, cookie, claim, status code, error code, authentication, or authorization behavior changes are included. `AuthController` receives a narrow internal mapping cleanup only. See [contracts/repository-contracts.md](./contracts/repository-contracts.md) for internal contracts.

## UI and Component Changes

No frontend files or UI contracts change in Phase 4.

## Validation and Error Handling

- Preserve existing service validation order and `Result<T>` failures.
- Preserve existing HTTP statuses and error codes exactly; error-code cleanup is out of scope.
- Preserve `GlobalExceptionMiddleware` handling for unexpected exceptions.
- Forward `CancellationToken` through repository queries, unit-of-work saves, and transaction operations.
- Keep employee creation and deletion rollback-safe across HR records and Identity operations.
- Do not add new business validation rules.

## Testing and Check Strategy

### Automated Checks

1. Run `dotnet restore .\HR.slnx`.
2. Run `dotnet build .\HR.slnx -c Release`.
3. Run `dotnet test .\HR.slnx -c Release --no-build`.
4. Preserve and adapt the Phase 3 employee transaction-safety tests.
5. Add focused repository tests for filters, ordering, normalization, maximum page size, detail loading, and model parity.
6. Verify `HR.Shared` contains no EF Core package, namespace, or query-execution reference after caller migration.
7. Verify login JSON, cookies, claims, HTTP statuses, and representative error payloads remain unchanged.
8. Run a no-pending-model-change check and confirm no migration files were added or modified.

### Static Checks

```powershell
rg -n "ApplicationDbContext" .\HR.Infrastructure -g "*Service.cs"
rg -n "builder\.Entity<" .\HR.Infrastructure\Data\ApplicationDbContext.cs
rg -n "IEntityTypeConfiguration<" .\HR.Infrastructure\Data\Configurations
rg -n "Microsoft\.EntityFrameworkCore|CountAsync|ToListAsync|CreateAsync" .\HR.Shared
git diff --check
```

The first two searches and the `HR.Shared` search should return no matches. The configuration search should find all four HR entity configurations.

### Manual Regression

Use [quickstart.md](./quickstart.md) to exercise authentication and each existing HR workflow on an isolated API port. Complete the pending authenticated Phase 3 checks before treating Phase 4 implementation as started.

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Repository refactor changes query ordering, filters, or tracking behavior | Use tailored repository methods and focused repository regression tests. |
| Employee and Identity operations lose atomicity | Route saves and transactions through `IUnitOfWork`; preserve Phase 3 transaction tests. |
| Identity response mapping introduces per-record queries | Use `IIdentityUserLookup.GetByIdsAsync` for page-scoped batch reads. |
| Extracted configurations drift from the current model | Move mappings mechanically and run model-parity checks with no migration changes. |
| Phase 6 DI cleanup expands Phase 4 scope | Register only required Phase 4 boundaries in the existing infrastructure extension. |
| Phase 5 business rules leak into repository work | Preserve current behavior and keep rule changes explicitly out of scope. |

## Dependencies on Previous Phases

- **Phase 0**: Five-project structure, entities, DTOs, and shared result types.
- **Phase 1**: Global exception middleware and pagination utility.
- **Phase 2**: Cookie authentication, Identity credential store, and current-session endpoints.
- **Phase 3**: Thin controllers, infrastructure service implementations, structured errors, cancellation forwarding, pagination, and employee transaction-safety tests.

## Out of Scope

- New entities, fields, relationships, migrations, or schema changes.
- Phase 5 rules: soft delete, vacation overlap checks, balances, status state machines, reviewer audits, circular manager detection, duplicate active-email rules, trip ownership, and trip-date rules.
- Phase 6 DI cleanup beyond required repository registrations.
- New routes, DTO changes, frontend work, roles, RBAC, attendance, salary, documents, dashboards, or audit logs.
- Generic repository base classes, MediatR, CQRS, or new libraries.

## Project Structure

### Documentation (this feature)

```text
specs/004-repository-entity-configurations/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- repository-contracts.md
|-- checklists/
|   |-- requirements.md
|   `-- requirements-quality.md
`-- tasks.md                 # generated later by /speckit-tasks
```

### Source Code (repository root)

```text
HR.API/                     # unchanged HTTP boundary
HR.Application/             # unchanged DTO and service contracts
HR.Infrastructure/
|-- Data/
|   |-- ApplicationDbContext.cs
|   |-- Configurations/     # new per-entity mappings
|   `-- Migrations/         # unchanged
|-- Repositories/           # new tailored data-access boundaries
|-- Auth/
|-- Departments/
|-- VacationRequests/
|-- Transportation/
`-- Employees/
HR.Domain/                  # unchanged entities and enums
HR.Shared/                  # EF-free results and pagination metadata
HR.Tests/                   # expanded repository and regression coverage
```

**Structure Decision**: Continue the existing five-project layered solution. Keep repository interfaces, implementations, transaction coordination, and Identity read lookup in `HR.Infrastructure`, where the current service implementations already live and where EF Core and Identity dependencies are allowed.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| `HR.Application.DependencyInjection` remains deferred until Phase 6 | The approved master plan separates repository extraction from DI ownership cleanup. Phase 4 adds only required repository registrations to the existing infrastructure extension. | Pulling Phase 6 into Phase 4 increases scope and makes repository regressions harder to isolate. |
| Infrastructure paging executor | `HR.Shared` must remain EF-free without duplicating query execution in every repository. | Keeping `PagedList<T>.CreateAsync` leaks EF Core into Shared; duplicating paging code increases drift risk. |
| DTO-based internal authentication result | Read boundaries must not return raw entities while public login behavior remains unchanged. | Keeping the raw entity violates the service boundary; redesigning the public response expands scope. |
