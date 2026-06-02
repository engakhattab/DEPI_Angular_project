# Tasks: Phase 4 - Repository Pattern and Entity Configurations

**Input**: Design documents from `specs/004-repository-entity-configurations/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/repository-contracts.md`, `quickstart.md`

**Tests**: Phase 4 requires regression verification because it refactors shared persistence behavior while preserving public contracts and Phase 3 transaction-safety guarantees.

**Organization**: Tasks are grouped by user story so each behavior-preservation increment can be implemented and verified before the next one.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it changes different files and does not depend on an incomplete task.
- **[Story]**: Maps the task to a user story from `spec.md`.
- Every task includes an exact file path or path group.

## Task Stage 1: Setup and Baseline

**Purpose**: Confirm the completed Phase 3 baseline before changing persistence boundaries.

- [x] T001 Run the Phase 3 automated baseline and complete the pending authenticated Phase 3 manual checkpoints documented in `specs/004-repository-entity-configurations/quickstart.md`

**Checkpoint**: Phase 3 behavior is verified before Phase 4 implementation begins.

---

## Task Stage 2: Foundational Persistence Contracts

**Purpose**: Add the shared internal contracts and safe paging migration path required before service refactoring.

**CRITICAL**: Complete this phase before starting user-story implementation.

- [x] T002 [P] Create the transaction wrapper contract with commit, rollback, and async disposal operations in `HR.Infrastructure/Repositories/IDataTransaction.cs`
- [x] T003 [P] Create the unit-of-work contract for save, execution-strategy execution, and transaction creation in `HR.Infrastructure/Repositories/IUnitOfWork.cs`
- [x] T004 Implement the EF Core-backed transaction wrapper and unit of work using `ApplicationDbContext` in `HR.Infrastructure/Repositories/UnitOfWork.cs`
- [x] T005 [P] Create the tailored department repository contract in `HR.Infrastructure/Repositories/IDepartmentRepository.cs`
- [x] T006 [P] Create the tailored vacation-request repository contract in `HR.Infrastructure/Repositories/IVacationRequestRepository.cs`
- [x] T007 [P] Create the tailored trip repository contract in `HR.Infrastructure/Repositories/ITripRepository.cs`
- [x] T008 [P] Create the tailored employee repository contract in `HR.Infrastructure/Repositories/IEmployeeRepository.cs`
- [x] T009 [P] Create the read-only single-user and batch-user Identity lookup contract in `HR.Infrastructure/Repositories/IIdentityUserLookup.cs`
- [x] T010 Expose an EF-free public page normalization helper while temporarily retaining `PagedList<T>.CreateAsync` for caller migration compatibility in `HR.Shared/Pagination/PagedList.cs`
- [x] T011 Implement the single EF Core paging execution helper that uses shared normalization and returns `PagedList<T>` in `HR.Infrastructure/Data/Pagination/PagedQueryExecutor.cs`

**Checkpoint**: Contracts compile, the Infrastructure paging executor exists, and existing callers still compile before migration.

---

## Task Stage 3: User Story 1 - Continue HR Operations Reliably (Priority: P1) MVP

**Goal**: Move department, vacation request, trip, and employee persistence behind tailored repositories while preserving existing HR behavior and paging results.

**Independent Test**: Exercise the existing browse, view, create, update, and delete workflows for all four HR areas and confirm that responses, expected failures, filters, ordering, normalization, maximum page size, pagination metadata, and employee transaction rollback behavior remain unchanged.

### Regression Tests for User Story 1

- [x] T012 [P] [US1] Add paging-executor regression coverage for normalization, maximum page size, total count, and returned page contents in `HR.Tests/Data/Pagination/PagedQueryExecutorTests.cs`
- [x] T013 [P] [US1] Add department repository regression coverage for alphabetical paging, lookup, uniqueness checks, and assigned-employee detail loading in `HR.Tests/Repositories/DepartmentRepositoryTests.cs`
- [x] T014 [P] [US1] Add vacation-request repository regression coverage for status and employee filters, newest-first paging, detail loading, and employee-related lookup in `HR.Tests/Repositories/VacationRequestRepositoryTests.cs`
- [x] T015 [P] [US1] Add trip repository regression coverage for newest-first paging and lookup behavior in `HR.Tests/Repositories/TripRepositoryTests.cs`
- [x] T016 [P] [US1] Add employee repository regression coverage for status filtering, employee-number ordering, detail loading, profile lookup, direct reports, and number-existence checks in `HR.Tests/Repositories/EmployeeRepositoryTests.cs`
- [x] T017 [US1] Adapt the Phase 3 employee transaction fixture to construct repository and unit-of-work dependencies while retaining all rollback assertions in `HR.Tests/Employees/EmployeeServiceSafetyTests.cs`

