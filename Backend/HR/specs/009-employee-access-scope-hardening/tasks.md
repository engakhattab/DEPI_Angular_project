# Tasks: Phase 9 - Employee Access Scope Hardening

**Input**: Design documents from `specs/009-employee-access-scope-hardening/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/phase9-employee-access-contract.md](./contracts/phase9-employee-access-contract.md)

**Tests**: Required by FR-027 and the Phase 9 plan. Write focused tests before implementation tasks in each user-story phase.

**Organization**: Tasks are grouped by user story so each role/scope slice can be implemented and validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other [P] tasks in the same phase because it touches different files or has no dependency on incomplete tasks.
- **[Story]**: User story label, present only for user-story phases.
- Every task includes an exact repository-relative file path.

## Phase 1: Setup

**Purpose**: Establish the Phase 9 implementation baseline and protect boundaries before code changes.

- [X] T001 Review Phase 9 requirements checklist and record any open gaps in `specs/009-employee-access-scope-hardening/checklists/employee-access.md`
- [X] T002 Capture baseline employee service call sites with `rg` and identify impacted test doubles in `HR.Tests`
- [X] T003 [P] Inspect stale employee-access wording to update later in `API_LIFECYCLE_TESTING_GUIDE.md`
- [X] T004 [P] Confirm Phase 9 must not touch migrations by checking `HR.Infrastructure/Data/Migrations`

---

## Phase 2: Foundational

**Purpose**: Create the shared requester-aware service/repository surface needed by all Phase 9 stories.

**Critical**: No user story implementation should begin until these tasks are complete.

- [X] T005 Update requester-aware method signatures in `HR.Application/Employees/IEmployeeService.cs`
- [X] T006 Update compile-time test doubles for new employee service signatures in `HR.Tests/Authorization/EmployeeRoleControllerTests.cs`
- [X] T007 Update compile-time test doubles for new employee service signatures in `HR.Tests/Compatibility/ErrorResponseParityTests.cs`
- [X] T008 Add repository contract methods for scoped list/detail and active system administrator guards in `HR.Infrastructure/Repositories/IEmployeeRepository.cs`
- [X] T009 Implement repository query methods with filtering-before-pagination support in `HR.Infrastructure/Repositories/EmployeeRepository.cs`
- [X] T010 Add requester employee ID extraction and structured invalid-session handling for employee endpoints in `HR.API/Controllers/EmployeesController.cs`
- [X] T011 Refactor `HR.Infrastructure/Employees/EmployeeService.cs` to accept requester-aware calls without changing existing business-rule behavior
- [X] T012 [P] Add shared Phase 9 test data helpers for role/scope employee graphs in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`

**Checkpoint**: Project compiles after requester-aware interfaces and call sites are updated.

---

## Phase 3: User Story 1 - Block Broad Employee Directory Access (Priority: P1) MVP

**Goal**: Normal employees cannot list all employee profiles and can only view their own detail record.

**Independent Test**: Login/request as an Employee role user; `GET /api/employees` returns `403`, own detail succeeds, other detail returns `403`, and `/api/auth/me` compatibility remains unchanged.

### Tests for User Story 1

- [X] T013 [P] [US1] Add normal employee list forbidden test in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`
- [X] T014 [P] [US1] Add normal employee self-detail allowed and other-detail forbidden tests in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`
- [X] T015 [P] [US1] Add employee endpoint invalid-session structured `401` coverage in `HR.Tests/Authorization/EmployeeRoleControllerTests.cs`

### Implementation for User Story 1

- [X] T016 [US1] Implement Employee role list denial in `HR.Infrastructure/Employees/EmployeeService.cs`
- [X] T017 [US1] Implement Employee role self-only detail authorization in `HR.Infrastructure/Employees/EmployeeService.cs`
- [X] T018 [US1] Map employee list/detail service failures to structured responses in `HR.API/Controllers/EmployeesController.cs`
- [X] T019 [US1] Preserve `/api/auth/me` compatibility by avoiding changes to `HR.API/Controllers/AuthController.cs`

