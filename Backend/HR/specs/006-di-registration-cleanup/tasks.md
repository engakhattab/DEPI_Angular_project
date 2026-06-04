# Tasks: Phase 6 - DI Registration Cleanup

**Input**: Design documents from `specs/006-di-registration-cleanup/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Included because Phase 6 requires DI registration regression coverage, static ownership checks, compatibility tests, and local startup smoke validation.

**Organization**: Tasks are grouped by user story so implementation can be delivered and verified incrementally.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other independent tasks
- **[Story]**: User story association, for example `[US1]`
- Every task includes the target file or artifact path

## Phase 1: Setup

**Purpose**: Confirm baseline state, checklist readiness, and local environment assumptions before changing registration code.

- [X] T001 Run baseline `dotnet restore`, `dotnet build HR.slnx`, and `dotnet test HR.slnx`; record any pre-existing failures in the implementation handoff/final summary, not in `specs/006-di-registration-cleanup/tasks.md`.
- [X] T002 Review and complete Phase 6 checklists in `specs/006-di-registration-cleanup/checklists/requirements.md` and `specs/006-di-registration-cleanup/checklists/di-registration.md`; update spec/plan artifacts first if any item is not satisfied.
- [X] T003 Verify no running `HR.API` process is locking build outputs before implementation, using `HR.API/bin/Debug/net8.0/HR.API.exe` as the lock target.
- [X] T004 Verify the existing Phase 5 migration `HR.Infrastructure/Data/Migrations/20260603014628_Phase5HrBusinessRules.cs` exists and confirm local `HrSystemDb` is at the required baseline using `specs/006-di-registration-cleanup/quickstart.md`; do not create a Phase 6 migration.
- [X] T005 [P] Create or confirm a focused test location for DI registration checks in `HR.Tests/DependencyInjection/`.

---

## Phase 2: Foundation

**Purpose**: Add failing coverage and the minimal application/infrastructure registration entry points needed by all stories.

- [X] T006 [P] Add failing DI resolution tests for `AddApplication()` and `AddInfrastructure(configuration)` in `HR.Tests/DependencyInjection/DependencyRegistrationTests.cs`.
- [X] T007 [P] Add failing static ownership tests for `Program.cs`, `HR.Application`, and `HR.Infrastructure` registration boundaries in `HR.Tests/DependencyInjection/RegistrationBoundaryTests.cs`.
- [X] T008 [P] Add the minimal dependency required for `IServiceCollection` extension methods to `HR.Application/HR.Application.csproj` without adding infrastructure, EF Core, or Identity references.
- [X] T009 Add `AddApplication(this IServiceCollection services)` in `HR.Application/DependencyInjection.cs`; keep it stable and empty unless application-layer registrations are genuinely required.
- [X] T010 Add any missing configuration abstractions package support needed by the new infrastructure registration signature in `HR.Infrastructure/HR.Infrastructure.csproj`.
- [X] T011 Update test composition in `HR.Tests/TestInfrastructure/SqliteTestEnvironment.cs` so existing SQLite-based tests remain valid after infrastructure owns SQL Server `ApplicationDbContext` registration in production startup; keep any test-safe provider override isolated to test infrastructure.

**Checkpoint**: DI entry point types exist, tests describe the desired boundaries, and the test harness can avoid conflicting SQL Server/SQLite context registrations.

---

## Phase 3: User Story 1 - Keep Application Startup Readable (Priority: P1)

**Goal**: `Program.cs` delegates application and infrastructure setup to named extension methods and no longer owns EF Core, Identity store, repository, or service registration details.

**Independent Test**: Static registration-boundary tests fail before implementation and pass only when `Program.cs` calls `AddApplication()` and `AddInfrastructure(builder.Configuration)` without direct moved registrations.

- [X] T012 [US1] Change `HR.Infrastructure/DependencyInjection.cs` from `AddInfrastructure(this IServiceCollection services)` to `AddInfrastructure(this IServiceCollection services, IConfiguration configuration)`.
- [X] T013 [US1] Move SQL Server `ApplicationDbContext` registration and `DefaultConnection` lookup from `HR.API/Program.cs` into `HR.Infrastructure/DependencyInjection.cs`.
- [X] T014 [US1] Move Identity role/store registration and existing password options from `HR.API/Program.cs` into `HR.Infrastructure/DependencyInjection.cs`.
- [X] T015 [US1] Keep existing infrastructure registrations for repositories, unit of work, identity lookup, application service implementations, `TimeProvider`, working-day calendar, and employee session validation in `HR.Infrastructure/DependencyInjection.cs`.
- [X] T016 [US1] Update `HR.API/Program.cs` to call `builder.Services.AddApplication()` and `builder.Services.AddInfrastructure(builder.Configuration)`.
- [X] T017 [US1] Remove moved EF Core, Identity store, repository, and service-registration details plus obsolete `using` directives from `HR.API/Program.cs`.
- [X] T018 [US1] Run focused DI/static tests in `HR.Tests/DependencyInjection/DependencyRegistrationTests.cs` and `HR.Tests/DependencyInjection/RegistrationBoundaryTests.cs`.

**Checkpoint**: `Program.cs` remains readable and startup registration ownership is delegated to Application and Infrastructure.

---

## Phase 4: User Story 2 - Preserve Existing Runtime Behavior (Priority: P1)

**Goal**: Refactoring registration ownership does not change authentication, authorization, JSON, CORS, Swagger, middleware order, structured error handling, or Phase 5 business-rule behavior.

**Independent Test**: Existing API and service tests pass with no behavior changes after registration ownership moves.

- [X] T019 [P] [US2] Add or extend login compatibility assertions in `HR.Tests/Auth/AuthControllerCompatibilityTests.cs` so the response wrapper and existing fields remain stable after DI cleanup.
- [X] T020 [P] [US2] Add or extend structured error compatibility assertions in `HR.Tests/Compatibility/ErrorResponseParityTests.cs`, `HR.Tests/Middleware/GlobalExceptionMiddlewareTests.cs`, or relevant controller tests so existing `status`, `code`, and `message` behavior remains compatible.
- [X] T021 [P] [US2] Add or extend authentication/session rejection coverage for soft-deleted or terminated employees in `HR.Tests/Auth/AuthRevocationTests.cs` or existing Phase 5 test files.
- [X] T022 [US2] Confirm `HR.API/Program.cs` still owns cookie event wiring, cookie settings, authorization policies, controllers, JSON converters, CORS, Swagger, and middleware pipeline order after DI cleanup.
- [X] T023 [US2] Run existing Phase 5 business-rule tests for vacations, employees, departments, trips, and structured errors from `HR.Tests/`.
- [X] T024 [US2] Start `HR.API/HR.API.csproj` against local SQL Server `HrSystemDb` and run the manual smoke checks documented in `specs/006-di-registration-cleanup/quickstart.md`.

**Checkpoint**: Runtime behavior is preserved while the registration ownership changes.

---

## Phase 5: User Story 3 - Make Registration Ownership Auditable (Priority: P2)

**Goal**: Future contributors can verify registration ownership through documented static checks and test coverage.

**Independent Test**: Static scans and tests prove Application does not depend on Infrastructure and that `Program.cs` no longer contains direct infrastructure registration details.

- [X] T025 [P] [US3] Verify `HR.Application/HR.Application.csproj` references only allowed abstractions and does not reference `HR.Infrastructure`, EF Core providers, or ASP.NET Identity implementation packages.
- [X] T026 [P] [US3] Verify `HR.Infrastructure/HR.Infrastructure.csproj` remains the only project responsible for EF Core SQL Server, Identity stores, repositories, and infrastructure-backed service registrations.
- [X] T027 [US3] Encode the ownership scans from `specs/006-di-registration-cleanup/quickstart.md` into `HR.Tests/DependencyInjection/RegistrationBoundaryTests.cs` where practical.
- [X] T028 [US3] Run static scans from `specs/006-di-registration-cleanup/quickstart.md` and confirm no direct service, repository, `DbContext`, or Identity store registration remains in `HR.API/Program.cs`.

**Checkpoint**: Registration ownership is easy to inspect through both tests and documented scan commands.

---

## Phase 6: Polish and Cross-Cutting Validation

**Purpose**: Final validation, no-schema-change confirmation, and handoff.

- [X] T029 Run full `dotnet restore`, `dotnet build HR.slnx`, and `dotnet test HR.slnx` from the repository root.
- [X] T030 Verify no Phase 6 migration files were created under `HR.Infrastructure/Data/Migrations/` and no schema changes were introduced.
- [X] T031 Verify no source changes conflict with the existing Phase 5 migration baseline or local `HrSystemDb` setup described in `specs/006-di-registration-cleanup/quickstart.md`.
- [X] T032 Run `git diff --check` for changed Phase 6 files and source files.
- [X] T033 Review final changed files and confirm changes are limited to DI registration cleanup, related tests, and required docs.
- [X] T034 Update implementation handoff notes with baseline failures, commands run, local DB migration status, manual smoke results, and any remaining risks.

---

## Dependencies

- Setup tasks T001-T005 must complete before source implementation.
- Foundation tasks T006-T011 must complete before user-story implementation.
- US1 is required before US2 manual runtime validation.
- US2 depends on US1 because behavior preservation must be verified against the new registration composition.
- US3 can run after US1 and in parallel with most US2 validation.
- Polish tasks T029-T034 run after all selected user stories are complete.

## Parallel Execution Examples

- T005 can run in parallel with T001-T004 after the repository layout is known.
- T006 and T007 can run in parallel because they create independent test files.
- T008 and T010 can run in parallel because they update different project files.
- T019, T020, and T021 can run in parallel because they target different behavior coverage areas.
- T025 and T026 can run in parallel because they inspect separate project files.

## Implementation Strategy

1. Complete checklist review first; Phase 6 should not start while `di-registration.md` is incomplete.
2. Add tests that describe DI ownership and behavior preservation.
3. Add the Application registration entry point.
4. Move infrastructure registration ownership into `HR.Infrastructure`.
5. Simplify `Program.cs` without changing runtime middleware behavior.
6. Run focused tests, full tests, static scans, and local SQL Server smoke checks.
7. Confirm no Phase 6 migration or database schema change exists.

## Notes

- Phase 6 is a registration refactor only; it must not add tables or create migrations.
- The local SQL Server database may need the existing Phase 5 migration applied before smoke testing, but Phase 6 itself should not change schema.
- Automated tests should keep SQLite provider setup isolated in test infrastructure; production `AddInfrastructure(configuration)` remains SQL Server-based.
- If `HR.API` is running during build, stop the running process before rebuilding to avoid locked DLL failures.