### Implementation for User Story 1

- [x] T018 [P] [US1] Implement tracked department mutations and no-tracking alphabetical pages through `PagedQueryExecutor` in `HR.Infrastructure/Repositories/DepartmentRepository.cs`
- [x] T019 [P] [US1] Implement vacation-request filters, tracked mutations, related lookups, and newest-first pages through `PagedQueryExecutor` in `HR.Infrastructure/Repositories/VacationRequestRepository.cs`
- [x] T020 [P] [US1] Implement tracked trip mutations and newest-first pages through `PagedQueryExecutor` in `HR.Infrastructure/Repositories/TripRepository.cs`
- [x] T021 [P] [US1] Implement employee filters, detail loading, existence checks, direct-report lookup, and employee-number pages through `PagedQueryExecutor` in `HR.Infrastructure/Repositories/EmployeeRepository.cs`
- [x] T022 [P] [US1] Implement cancellation-aware single-user and batch-user reads without credential mutations in `HR.Infrastructure/Repositories/IdentityUserLookup.cs`
- [x] T023 [P] [US1] Refactor department orchestration to use `IDepartmentRepository` and `IUnitOfWork` without changing validation ordering in `HR.Infrastructure/Departments/DepartmentService.cs`
- [x] T024 [P] [US1] Refactor vacation-request orchestration to use `IVacationRequestRepository`, `IEmployeeRepository`, and `IUnitOfWork` without changing filters or validations in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [x] T025 [P] [US1] Refactor trip orchestration to use `ITripRepository` and `IUnitOfWork` without changing identifier generation or ordering in `HR.Infrastructure/Transportation/TripService.cs`
- [x] T026 [US1] Refactor employee orchestration to use repositories, `IIdentityUserLookup`, `IUnitOfWork`, and `UserManager<ApplicationUser>` while preserving Phase 3 create/delete transaction ordering and rollback behavior in `HR.Infrastructure/Employees/EmployeeService.cs`
- [x] T027 [US1] Run the focused paging, repository, cancellation-forwarding, and employee transaction-safety tests in `HR.Tests/Data/Pagination/`, `HR.Tests/Repositories/`, and `HR.Tests/Employees/EmployeeServiceSafetyTests.cs`
- [x] T028 [US1] Scan for remaining `PagedList<T>.CreateAsync` callers, then remove that method and the EF Core package reference only after every caller has migrated in `HR.Shared/Pagination/PagedList.cs` and `HR.Shared/HR.Shared.csproj`
- [x] T029 [US1] Run the Shared-layer scan and paging regression tests to prove `HR.Shared/` has no EF Core namespace, package, or query-execution references

**Checkpoint**: Existing HR operations work through repositories, paging behavior is unchanged, and `HR.Shared` is EF-free.

---

## Task Stage 4: User Story 2 - Continue Signing In Reliably (Priority: P2)

**Goal**: Route authentication employee-profile reads through the employee repository and return an application DTO internally while preserving public Identity and cookie-session behavior.

**Independent Test**: Sign in with an email address and an employee number, restore the current session, sign out, reject invalid credentials, and confirm login JSON, claims, cookies, HTTP statuses, and error codes remain unchanged.

### Regression Tests for User Story 2

- [x] T030 [P] [US2] Add authentication service regression coverage for email lookup, employee-number lookup, invalid credentials, missing matching Identity rejection, and DTO-based profile mapping in `HR.Tests/Auth/AuthServiceTests.cs`
- [x] T031 [P] [US2] Add automated login compatibility coverage for response JSON, `NameIdentifier`, email, `employee_id`, `employee_number`, `full_name`, and session behavior in `HR.Tests/Auth/AuthControllerCompatibilityTests.cs`; verify actual `Set-Cookie` flags through the isolated API quickstart unless an explicit host-level integration test is added
- [x] T032 [P] [US2] Add representative error-payload parity coverage for authentication, validation, conflict, not-found, and generic HTTP `500` responses in `HR.Tests/Compatibility/ErrorResponseParityTests.cs`

### Implementation for User Story 2