**Checkpoint**: US1 passes independently without manager, HR, or system-admin list behavior complete.

---

## Phase 4: User Story 2 - Allow Managers to See Only Their Team (Priority: P1)

**Goal**: Managers see active direct and indirect reports only in list responses and can view self/team detail only.

**Independent Test**: With a manager hierarchy, manager list contains active direct/indirect reports only, excludes manager self and out-of-scope records, and manager detail access follows self/team rules.

### Tests for User Story 2

- [X] T020 [P] [US2] Add manager list direct/indirect active reports-only test in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`
- [X] T021 [P] [US2] Add manager list excludes self, peers, unrelated, soft-deleted, and terminated reports test in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`
- [X] T022 [P] [US2] Add manager no-team empty page test in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`
- [X] T023 [P] [US2] Add manager detail self/team allowed and outside-scope forbidden tests in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`

### Implementation for User Story 2

- [X] T024 [US2] Implement manager scoped employee page retrieval in `HR.Infrastructure/Employees/EmployeeService.cs`
- [X] T025 [US2] Implement manager detail self/team authorization in `HR.Infrastructure/Employees/EmployeeService.cs`
- [X] T026 [US2] Ensure manager list filtering happens before pagination in `HR.Infrastructure/Repositories/EmployeeRepository.cs`

**Checkpoint**: US1 and US2 both pass independently with no organization-wide access regressions.

---

## Phase 5: User Story 3 - Preserve HR and System Administrator Organization Access (Priority: P1)

**Goal**: HR administrators and system administrators retain organization-wide employee list/detail access, including terminated and soft-deleted records.

**Independent Test**: HR/System users can list and view active, suspended, terminated, and soft-deleted employee records while preserving existing list filters and pagination.

### Tests for User Story 3

- [X] T027 [P] [US3] Add HRAdministrator organization-wide list includes active, suspended, terminated, and soft-deleted records test in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`
- [X] T028 [P] [US3] Add SystemAdministrator organization-wide list includes active, suspended, terminated, and soft-deleted records test in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`
- [X] T029 [P] [US3] Add HR/System detail access for terminated and soft-deleted employees tests in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`
- [X] T030 [P] [US3] Add status-filter plus pagination-within-organization-scope test in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`

### Implementation for User Story 3

- [X] T031 [US3] Implement HR/System organization-wide list behavior including soft-deleted records in `HR.Infrastructure/Employees/EmployeeService.cs`
- [X] T032 [US3] Implement HR/System detail behavior including soft-deleted records in `HR.Infrastructure/Employees/EmployeeService.cs`
- [X] T033 [US3] Add organization-wide including-soft-deleted query support in `HR.Infrastructure/Repositories/EmployeeRepository.cs`

**Checkpoint**: US1, US2, and US3 pass and employee read access matrix is complete.

---

## Phase 6: User Story 4 - Restrict Employee Management Writes (Priority: P2)

**Goal**: Employee create/update/delete are HR/System administrator operations, HR cannot update/delete SystemAdministrator records, role assignment remains SystemAdministrator-only, and last-active-SystemAdministrator removal or role demotion is blocked.

**Independent Test**: Exercise create/update/delete/role-assignment as each role and validate only permitted roles can mutate employees, protected SystemAdministrator records are enforced, the last active SystemAdministrator cannot be removed or demoted, and SystemAdministrator demotion is allowed only when another active SystemAdministrator remains.

### Tests for User Story 4

