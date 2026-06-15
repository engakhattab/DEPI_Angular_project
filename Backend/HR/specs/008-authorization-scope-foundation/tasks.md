# Tasks: Phase 8 - Authorization Scope Foundation

**Input**: Design documents from `specs/008-authorization-scope-foundation/`

**Prerequisites**: [spec.md](./spec.md), [plan.md](./plan.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/authorization-scope-contract.md](./contracts/authorization-scope-contract.md), [quickstart.md](./quickstart.md)

**Tests**: Required by FR-020 and the user-story independent test criteria. Write focused tests before implementation changes and make sure they fail for missing contract behavior before patching production code.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently. Phase 8 must not harden employee, vacation, or trip endpoints.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files or is a read-only validation task.
- **[Story]**: User-story label for story phases only.
- Every task includes the target file or validation path.

## Phase 1: Setup (Shared Planning and Baseline)

**Purpose**: Establish the Phase 8 implementation boundary before any code changes.

- [X] T001 Create `specs/008-authorization-scope-foundation/implementation-summary.md` with Phase 8 boundary notes, baseline command placeholders, and an explicit "no endpoint hardening, no migration" section.
- [X] T002 [P] Review `HR.Application/Authorization/IEmployeeAccessService.cs`, `HR.Infrastructure/Authorization/EmployeeAccessService.cs`, and `HR.Infrastructure/Repositories/EmployeeRepository.cs`; record existing contract gaps in `specs/008-authorization-scope-foundation/implementation-summary.md`.
- [X] T003 [P] Run the existing focused authorization tests with `dotnet test .\HR.slnx -c Release --filter "FullyQualifiedName~Authorization"` and record the baseline result in `specs/008-authorization-scope-foundation/implementation-summary.md`.
- [X] T004 [P] Capture migration baseline with `git status --short .\HR.Infrastructure\Data\Migrations` and record the result in `specs/008-authorization-scope-foundation/implementation-summary.md`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add the shared internal contract surface required by every Phase 8 story.

**CRITICAL**: No endpoint-specific hardening belongs in this phase.

- [X] T005 Add failing contract-surface tests for required `IEmployeeAccessService` members in `HR.Tests/Authorization/EmployeeAccessContractTests.cs`.
- [X] T006 Update `HR.Application/Authorization/IEmployeeAccessService.cs` with explicit methods for `IsSelf`, `IsManagerOfAsync`, `CanAccessTeamDataAsync`, `IsHRAdministratorAsync`, `IsSystemAdministratorAsync`, and `HasOrganizationScopeAsync`.
- [X] T007 Update `HR.Infrastructure/Authorization/EmployeeAccessService.cs` with stub or delegated implementations that compile only after the contract surface is complete, without changing endpoint behavior.
- [X] T008 Run `dotnet build .\HR.slnx -c Release` and record the result in `specs/008-authorization-scope-foundation/implementation-summary.md`.

**Checkpoint**: Contract compiles and user-story tests can now target explicit Phase 8 decisions.

---

## Phase 3: User Story 1 - Resolve Current Employee Scope (Priority: P1) MVP

**Goal**: Resolve current employee context consistently for active, suspended, terminated, soft-deleted, and missing employees.

**Independent Test**: Current employee context returns employee ID, role, active state, deleted state, and terminated state for valid employees; missing employees fail as invalid session; suspended employees are not active but remain distinguishable from terminated employees.

### Tests for User Story 1

- [X] T009 [P] [US1] Add current employee context tests for active, suspended, terminated, soft-deleted, and missing employees in `HR.Tests/Authorization/EmployeeAccessCurrentContextTests.cs`.
- [X] T010 [P] [US1] Add authentication/session eligibility alignment tests for deleted, terminated, and suspended employees in `HR.Tests/Auth/AuthRevocationTests.cs`.

### Implementation for User Story 1