- [x] T033 [US2] Change `AuthenticatedEmployee.Employee` from the raw domain entity to `EmployeeResponse` in `HR.Application/Auth/IAuthService.cs`
- [x] T034 [US2] Refactor authentication validation to use `IEmployeeRepository`, map repository entities to `EmployeeResponse`, and retain `UserManager<ApplicationUser>` password checks in `HR.Infrastructure/Auth/AuthService.cs`
- [x] T035 [US2] Update login claim and response construction to consume the DTO and remove raw `Employee` mapping without changing wire behavior in `HR.API/Controllers/AuthController.cs`
- [x] T036 [US2] Run the focused authentication and compatibility tests in `HR.Tests/Auth/` and `HR.Tests/Compatibility/ErrorResponseParityTests.cs`, then execute the authentication regression section in `specs/004-repository-entity-configurations/quickstart.md`

**Checkpoint**: Authentication no longer returns a raw entity or reads HR records through `ApplicationDbContext`, and public login behavior remains unchanged.

---

## Task Stage 5 (Phase 4 scope): User Story 3 - Preserve Stored-Data Rules (Priority: P3)

**Goal**: Split inline EF Core mapping declarations into independently reviewable entity configurations without changing the effective model.

**Independent Test**: Compare the EF Core model before and after extraction, confirm that no migration is needed, and exercise representative uniqueness, required-field, relationship, and delete behavior checks.

### Regression Tests for User Story 3

- [x] T037 [US3] Add model-metadata parity coverage for current lengths, unique indexes, enum storage, relationships, and delete behaviors in `HR.Tests/Data/ApplicationDbContextModelParityTests.cs`

### Implementation for User Story 3

- [x] T038 [P] [US3] Move existing department mapping declarations unchanged into `HR.Infrastructure/Data/Configurations/DepartmentConfiguration.cs`
- [x] T039 [P] [US3] Move existing employee and Identity-user relationship mapping declarations unchanged into `HR.Infrastructure/Data/Configurations/EmployeeConfiguration.cs`
- [x] T040 [P] [US3] Move existing vacation-request mapping declarations unchanged into `HR.Infrastructure/Data/Configurations/VacationRequestConfiguration.cs`
- [x] T041 [P] [US3] Move existing trip mapping declarations unchanged into `HR.Infrastructure/Data/Configurations/TripConfiguration.cs`
- [x] T042 [US3] Replace inline HR mappings so `base.OnModelCreating(builder)` remains first and `ApplyConfigurationsFromAssembly(...)` remains second in `HR.Infrastructure/Data/ApplicationDbContext.cs`
- [x] T043 [US3] Run the model-parity test and EF pending-model-change check, then confirm that `HR.Infrastructure/Data/Migrations/` has no added or modified migration files

**Checkpoint**: Four entity configuration classes preserve the existing model with no schema migration.

---

## Task Stage 6 (Phase 4 scope): User Story 4 - Support Safer Future Changes (Priority: P4)

**Goal**: Activate the Phase 4 boundaries through narrow Infrastructure registrations and prove that service classes no longer reference the shared persistence session.

**Independent Test**: Confirm that each HR entity has one focused repository, all new boundaries are scoped, no generic repository exists, no `*Service.cs` file references `ApplicationDbContext`, and no Phase 6 DI restructuring was introduced.

### Implementation for User Story 4

- [x] T044 [US4] Register the four repositories, `IIdentityUserLookup`, and `IUnitOfWork` as scoped dependencies without expanding into Phase 6 startup cleanup in `HR.Infrastructure/DependencyInjection.cs`
- [x] T045 [US4] Run the structural scans documented in `specs/004-repository-entity-configurations/quickstart.md` and verify `HR.API/Program.cs` remains unchanged unless a necessary Phase 4 registration gap is explicitly documented

**Checkpoint**: Services use dedicated persistence boundaries, registrations resolve, and Phase 6 cleanup remains deferred.

---

## Task Stage 7: Polish and Cross-Cutting Verification

**Purpose**: Validate the complete refactor and reject scope expansion.

- [x] T046 Run `dotnet restore`, `dotnet build`, and the complete test suite for `HR.slnx`
- [x] T047 Run the full authenticated manual regression checklist on an isolated API port using `specs/004-repository-entity-configurations/quickstart.md`
- [x] T048 Run final scans to prove `HR.Shared/` is EF-free, no `PagedList<T>.CreateAsync` caller remains, `ApplicationDbContext.OnModelCreating` keeps the required call order with no inline custom mappings, and `HR.Infrastructure/Data/Migrations/` is unchanged
- [x] T049 Run `git diff --check` and review the final diff to prove no Phase 5 soft deletion, state-machine, overlap, or circular-manager work and no Phase 6 DI restructuring entered Phase 4

---

## Dependencies and Execution Order

### Task Stage Dependencies

