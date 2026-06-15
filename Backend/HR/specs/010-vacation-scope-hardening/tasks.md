# Tasks: Phase 10 - Vacation Request Scope Hardening

**Input**: Design documents from `specs/010-vacation-scope-hardening/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/phase10-vacation-scope-contract.md](./contracts/phase10-vacation-scope-contract.md)

**Tests**: Required by FR-035 and the Phase 10 plan. Write focused tests before implementation tasks in each user-story phase.

**Organization**: Tasks are grouped by user story so each vacation scope slice can be implemented and validated independently. Creator tracking is included as a separate approval gate because it requires a database migration that is not approved by this task list.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other [P] tasks in the same phase because it touches different files or has no dependency on incomplete tasks.
- **[Story]**: User story label, present only for user-story phases.
- Every task includes an exact repository-relative file path.

## Phase 1: Setup

**Purpose**: Establish the Phase 10 implementation baseline, review requirements quality, and protect migration and phase boundaries before code changes.

- [X] T001 Review Phase 10 requirements checklist and record any unresolved requirement gaps in `specs/010-vacation-scope-hardening/checklists/vacation-scope.md`
- [X] T002 Review Phase 10 pre-plan checklist and keep it aligned with clarified requirements in `specs/010-vacation-scope-hardening/checklists/requirements.md`
- [X] T003 Create a Phase 10 implementation summary stub with baseline, migration gate, and validation sections in `specs/010-vacation-scope-hardening/implementation-summary.md`
- [X] T004 Capture baseline vacation service/controller/repository call sites and test doubles in `specs/010-vacation-scope-hardening/implementation-summary.md`
- [X] T005 [P] Confirm no Phase 10 migration approval exists and document the `CreatedByEmployeeId` approval gate in `specs/010-vacation-scope-hardening/implementation-summary.md`
- [X] T006 [P] Inspect stale vacation-access wording to update later in `API_LIFECYCLE_TESTING_GUIDE.md`
- [X] T007 [P] Confirm Phase 10 must not touch trip endpoints by recording the current boundary for `HR.API/Controllers/TripsController.cs`

---

## Phase 2: Foundational

**Purpose**: Create the shared requester-aware vacation service/repository/controller surface needed by all Phase 10 stories.

**Critical**: No user story implementation should begin until these tasks are complete.

- [X] T008 Update requester-aware method signatures in `HR.Application/VacationRequests/IVacationRequestService.cs`
- [X] T009 Update compile-time vacation controller test double for new service signatures in `HR.Tests/VacationRequests/VacationRequestsControllerTests.cs`
- [X] T010 Update existing vacation creation business-rule tests to pass requester IDs in `HR.Tests/VacationRequests/VacationRequestServiceBusinessRuleTests.cs`
- [X] T011 Update existing vacation review/delete business-rule tests to pass requester IDs in `HR.Tests/VacationRequests/VacationRequestReviewBusinessRuleTests.cs`
- [X] T012 Add repository contract methods for scoped vacation paging and tracked target lookup in `HR.Infrastructure/Repositories/IVacationRequestRepository.cs`
- [X] T013 Implement scoped vacation paging and tracked target lookup methods in `HR.Infrastructure/Repositories/VacationRequestRepository.cs`
- [X] T014 Add requester employee ID extraction and invalid-session handling to all vacation endpoints in `HR.API/Controllers/VacationRequestsController.cs`
- [X] T015 Inject and store `IEmployeeAccessService` in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T016 Refactor `HR.Infrastructure/VacationRequests/VacationRequestService.cs` to accept requester-aware calls without changing existing vacation business-rule behavior
- [X] T017 [P] Add shared Phase 10 vacation role/scope fixture helpers in `HR.Tests/VacationRequests/VacationRequestScopeTestData.cs`
- [X] T018 Run a compile checkpoint after signature changes using `HR.slnx`

**Checkpoint**: Project compiles after requester-aware interfaces, call sites, controller tests, and existing business-rule tests are updated.

---

## Phase 3: User Story 1 - Employee Own Vacation Privacy (Priority: P1) MVP

**Goal**: Normal employees can see, create, review, and delete only according to own-vacation rules and cannot access other employees' vacation records.

**Independent Test**: Act as an Employee role user; list returns own requests only, own detail succeeds, other detail returns `403`, own create succeeds if business rules pass, non-self create returns `403` before target lookup, review returns `403`, and own pending delete is allowed.

### Tests for User Story 1

- [X] T019 [US1] Add employee vacation list/detail scope tests including out-of-scope `employeeId` filter empty scoped page behavior in `HR.Tests/VacationRequests/VacationRequestEmployeeScopeTests.cs`
- [X] T020 [US1] Add employee vacation create self-allowed and non-self-forbidden-before-target-lookup tests in `HR.Tests/VacationRequests/VacationRequestEmployeeScopeTests.cs`
- [X] T021 [US1] Add employee vacation review forbidden tests in `HR.Tests/VacationRequests/VacationRequestEmployeeScopeTests.cs`
- [X] T022 [US1] Add employee vacation own-pending-delete allowed and other-request-delete forbidden tests in `HR.Tests/VacationRequests/VacationRequestEmployeeScopeTests.cs`
- [X] T023 [P] [US1] Add controller invalid `employee_id` claim coverage and structured `403` forbidden payload shape coverage for list/detail/create/delete vacation endpoints in `HR.Tests/VacationRequests/VacationRequestsControllerTests.cs`

### Implementation for User Story 1

- [X] T024 [US1] Implement Employee role own-only vacation list filtering in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T025 [US1] Implement Employee role own-only vacation detail authorization in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T026 [US1] Implement Employee role self-only vacation creation authorization before target lookup in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T027 [US1] Implement Employee role review forbidden behavior in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T028 [US1] Implement Employee role own-pending-delete authorization in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T029 [US1] Map vacation list/detail/create/delete service failures to structured responses in `HR.API/Controllers/VacationRequestsController.cs`

**Checkpoint**: US1 passes independently without manager or administrator scope behavior complete.

---

## Phase 4: User Story 2 - Manager Team Vacation Review (Priority: P1)

**Goal**: Managers see active direct/indirect team requests by default, can explicitly list their own requests with self filter, can view self/team details, can create/delete only own requests, and can review team requests only.

**Independent Test**: With a manager hierarchy, unfiltered manager list contains active direct/indirect team requests only, `employeeId` self filter returns manager-owned requests, detail follows self/team rules, create is self-only, review is team-only, self-review is rejected, and team delete is forbidden.

### Tests for User Story 2

- [X] T030 [US2] Add manager unfiltered vacation list team-only, excludes-self, and manager-with-no-team empty list tests in `HR.Tests/VacationRequests/VacationRequestManagerListScopeTests.cs`
- [X] T031 [US2] Add manager `employeeId` self/team/outside filter tests proving out-of-scope filters return empty scoped pages without target-existence disclosure in `HR.Tests/VacationRequests/VacationRequestManagerListScopeTests.cs`
- [X] T032 [P] [US2] Add manager detail self/team allowed, outside-scope forbidden, and empty-team outside-self forbidden tests in `HR.Tests/VacationRequests/VacationRequestManagerDetailScopeTests.cs`
- [X] T033 [US2] Add manager create self allowed plus team/outside/suspended/inactive target forbidden or validation tests in `HR.Tests/VacationRequests/VacationRequestManagerMutationScopeTests.cs`
- [X] T034 [US2] Add manager review team allowed, self-review rejected, and outside-team forbidden-before-domain-validation tests in `HR.Tests/VacationRequests/VacationRequestManagerMutationScopeTests.cs`
- [X] T035 [US2] Add manager own-pending-delete allowed and team-pending-delete forbidden tests in `HR.Tests/VacationRequests/VacationRequestManagerMutationScopeTests.cs`

### Implementation for User Story 2

- [X] T036 [US2] Implement Manager unfiltered team-only vacation list filtering and explicit self/team `employeeId` filter behavior in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T037 [US2] Implement Manager vacation detail self/team authorization in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T038 [US2] Implement Manager self-only vacation creation authorization in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T039 [US2] Implement Manager team-only review authorization and self-review ordering in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T040 [US2] Implement Manager own-pending-delete authorization and team-delete denial in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T041 [US2] Ensure manager vacation list filtering happens before pagination and total count in `HR.Infrastructure/Repositories/VacationRequestRepository.cs`

**Checkpoint**: US1 and US2 pass independently with no HR/System organization-scope regressions.

---

## Phase 5: User Story 3 - HR and System On-Behalf Vacation Operations (Priority: P1)

**Goal**: HR administrators and system administrators retain organization-wide vacation list/detail access, can create vacation requests on behalf of eligible employees, can review any non-self request, and cannot self-review.

**Independent Test**: Act as HRAdministrator and SystemAdministrator users; organization-wide list/detail succeeds, on-behalf creation succeeds for eligible employees and preserves existing validation rules, review succeeds for non-self requests, self-review is rejected, and routes/DTOs remain compatible.

### Tests for User Story 3

- [X] T042 [US3] Add HRAdministrator and SystemAdministrator organization-wide vacation list/detail tests including suspended requester behavior compatibility in `HR.Tests/VacationRequests/VacationRequestAdminScopeTests.cs`
- [X] T043 [US3] Add HRAdministrator and SystemAdministrator on-behalf create allowed plus suspended, inactive, soft-deleted, and terminated target validation tests in `HR.Tests/VacationRequests/VacationRequestAdminScopeTests.cs`
- [X] T044 [US3] Add HRAdministrator and SystemAdministrator non-self review allowed and self-review rejected tests in `HR.Tests/VacationRequests/VacationRequestAdminScopeTests.cs`
- [X] T045 [P] [US3] Add route and structured error response compatibility coverage for vacation paths, including `403`, `404`, and `422` `{ code, message }` payloads, no error-code normalization, and no unauthorized data leakage in `HR.Tests/VacationRequests/VacationRequestsControllerTests.cs`

### Implementation for User Story 3

- [X] T046 [US3] Implement HRAdministrator and SystemAdministrator organization-wide vacation list/detail authorization in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T047 [US3] Implement HRAdministrator and SystemAdministrator on-behalf vacation creation authorization while preserving existing validations in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T048 [US3] Implement HRAdministrator and SystemAdministrator non-self review authorization and self-review rejection in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T049 [US3] Preserve existing vacation creation DTO and response DTO compatibility in `HR.Application/DTOs/VacationRequests/VacationRequestCreateRequest.cs`

**Checkpoint**: US1, US2, and US3 pass and vacation read/create/review access matrix is complete.

---

## Phase 6: User Story 4 - Scoped Pending Vacation Deletion (Priority: P2)

**Goal**: Vacation deletion remains pending-only and is constrained by owner, manager, and organization scope without allowing target-state probing.

**Independent Test**: Exercise pending and non-pending vacation deletion as Employee, Manager, HRAdministrator, and SystemAdministrator; in-scope pending deletes succeed, in-scope non-pending deletes preserve the existing business-rule failure, and existing out-of-scope targets return `403` before pending-status validation.

### Tests for User Story 4

- [X] T050 [US4] Add delete missing-target `404` versus existing-out-of-scope `403` tests in `HR.Tests/VacationRequests/VacationRequestDeleteScopeTests.cs`
- [X] T051 [US4] Add delete pending-only preservation tests for in-scope Employee, Manager, HRAdministrator, and SystemAdministrator users in `HR.Tests/VacationRequests/VacationRequestDeleteScopeTests.cs`
- [X] T052 [US4] Add out-of-scope delete authorization-before-pending-status-validation tests in `HR.Tests/VacationRequests/VacationRequestDeleteScopeTests.cs`
- [X] T053 [P] [US4] Add repository delete target lookup coverage for owner/reviewer data in `HR.Tests/Repositories/VacationRequestRepositoryTests.cs`

### Implementation for User Story 4

- [X] T054 [US4] Implement missing-target `404` versus out-of-scope `403` delete ordering in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T055 [US4] Preserve pending-only delete business-rule behavior after authorization passes in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T056 [US4] Ensure delete target lookup includes enough owner data for scope checks without exposing raw entities outside repositories in `HR.Infrastructure/Repositories/VacationRequestRepository.cs`

**Checkpoint**: All Phase 10 user stories pass independently and together.

---

## Phase 7: Creator Tracking Approval Gate - Approved and Implemented

**Purpose**: Implement the required `CreatedByEmployeeId` work after explicit user migration approval.

**Approval**: User explicitly approved the VacationRequest `CreatedByEmployeeId` migration plan. The schema change is limited to a nullable creator FK, index, and restrict/no-action relationship. Existing rows are not backfilled with invented creator data.

- [X] T057 Create the creator-tracking migration approval packet in `specs/010-vacation-scope-hardening/implementation-summary.md`
- [X] T058 Record explicit user migration approval in `specs/010-vacation-scope-hardening/implementation-summary.md`
- [X] T059 Add nullable `CreatedByEmployeeId` and optional creator navigation in `HR.Domain/Entities/VacationRequest.cs`
- [X] T060 Configure the creator FK, index, and restrict/no-action delete behavior in `HR.Infrastructure/Data/Configurations/VacationRequestConfiguration.cs`
- [X] T061 Create approved EF migration `AddVacationRequestCreatedByEmployee` in `HR.Infrastructure/Data/Migrations`
- [X] T062 Add additive creator metadata to vacation responses and tests in `HR.Application/DTOs/VacationRequests/VacationRequestResponse.cs` and `HR.Tests/VacationRequests`
- [X] T063 Apply the approved migration to the configured local database and record the result in `specs/010-vacation-scope-hardening/implementation-summary.md`

**Checkpoint**: Creator tracking is implemented for new rows. Existing rows remain valid with null `CreatedByEmployeeId`.

---

## Phase 8: Documentation, Compatibility, and Validation

**Purpose**: Update stale vacation documentation, validate no unrelated phase work occurred, and run final checks.

- [X] T064 Update vacation endpoint role/scope wording in `API_LIFECYCLE_TESTING_GUIDE.md`
- [X] T065 Add Phase 10 manual smoke checklist notes from quickstart in `API_LIFECYCLE_TESTING_GUIDE.md`
- [X] T066 [P] Confirm `CLIENT_INSTALLATION_GUIDE.md` has no stale broad vacation endpoint access wording
- [X] T067 [P] Run static check for no Phase 9 employee behavior changes by inspecting `HR.API/Controllers/EmployeesController.cs`
- [X] T068 [P] Run static check for no Phase 11 trip changes by inspecting `HR.API/Controllers/TripsController.cs`
- [X] T069 [P] Run static check for no unapproved migration files in `HR.Infrastructure/Data/Migrations`
- [X] T070 Run `dotnet restore .\HR.slnx` using `HR.slnx`
- [X] T071 Run `dotnet build .\HR.slnx -c Release` using `HR.slnx`
- [X] T072 Run `dotnet test .\HR.slnx -c Release --no-build` using `HR.slnx`
- [X] T073 Run EF pending model check with `HR.Infrastructure/HR.Infrastructure.csproj` and `HR.API/HR.API.csproj`
- [X] T074 Run `git diff --check` from the repository root and record the result in `specs/010-vacation-scope-hardening/implementation-summary.md`
- [X] T075 Record final Phase 10 validation results in `specs/010-vacation-scope-hardening/implementation-summary.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user stories.
- **US1 (Phase 3)**: Depends on Foundational; MVP employee vacation privacy.
- **US2 (Phase 4)**: Depends on Foundational; can be developed after or parallel with US1 once shared signatures compile, but validates manager behavior independently.
- **US3 (Phase 5)**: Depends on Foundational; can be developed after or parallel with US1/US2 once repository scoped queries exist.
- **US4 (Phase 6)**: Depends on Foundational and benefits from read-scope helpers, but delete behavior can be validated independently after target lookup and authorization ordering exist.
- **Creator Tracking Gate (Phase 7)**: Depends on Setup and explicit future migration approval; current tasks document the hard stop only and do not execute migration/schema work.
- **Polish (Phase 8)**: Depends on all selected user stories and any approved creator-tracking work.