- [X] T034 [P] [US4] Add Employee and Manager create/update/delete forbidden-before-mutation tests in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`
- [X] T035 [P] [US4] Add HRAdministrator create/update/delete allowed for non-SystemAdministrator target tests in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`
- [X] T036 [P] [US4] Add HRAdministrator update/delete SystemAdministrator forbidden tests in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`
- [X] T037 [P] [US4] Add SystemAdministrator update/delete SystemAdministrator allowed when another active SystemAdministrator exists tests in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`
- [X] T038 [P] [US4] Add last-active-SystemAdministrator update/delete/termination/status-change rejection-before-mutation tests in `HR.Tests/Employees/EmployeeAccessScopeTests.cs`
- [X] T039 [P] [US4] Add role assignment regression tests covering SystemAdministrator-only authorization, HRAdministrator forbidden, last-active-SystemAdministrator demotion rejected before mutation, and SystemAdministrator demotion allowed only when another active SystemAdministrator remains in `HR.Tests/Authorization/EmployeeRoleControllerTests.cs`
- [X] T040 [P] [US4] Add existing employee business-rule regression coverage for duplicate email, employee-number immutability, and same-status behavior in `HR.Tests/Employees/EmployeeServiceBusinessRuleTests.cs`

### Implementation for User Story 4

- [X] T041 [US4] Implement create authorization for HRAdministrator and SystemAdministrator only in `HR.Infrastructure/Employees/EmployeeService.cs`
- [X] T042 [US4] Implement update authorization and HR protected-SystemAdministrator target guard in `HR.Infrastructure/Employees/EmployeeService.cs`
- [X] T043 [US4] Implement delete authorization and HR protected-SystemAdministrator target guard in `HR.Infrastructure/Employees/EmployeeService.cs`
- [X] T044 [US4] Implement last-active-SystemAdministrator update/delete/termination/status-change and role-demotion guard in `HR.Infrastructure/Employees/EmployeeService.cs`
- [X] T045 [US4] Add active SystemAdministrator count or guard query for update/delete/termination/status/role-demotion checks in `HR.Infrastructure/Repositories/EmployeeRepository.cs`
- [X] T046 [US4] Preserve role assignment SystemAdministrator-only behavior and route/response compatibility in `HR.API/Controllers/EmployeesController.cs`

**Checkpoint**: All Phase 9 user stories pass independently and together.

---

## Phase 7: Documentation, Compatibility, and Validation

**Purpose**: Update stale documentation, validate no unrelated phase work occurred, and run final checks.