- **Setup and Baseline (Task Stage 1)**: No dependencies. Must complete before source edits.
- **Foundational Persistence Contracts (Task Stage 2)**: Depends on Task Stage 1 and blocks service refactors.
- **User Story 1 (Task Stage 3)**: Depends on Task Stage 2. Delivers repositories, caller migration, and EF-free Shared.
- **User Story 2 (Task Stage 4)**: Depends on `IEmployeeRepository` and `EmployeeRepository` from T008 and T021. It can begin after those tasks without waiting for every US1 manual check.
- **User Story 3 (Task Stage 5, Phase 4 scope)**: Depends on Task Stage 2 only and can proceed in parallel with service rewiring.
- **User Story 4 (Task Stage 6, Phase 4 scope)**: Depends on repository implementations, Identity lookup, unit of work, service rewiring, Shared paging cleanup, authentication DTO work, and configuration extraction from US1 through US3.
- **Polish (Task Stage 7)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Starts after the foundation and is the suggested MVP increment.
- **User Story 2 (P2)**: Uses the employee repository introduced by US1 but remains independently testable through authentication scenarios.
- **User Story 3 (P3)**: Independent after the foundation; it can be implemented alongside US1 and US2.
- **User Story 4 (P4)**: Integrates the completed boundaries and verifies the architectural completion signal.

### Within Each User Story

- Add or adapt regression tests before completing the corresponding implementation.
- Add the paging executor before migrating callers and remove Shared EF only after callers are migrated.
- Implement repository queries before rewiring the service that consumes them.
- Preserve validation order, response mapping, cookies, claims, HTTP statuses, error codes, cancellation forwarding, and global exception behavior.
- Stop at each checkpoint and run the focused checks before progressing.

### Parallel Opportunities

- T002, T003, and T005 through T010 can run in parallel.
- T012 through T016 can run in parallel.
- T018 through T022 can run in parallel after their contracts and T011 exist.
- T023 through T025 can run in parallel after their repositories exist.
- T030 through T032 can run in parallel.
- T038 through T041 can run in parallel.
- US3 mapping extraction can run in parallel with US1 and US2 service rewiring after the foundational contracts are ready.

---

## Parallel Example: User Story 1

```text
Task T018: Implement HR.Infrastructure/Repositories/DepartmentRepository.cs
Task T019: Implement HR.Infrastructure/Repositories/VacationRequestRepository.cs
Task T020: Implement HR.Infrastructure/Repositories/TripRepository.cs
Task T021: Implement HR.Infrastructure/Repositories/EmployeeRepository.cs
Task T022: Implement HR.Infrastructure/Repositories/IdentityUserLookup.cs
```

## Parallel Example: User Story 3

```text
Task T038: Create HR.Infrastructure/Data/Configurations/DepartmentConfiguration.cs
Task T039: Create HR.Infrastructure/Data/Configurations/EmployeeConfiguration.cs
Task T040: Create HR.Infrastructure/Data/Configurations/VacationRequestConfiguration.cs
Task T041: Create HR.Infrastructure/Data/Configurations/TripConfiguration.cs
```

---

## Implementation Strategy

### MVP First: User Story 1

1. Complete Task Stage 1 baseline verification.
2. Complete Task Stage 2 contracts and the paging compatibility path.
3. Complete Task Stage 3 repository extraction and caller migration.
4. Remove Shared EF only after the caller scan is clean.
5. Run focused HR, pagination, and transaction-safety tests.
6. Stop and verify the P1 increment before authentication or configuration extraction is accepted.

### Incremental Delivery

1. Baseline plus foundation establishes contracts and the safe paging migration path.
2. US1 preserves HR workflows through tailored repositories and makes Shared EF-free.
3. US2 preserves authentication through repository-based employee reads and DTO-based results.
4. US3 extracts entity mappings with model parity.
5. US4 activates narrow registrations and verifies the architectural boundary.
6. Final verification runs the complete automated and manual regression suite.

### Scope Discipline

- Do not add migrations, schema changes, new routes, public DTO changes, frontend changes, or new business rules.
- Do not implement Phase 5 soft deletion, state machines, overlap checks, or circular-manager rejection.
- Do not introduce a generic repository base class.
- Do not move the remaining startup registrations from `HR.API/Program.cs`; that cleanup belongs to Phase 6 of the master plan.
- Do not normalize or rename existing HTTP statuses or error codes.
- Do not convert expected validation failures into unexpected exceptions or catch unexpected persistence exceptions inside services.

## Notes

- `[P]` tasks change separate files and can be assigned concurrently when their dependencies are satisfied.
- `[US1]` through `[US4]` labels provide traceability to the Phase 4 specification.
- Public HTTP contracts remain unchanged throughout this task list.
- Manual authenticated checks remain required before Phase 4 is considered complete.