- [X] T011 [US1] Adjust `GetCurrentAsync` in `HR.Infrastructure/Authorization/EmployeeAccessService.cs` so it returns the required `EmployeeAccessContext` fields and unauthorized-compatible failure for missing employee records. *(Already satisfied by existing implementation.)*
- [X] T012 [US1] Verify `HR.Application/Authorization/IEmployeeAccessService.cs` exposes `EmployeeAccessContext` with `EmployeeId`, `Role`, `IsActive`, `IsDeleted`, and `IsTerminated` only; do not add email as a required scope field. *(Verified: record has exactly those 5 fields, no email.)*
- [X] T013 [US1] Run `dotnet test .\HR.slnx -c Release --no-build --filter "FullyQualifiedName~EmployeeAccessCurrentContextTests|FullyQualifiedName~AuthRevocationTests"` and record the result in `specs/008-authorization-scope-foundation/implementation-summary.md`.

**Checkpoint**: User Story 1 is independently testable through current-context service tests.

---

## Phase 4: User Story 2 - Evaluate Role and Organization Scope (Priority: P1)

**Goal**: Make role and organization-scope decisions explicit and reusable for later endpoint hardening phases.

**Independent Test**: Employees and managers do not have organization scope; HR administrators and System administrators have organization scope when not deleted or terminated; suspended administrators remain organization-scope eligible in Phase 8.

### Tests for User Story 2

- [X] T014 [P] [US2] Add organization-scope tests for `Employee`, `Manager`, `HRAdministrator`, and `SystemAdministrator` in `HR.Tests/Authorization/OrganizationScopeTests.cs`.
- [X] T015 [P] [US2] Add deleted, terminated, suspended, and missing requester role-check tests in `HR.Tests/Authorization/OrganizationScopeEligibilityTests.cs`.

### Implementation for User Story 2

- [X] T016 [US2] Implement `IsHRAdministratorAsync`, `IsSystemAdministratorAsync`, and `HasOrganizationScopeAsync` in `HR.Infrastructure/Authorization/EmployeeAccessService.cs`. *(Implemented in T007.)*
- [X] T017 [US2] Confirm `HasAnyRoleAsync` in `HR.Infrastructure/Authorization/EmployeeAccessService.cs` denies missing, deleted, and terminated employees while preserving suspended employee eligibility. *(Already satisfied by existing implementation.)*
- [X] T018 [US2] Run `dotnet test .\HR.slnx -c Release --no-build --filter "FullyQualifiedName~OrganizationScope"` and record the result in `specs/008-authorization-scope-foundation/implementation-summary.md`.

**Checkpoint**: User Story 2 is independently testable through organization-scope service tests.

---

## Phase 5: User Story 3 - Evaluate Self and Manager Team Scope (Priority: P1)

**Goal**: Make self, manager-team, team-data, and visible-employee decisions explicit for direct plus indirect manager hierarchy.

**Independent Test**: Self access succeeds only for the same employee; manager team scope includes direct and indirect active reports; manager scope excludes peers, unrelated employees, the manager's own manager, deleted reports, terminated reports, and suspended report targets in visible team data.

### Tests for User Story 3

- [X] T019 [P] [US3] Add self-scope tests for same and different employee IDs in `HR.Tests/Authorization/SelfScopeTests.cs`.
- [X] T020 [P] [US3] Add manager topology tests for direct report, indirect report, peer, unrelated employee, and manager's own manager in `HR.Tests/Authorization/ManagerTeamScopeTests.cs`.
- [X] T021 [P] [US3] Add manager visibility exclusion tests for suspended, soft-deleted, and terminated report targets in `HR.Tests/Authorization/VisibleEmployeeScopeTests.cs`.
- [X] T022 [P] [US3] Add visible-employee-set tests for employee, manager, HR administrator, System administrator, missing requester, deleted requester, and terminated requester in `HR.Tests/Authorization/VisibleEmployeeSetTests.cs`.

### Implementation for User Story 3