### User Story Dependencies

- **US1**: No dependency on other user stories after Foundational.
- **US2**: No dependency on US1 behavior, but shares repository scoped-query infrastructure.
- **US3**: No dependency on US1/US2 behavior, but shares requester-aware service contract and repository paging infrastructure.
- **US4**: No dependency on list behavior, but shares requester-aware service contract and target lookup/scope helpers.

### Within Each User Story

- Write tests before implementation.
- Update service authorization before controller response mapping when both are needed.
- Repository query work must happen before service methods that call those queries.
- Validate each checkpoint before moving to the next priority group.

---

## Parallel Opportunities

- T005, T006, and T007 can run in parallel after T001.
- T009, T010, and T011 can run in parallel after T008 because they update different test files.
- T017 can run in parallel with foundational interface implementation.
- Tests in separate user-story test files marked [P] can be created in parallel.
- Implementation tasks in `HR.Infrastructure/VacationRequests/VacationRequestService.cs` should be sequenced to avoid same-file conflicts.
- Documentation/static validation tasks T064-T069 can run in parallel after implementation tasks are complete.

## Parallel Example: User Story 1

```text
Task: "T023 [US1] Add controller invalid employee_id claim coverage for list/detail/create/delete vacation endpoints in HR.Tests/VacationRequests/VacationRequestsControllerTests.cs"
Task: "T017 Add shared Phase 10 vacation role/scope fixture helpers in HR.Tests/VacationRequests/VacationRequestScopeTestData.cs"
```

