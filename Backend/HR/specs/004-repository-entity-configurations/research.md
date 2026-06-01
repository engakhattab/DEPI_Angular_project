# Research: Phase 4 - Repository Pattern and Entity Configurations

**Status**: Complete

## Decision 1: Use Tailored Repositories Per HR Entity

**Decision**: Add separate repository interfaces and implementations for departments, vacation requests, trips, and employees. Expose only methods required by current workflows.

**Rationale**: The constitution requires per-aggregate repositories and explicitly rejects generic repository bases. Tailored methods keep filters, ordering, eager loading, and tracking behavior reviewable while removing direct persistence-session usage from services.

**Alternatives considered**:

- Add a generic repository base: rejected because it conflicts with the constitution and obscures entity-specific query behavior.
- Expose only raw query objects: rejected as the default because it leaves storage concerns spread through services. Use focused methods instead.
- Keep direct context access in services: rejected because Phase 4 exists to remove it.

## Decision 2: Keep Repository Interfaces in Infrastructure

**Decision**: Place repository interfaces and implementations in `HR.Infrastructure/Repositories/`.

**Rationale**: The constitution assigns EF Core, Identity, repositories, and data-access concerns to `HR.Infrastructure`. Current concrete services also live in infrastructure because they coordinate Identity and persistence internals. No forbidden application-to-infrastructure dependency is introduced.

**Alternatives considered**:

- Put repository interfaces in `HR.Application`: rejected for this phase because the approved constitution explicitly places repository interfaces in infrastructure.
- Move all service implementations to `HR.Application`: rejected because current employee and authentication orchestration still depend on Identity abstractions owned by infrastructure.

## Decision 3: Add an Infrastructure Unit of Work

**Decision**: Add `IUnitOfWork`, `IDataTransaction`, and `UnitOfWork`. The unit of work owns saves, execution-strategy execution, and transaction creation.

**Rationale**: Employee creation and deletion span HR repositories and the existing Identity credential store. A unit-of-work boundary removes direct `ApplicationDbContext` usage from services while preserving Phase 3 atomicity and rollback behavior. Services retain orchestration; infrastructure retains persistence details.

**Alternatives considered**:

- Let `EmployeeService` keep context access only for transactions: rejected because it violates the Phase 4 completion signal.
- Make `EmployeeRepository` own Identity orchestration: rejected because credential operations belong to `UserManager<ApplicationUser>` and repository responsibilities would become blurred.
- Add a full transaction pipeline framework: rejected because a small local abstraction is sufficient.

## Decision 4: Add a Narrow Identity Read Lookup

**Decision**: Add `IIdentityUserLookup` for cancellation-aware single-user and batch-user reads used during employee response mapping. Keep password checks and Identity mutations in `UserManager<ApplicationUser>`.

**Rationale**: Employee list mapping currently reads `ApplicationDbContext.Users` directly. Using `UserManager.Users` inside services would still leak persistence queries and could reintroduce per-record reads. A narrow lookup encapsulates this read concern without creating a general Identity repository.

**Alternatives considered**:

- Query users through `UserManager.Users` in services: rejected because query execution remains a data-access concern.
- Call `FindByIdAsync` once per employee: rejected because it creates avoidable page-scoped N+1 queries.
- Replace `UserManager`: rejected because the existing Identity credential store must remain unchanged.

## Decision 5: Extract Four HR Entity Configuration Classes

**Decision**: Move the existing department, employee, vacation-request, and trip mappings into one `IEntityTypeConfiguration<T>` class per entity. Keep Identity's built-in configuration in the base context.

**Rationale**: This mechanically reduces `ApplicationDbContext.OnModelCreating` to base configuration plus assembly scanning while preserving the model. The employee configuration retains the one-to-one `ApplicationUser` relationship.

**Alternatives considered**:

- Add a separate custom Identity-user configuration: rejected because no custom Identity-user mapping exists beyond the employee relationship.
- Modify entities or migrations during extraction: rejected because Phase 4 is schema-neutral.

## Decision 6: Preserve Current Service and API Contracts

**Decision**: Keep controllers, application service interfaces, routes, DTOs, pagination envelopes, errors, authentication, and authorization unchanged.

**Rationale**: Phase 4 is an internal persistence refactor. Contract changes would expand risk and violate the phase boundary.

**Alternatives considered**:

- Redesign application service interfaces: rejected because no user-facing or architectural need requires it.
- Add new business validations: rejected because they belong to Phase 5.

## Decision 7: Limit Dependency Registration Changes

**Decision**: Register repositories, `IIdentityUserLookup`, and `IUnitOfWork` as scoped dependencies in the existing `HR.Infrastructure.DependencyInjection` extension. Leave startup restructuring for Phase 6.

**Rationale**: The new boundaries must be resolvable, but the approved master plan schedules complete DI ownership cleanup after repository extraction.

**Alternatives considered**:

- Complete Phase 6 registration cleanup now: rejected because phases are independent reviewable units.
- Register dependencies directly in `Program.cs`: rejected because infrastructure already owns the relevant extension method.

## Decision 8: Validate Model Parity and Transaction Safety

**Decision**: Preserve the Phase 3 SQLite-backed transaction tests, add focused repository tests, and run a no-pending-model-change check without adding migrations.

**Rationale**: Compilation alone cannot detect mapping drift, changed query behavior, or broken rollback semantics.

**Alternatives considered**:

- Rely only on build output: rejected because it misses behavioral regressions.
- Add broad end-to-end infrastructure: rejected because focused xUnit and manual regression checks are sufficient for this phase.

## Decision 9: Apply Constitution Rules According to Their Planned Phase

**Decision**: Treat Phase 5 business rules and full Phase 6 DI ownership cleanup as staged constitution requirements. Phase 4 preserves current runtime behavior, adds only required infrastructure registrations, and does not pull later-phase work forward.

**Rationale**: The approved master plan intentionally separates independent reviewable phases. Applying later-phase rules early would broaden Phase 4 and increase regression risk.

## Decision 10: Keep Shared Pagination EF-Free

**Decision**: Add one infrastructure-owned `PagedQueryExecutor` helper. Repositories call that helper for EF Core paging execution. `PagedList<T>` remains in `HR.Shared` as an EF-free result model with constants and normalization only.

**Rationale**: Infrastructure owns EF Core. A single executor avoids both the current Shared-layer leak and duplicated `CountAsync`, `Skip`, `Take`, and `ToListAsync` logic across repositories.

**Migration order**:

1. Expose shared normalization while temporarily retaining `PagedList<T>.CreateAsync`.
2. Add the infrastructure paging executor.
3. Move every caller to repository methods backed by the executor.
4. Verify no caller remains.
5. Remove `PagedList<T>.CreateAsync` and the EF Core package from `HR.Shared`.

## Decision 11: Return an Application DTO From Authentication

**Decision**: Change the internal `AuthenticatedEmployee.Employee` value from the raw domain entity to the existing `EmployeeResponse` application DTO.

**Rationale**: `EmployeeResponse` already contains the fields required for login response construction and claims creation. Mapping inside `AuthService` removes the raw-entity leak without changing public login JSON, cookies, claims, HTTP statuses, or error codes.