- [X] T023 [US3] Implement `IsSelf`, `IsManagerOfAsync`, and `CanAccessTeamDataAsync` in `HR.Infrastructure/Authorization/EmployeeAccessService.cs`. *(Implemented in T007.)*
- [X] T024 [US3] Refactor `CanAccessEmployeeAsync` in `HR.Infrastructure/Authorization/EmployeeAccessService.cs` to delegate to explicit self, manager, and organization-scope decisions without changing expected results. *(Refactored in T007.)*
- [X] T025 [US3] Verify `GetVisibleEmployeeIdsAsync` in `HR.Infrastructure/Authorization/EmployeeAccessService.cs` returns self-only for employees, self plus active direct and indirect reports for managers, and active organization employees for HR/System administrators. *(Already satisfied by existing implementation.)*
- [X] T026 [US3] Adjust `HR.Infrastructure/Repositories/IEmployeeRepository.cs` and `HR.Infrastructure/Repositories/EmployeeRepository.cs` only if the manager-scope tests prove existing direct-plus-indirect report traversal cannot satisfy the Phase 8 contract. *(No change needed: existing traversal already matches Phase 8 requirements.)*
- [X] T027 [US3] Run `dotnet test .\HR.slnx -c Release --no-build --filter "FullyQualifiedName~SelfScopeTests|FullyQualifiedName~ManagerTeamScopeTests|FullyQualifiedName~VisibleEmployee"` and record the result in `specs/008-authorization-scope-foundation/implementation-summary.md`.

**Checkpoint**: User Story 3 is independently testable through self, manager, and visible-employee service tests.

---

## Phase 6: User Story 4 - Prepare Later Endpoint Hardening Without Changing Behavior (Priority: P2)

**Goal**: Confirm Phase 8 prepares reusable scope decisions without changing employee, vacation, or trip endpoint behavior.

**Independent Test**: Static checks and focused tests show no employee, vacation, or trip endpoint hardening, no route/response/cookie/claim/status/error-code changes, and no migration.

### Tests for User Story 4

- [X] T028 [P] [US4] Add Phase 8 boundary tests proving `HR.API/Program.cs` still delegates through `AddApplication()` and `AddInfrastructure(builder.Configuration)` in `HR.Tests/DependencyInjection/Phase8BoundaryTests.cs`.
- [X] T029 [US4] Add Phase 8 boundary tests proving `HR.Application/HR.Application.csproj` does not reference `HR.Infrastructure` in `HR.Tests/DependencyInjection/Phase8BoundaryTests.cs`.
- [X] T030 [P] [US4] Add Phase 8 controller-boundary tests proving employee, vacation request, and trip controller route attributes remain unchanged in `HR.Tests/Authorization/Phase8EndpointBoundaryTests.cs`.
- [X] T031 [P] [US4] Add Phase 8 auth compatibility boundary tests covering `HR.API/Controllers/AuthController.cs`, login request/response DTOs, `/api/auth/me`, current `employee_id`, role, and `employee_role` claims, cookie authentication events/configuration in `HR.API/Program.cs`, `HR.API/Middleware/GlobalExceptionMiddleware.cs`, existing error response shape/status codes, and cookie behavior in `HR.Tests/Authorization/Phase8AuthCompatibilityBoundaryTests.cs`.

### Implementation for User Story 4

- [X] T032 [US4] Inspect `git diff -- .\HR.API\Controllers .\HR.API\Program.cs .\HR.Application\Employees .\HR.Application\VacationRequests .\HR.Application\Transportation` and record the no-hardening result in `specs/008-authorization-scope-foundation/implementation-summary.md`.
- [X] T033 [US4] Inspect `git status --short .\HR.Infrastructure\Data\Migrations` and record the no-migration result in `specs/008-authorization-scope-foundation/implementation-summary.md`.
- [X] T034 [US4] Run `dotnet test .\HR.slnx -c Release --no-build --filter "FullyQualifiedName~Phase8BoundaryTests|FullyQualifiedName~Phase8EndpointBoundaryTests|FullyQualifiedName~Phase8AuthCompatibilityBoundaryTests"` and record the result in `specs/008-authorization-scope-foundation/implementation-summary.md`.

**Checkpoint**: User Story 4 proves Phase 8 remains foundation-only.

---

## Phase 7: Polish and Cross-Cutting Validation

**Purpose**: Validate the complete Phase 8 implementation and document outcomes.