## Parallel Example: User Story 2

```text
Task: "T032 [US2] Add manager detail self/team allowed and outside-scope forbidden tests in HR.Tests/VacationRequests/VacationRequestManagerDetailScopeTests.cs"
Task: "T023 [US1] Add controller invalid employee_id claim coverage for list/detail/create/delete vacation endpoints in HR.Tests/VacationRequests/VacationRequestsControllerTests.cs"
```

## Parallel Example: User Story 3

```text
Task: "T045 [US3] Add route and response compatibility coverage for vacation admin paths in HR.Tests/VacationRequests/VacationRequestsControllerTests.cs"
Task: "T053 [US4] Add repository delete target lookup coverage for owner/reviewer data in HR.Tests/Repositories/VacationRequestRepositoryTests.cs"
```

## Parallel Example: User Story 4

```text
Task: "T053 [US4] Add repository delete target lookup coverage for owner/reviewer data in HR.Tests/Repositories/VacationRequestRepositoryTests.cs"
Task: "T045 [US3] Add route and response compatibility coverage for vacation admin paths in HR.Tests/VacationRequests/VacationRequestsControllerTests.cs"
```

---

## Implementation Strategy

### MVP First: US1 Only

1. Complete Phase 1 Setup.
2. Complete Phase 2 Foundational requester-aware service/repository/controller surface.
3. Complete Phase 3 US1.
4. Validate normal employee own-only list/detail/create/review/delete behavior.
5. Stop for review if only the highest-risk privacy fix is desired.

### Incremental Delivery

1. Setup and Foundational work.
2. US1: normal employee privacy.
3. US2: manager team scope and self-filter behavior.
4. US3: HR/System organization visibility, on-behalf creation, and non-self review.
5. US4: scoped pending deletion and authorization-before-domain-validation ordering.
6. Creator tracking gate: document the hard stop and wait for explicit migration approval before any schema work.
7. Documentation and full validation.

### Boundaries During Implementation

- Do not create migrations unless explicit user migration approval is granted in a later instruction.
- Do not change routes or successful DTO shapes except approved additive creator metadata after migration approval.
- Do not change cookies, claims, login, or `/api/auth/me`.
- Do not modify employee, trip, compensation, document, dashboard, attendance, audit, bootstrap, or Swagger behavior.
- Stop and report before any schema change appears necessary outside the approved creator-tracking path.