- [X] T047 [P] Update employee endpoint role/scope wording in `API_LIFECYCLE_TESTING_GUIDE.md`
- [X] T048 [P] Add Phase 9 manual smoke checklist notes from quickstart in `API_LIFECYCLE_TESTING_GUIDE.md`
- [X] T049 [P] Confirm `CLIENT_INSTALLATION_GUIDE.md` has no stale broad employee endpoint access wording
- [X] T050 [P] Run static check for no Phase 10 vacation changes by inspecting `HR.API/Controllers/VacationRequestsController.cs`
- [X] T051 [P] Run static check for no Phase 11 trip changes by inspecting `HR.API/Controllers/TripsController.cs`
- [X] T052 [P] Run static check for no migration files in `HR.Infrastructure/Data/Migrations`
- [X] T053 Run `dotnet restore .\HR.slnx` using `HR.slnx`
- [X] T054 Run `dotnet build .\HR.slnx -c Release` using `HR.slnx`
- [X] T055 Run `dotnet test .\HR.slnx -c Release --no-build` using `HR.slnx`
- [X] T056 Run EF pending model check with `HR.Infrastructure/HR.Infrastructure.csproj` and `HR.API/HR.API.csproj`
- [X] T057 Record final Phase 9 validation results in `specs/009-employee-access-scope-hardening/implementation-summary.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user stories.
- **US1 (Phase 3)**: Depends on Foundational; MVP read-access privacy.
- **US2 (Phase 4)**: Depends on Foundational; can be developed after or parallel with US1 once shared signatures compile, but validates manager behavior independently.
- **US3 (Phase 5)**: Depends on Foundational; can be developed after or parallel with US1/US2 once repository scoped queries exist.
- **US4 (Phase 6)**: Depends on Foundational and benefits from read-scope helpers, but write authorization can be validated independently.
- **Polish (Phase 7)**: Depends on all selected user stories.

### User Story Dependencies

- **US1**: No dependency on other user stories after Foundational.
- **US2**: No dependency on US1 behavior, but shares repository scoped-query infrastructure.
- **US3**: No dependency on US1/US2 behavior, but shares repository paging and detail infrastructure.
- **US4**: No dependency on list behavior, but shares requester-aware service contract and role lookup.

### Within Each User Story

- Write tests before implementation.
- Update service authorization before controller response mapping when both are needed.
- Repository query work must happen before service methods that call those queries.
- Validate each checkpoint before moving to the next priority group.

---

## Parallel Opportunities

- T003 and T004 can run in parallel after T001.
- T006 and T007 can run in parallel after T005 because they update different test files.
- T012 can run in parallel with foundational interface implementation.
- Tests within each user story marked [P] can be created in parallel, but implementation tasks in the same service/repository files should be sequenced to avoid conflicts.
- Documentation/static validation tasks T047-T052 can run in parallel after implementation tasks are complete.

## Parallel Example: User Story 1

```text
Task: "T013 [US1] Add normal employee list forbidden test in HR.Tests/Employees/EmployeeAccessScopeTests.cs"
Task: "T014 [US1] Add normal employee self-detail allowed and other-detail forbidden tests in HR.Tests/Employees/EmployeeAccessScopeTests.cs"
Task: "T015 [US1] Add employee endpoint invalid-session structured 401 coverage in HR.Tests/Authorization/EmployeeRoleControllerTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "T020 [US2] Add manager list direct/indirect active reports-only test in HR.Tests/Employees/EmployeeAccessScopeTests.cs"
Task: "T021 [US2] Add manager list excludes self, peers, unrelated, soft-deleted, and terminated reports test in HR.Tests/Employees/EmployeeAccessScopeTests.cs"
Task: "T023 [US2] Add manager detail self/team allowed and outside-scope forbidden tests in HR.Tests/Employees/EmployeeAccessScopeTests.cs"
```

## Parallel Example: User Story 3

```text
Task: "T027 [US3] Add HRAdministrator organization-wide historical list test in HR.Tests/Employees/EmployeeAccessScopeTests.cs"
Task: "T028 [US3] Add SystemAdministrator organization-wide historical list test in HR.Tests/Employees/EmployeeAccessScopeTests.cs"
Task: "T029 [US3] Add HR/System historical detail tests in HR.Tests/Employees/EmployeeAccessScopeTests.cs"
```

## Parallel Example: User Story 4

```text
Task: "T034 [US4] Add Employee and Manager write-forbidden tests in HR.Tests/Employees/EmployeeAccessScopeTests.cs"
Task: "T036 [US4] Add HRAdministrator protected-SystemAdministrator target tests in HR.Tests/Employees/EmployeeAccessScopeTests.cs"
Task: "T038 [US4] Add last-active-SystemAdministrator update/delete/termination/status guard tests in HR.Tests/Employees/EmployeeAccessScopeTests.cs"
Task: "T039 [US4] Add role-assignment authorization and last-active-demotion protection regression tests in HR.Tests/Authorization/EmployeeRoleControllerTests.cs"
```

---

## Implementation Strategy

### MVP First: US1 Only

1. Complete Phase 1 Setup.
2. Complete Phase 2 Foundational requester-aware service/repository/controller surface.
3. Complete Phase 3 US1.
4. Validate normal employee list denial and self-only detail.
5. Stop for review if only the highest-risk privacy fix is desired.

### Incremental Delivery

1. Setup and Foundational work.
2. US1: normal employee privacy.
3. US2: manager team scope.
4. US3: HR/System organization visibility.
5. US4: management writes and system-admin protection.
6. Documentation and full validation.

### Boundaries During Implementation

- Do not create migrations.
- Do not change routes or successful DTO shapes.
- Do not change cookies, claims, login, or `/api/auth/me`.
- Do not modify vacation, trip, compensation, document, dashboard, audit, bootstrap, or Swagger behavior.
- Stop and report before any schema change appears necessary.

## Final Validation Notes

- All task items above use the required checklist format.
- All story task items include `[US#]` labels.
- All task items include repository-relative file paths.
- No migration task exists.
- Phase 9 remains employee access hardening only.