- [X] T035 Run `dotnet restore .\HR.slnx` and record the result in `specs/008-authorization-scope-foundation/implementation-summary.md`.
- [X] T036 Run `dotnet build .\HR.slnx -c Release` and record the result in `specs/008-authorization-scope-foundation/implementation-summary.md`.
- [X] T037 Run `dotnet test .\HR.slnx -c Release --no-build --filter "FullyQualifiedName~Authorization"` and record the result in `specs/008-authorization-scope-foundation/implementation-summary.md`.
- [X] T038 Run `dotnet test .\HR.slnx -c Release --no-build --filter "FullyQualifiedName~Auth|FullyQualifiedName~Bootstrap|FullyQualifiedName~Attendance|FullyQualifiedName~Compensation|FullyQualifiedName~Documents|FullyQualifiedName~Dashboard|FullyQualifiedName~Audit"` and record the Phase 7-sensitive module compatibility result in `specs/008-authorization-scope-foundation/implementation-summary.md`.
- [X] T039 Run `dotnet test .\HR.slnx -c Release --no-build` and record the result in `specs/008-authorization-scope-foundation/implementation-summary.md`.
- [X] T040 Run `dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj` and record the result in `specs/008-authorization-scope-foundation/implementation-summary.md`.
- [X] T041 Run `git status --short .\HR.Infrastructure\Data\Migrations` and confirm no migration files were created in `specs/008-authorization-scope-foundation/implementation-summary.md`.
- [X] T042 Update completed task checkboxes in `specs/008-authorization-scope-foundation/tasks.md` only for tasks actually completed during implementation.

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational; MVP scope.
- **User Story 2 (Phase 4)**: Depends on Foundational; can run after or beside US1 if contract is stable.
- **User Story 3 (Phase 5)**: Depends on Foundational; can run after or beside US1/US2 if contract is stable.
- **User Story 4 (Phase 6)**: Depends on implementation changes from US1-US3.
- **Polish (Phase 7)**: Depends on all selected user stories.

### User Story Dependencies

- **US1**: No dependency on other user stories after Foundational.
- **US2**: No dependency on other user stories after Foundational.
- **US3**: No dependency on other user stories after Foundational, but should reuse role/organization helper methods if US2 is already implemented.
- **US4**: Depends on US1-US3 to verify boundary preservation.

### Within Each User Story

- Write tests first and confirm they fail for missing explicit contract behavior.
- Update `IEmployeeAccessService` before implementation changes that depend on the new methods.
- Update `EmployeeAccessService` before repository changes.
- Touch `IEmployeeRepository` and `EmployeeRepository` only if tests prove the existing manager traversal is insufficient.
- Complete story-specific focused tests before moving to the next story.

## Parallel Opportunities

- T002, T003, and T004 can run in parallel after T001.
- T009 and T010 can run in parallel because they use different test files.
- T014 and T015 can run in parallel because they use different test files.
- T019, T020, T021, and T022 can run in parallel because they use different test files.
- T028, T030, and T031 can run in parallel because they use different test files; T029 follows T028 because it shares `HR.Tests/DependencyInjection/Phase8BoundaryTests.cs`.
- US1, US2, and US3 can be implemented by separate workers after T005-T008, but merge order must preserve the shared interface contract.

## Parallel Example: User Story 3

```text
Task: "T019 [US3] Add self-scope tests in HR.Tests/Authorization/SelfScopeTests.cs"
Task: "T020 [US3] Add manager topology tests in HR.Tests/Authorization/ManagerTeamScopeTests.cs"
Task: "T021 [US3] Add manager visibility exclusion tests in HR.Tests/Authorization/VisibleEmployeeScopeTests.cs"
Task: "T022 [US3] Add visible-employee-set tests in HR.Tests/Authorization/VisibleEmployeeSetTests.cs"
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundational contract work.
3. Complete Phase 3 current employee context work.
4. Run the US1 focused tests.
5. Stop and report results if only MVP validation is requested.

### Incremental Delivery

1. Deliver US1 current employee context.
2. Deliver US2 role and organization scope.
3. Deliver US3 self, manager, team-data, and visible-employee decisions.
4. Deliver US4 boundary verification.
5. Run full validation and EF pending-model checks.

### Phase 8 Guardrails

- Do not change employee, vacation request, or trip endpoint behavior.
- Do not change public routes, JSON response shapes, cookies, claims, status codes, or error codes.
- Do not create migrations.
- Do not move infrastructure-backed implementations into `HR.Application`.
- Do not add Phase 9, Phase 10, Phase 11, Phase 12, or Phase 13 behavior.
