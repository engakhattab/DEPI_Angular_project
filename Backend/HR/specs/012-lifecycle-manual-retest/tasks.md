# Tasks: Phase 12 - Lifecycle Documentation and Manual Retest

**Input**: Design documents from `specs/012-lifecycle-manual-retest/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/manual-retest-contract.md](./contracts/manual-retest-contract.md), [quickstart.md](./quickstart.md)

**Tests**: Phase 12 uses manual lifecycle retest evidence plus existing automated validation commands. Do not add automated source tests unless a separate defect-fix scope is approved.

**Organization**: Tasks are grouped by user story so a cheaper model can complete documentation, setup, and manual validation incrementally.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches a different file or performs a read-only check
- **[Story]**: Which user story the task belongs to
- Every task includes an exact file path to read, update, or record evidence

## Critical Boundaries

- Do not modify source code in `HR.API/`, `HR.Application/`, `HR.Domain/`, `HR.Infrastructure/`, `HR.Shared/`, or `HR.Tests/`.
- Do not create new migration files under `HR.Infrastructure/Data/Migrations/`.
- Do not edit existing migration files or the EF model snapshot.
- Do not change routes, response JSON, cookies, claims, status codes, or error codes.
- Do not perform Phase 13 Swagger/OpenAPI documentation work.
- Local SQL Server work is allowed only for the disposable database `HrSystemDb_Phase12LifecycleTest` and only with existing approved migrations.
- If a runtime defect is found, record it in `specs/012-lifecycle-manual-retest/implementation-summary.md`, stop that scenario, and do not patch source code without separate user approval.

---

## Phase 1: Setup (Shared Documentation and Validation Context)

**Purpose**: Confirm active feature context, current documentation state, and immutable Phase 12 boundaries.

- [X] T001 Verify `.specify/feature.json` points to `specs/012-lifecycle-manual-retest` and record the result in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T002 Read `specs/012-lifecycle-manual-retest/spec.md` and copy Phase 12 boundaries into `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T003 [P] Read `specs/012-lifecycle-manual-retest/contracts/manual-retest-contract.md` and list required actors/scenarios in `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T004 [P] Read `API_LIFECYCLE_TESTING_GUIDE.md` and note stale or missing Phase 8-11 scope sections in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T005 [P] Read `CLIENT_INSTALLATION_GUIDE.md` and note stale migration/bootstrap/permission sections in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T006 [P] Read `specs/008-authorization-scope-foundation/implementation-summary.md`, `specs/009-employee-access-scope-hardening/implementation-summary.md`, `specs/010-vacation-scope-hardening/implementation-summary.md`, and `specs/011-trip-ownership-scope-hardening/implementation-summary.md`, then summarize completed Phase 8-11 behavior in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T007 [P] Inspect `HR.Infrastructure/Data/Migrations/` and record the approved migration list through Phase 11 in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T008 Confirm no Phase 12 implementation task will edit source or migration paths by recording the forbidden path list in `specs/012-lifecycle-manual-retest/implementation-summary.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Create reusable evidence/checklist artifacts and establish validation command discipline before user story work.

**CRITICAL**: No user story work should begin until this phase is complete.

- [X] T009 Create `specs/012-lifecycle-manual-retest/implementation-summary.md` with sections for environment, commands, documentation updates, manual checklist summary, failures, source-change confirmation, and migration confirmation
- [X] T010 Create `specs/012-lifecycle-manual-retest/manual-retest-checklist.md` with columns for scenario ID, module, actor, setup, action, expected result, actual result, pass/fail, and notes
- [X] T011 Add local database setup instructions for `HrSystemDb_Phase12LifecycleTest` to `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T012 Add command-result recording rows for restore, build, test, database update, migration list, EF pending model check, `git diff --check`, and `git status --short` to `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T013 Add a "STOP on runtime defect" rule to `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T014 Run `dotnet restore .\HR.slnx` and record the result in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T015 Run `dotnet build .\HR.slnx -c Release -p:UseSharedCompilation=false` and record the result in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T016 Run `dotnet test .\HR.slnx -c Release --no-build`; if stale output makes this unsafe, run `dotnet test .\HR.slnx -c Release`, then record the result in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T017 Run `dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj` and record the result in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T018 Confirm no new files exist under `HR.Infrastructure/Data/Migrations/` beyond the approved migration list and record the result in `specs/012-lifecycle-manual-retest/implementation-summary.md`

**Checkpoint**: Foundation ready - documentation and manual retest story work can begin.

---

## Phase 3: User Story 1 - Retest Current Role Scope Behavior (Priority: P1) MVP

**Goal**: Update lifecycle documentation and checklist scenarios so manual validation reflects completed Phase 8-11 role/scope behavior.

**Independent Test**: Review `API_LIFECYCLE_TESTING_GUIDE.md` and `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`; every required actor/module scenario has a current expected result and no stale "authenticated user can access everything" wording remains.

### Documentation and Checklist Updates for User Story 1

- [X] T019 [US1] Update the setup/preconditions section in `API_LIFECYCLE_TESTING_GUIDE.md` to name `HrSystemDb_Phase12LifecycleTest` as the disposable Phase 12 manual retest database
- [X] T020 [US1] Update the migration/preflight section in `API_LIFECYCLE_TESTING_GUIDE.md` to include approved migrations `20260615170903_AddVacationRequestCreatedByEmployee` and `20260615212225_AddTripRequesterEmployee`
- [X] T021 [US1] Update the login/session section in `API_LIFECYCLE_TESTING_GUIDE.md` to state that Swagger or the HTTP client must preserve cookies and that cookies must be cleared between actor switches
- [X] T022 [US1] Update the authentication checks in `API_LIFECYCLE_TESTING_GUIDE.md` to include login and `/api/auth/me` expectations for EMP001, EMP002, EMP003, and EMP004
- [X] T023 [US1] Update the employee lifecycle section in `API_LIFECYCLE_TESTING_GUIDE.md` with a role matrix for Employee, Manager, HRAdministrator, and SystemAdministrator list/detail/create/update/delete behavior
- [X] T024 [US1] Update the employee lifecycle section in `API_LIFECYCLE_TESTING_GUIDE.md` with role assignment expectations: only SystemAdministrator can assign roles, HRAdministrator receives 403, and last-active SystemAdministrator protection remains expected
- [X] T025 [US1] Add employee manual scenarios EMP-001 through EMP-010 to `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T026 [US1] Update the vacation lifecycle section in `API_LIFECYCLE_TESTING_GUIDE.md` with Employee own-only, Manager team-only review, HR/System organization-wide, HR/System create-for-employee, and self-review blocked expectations
- [X] T027 [US1] Update the vacation lifecycle section in `API_LIFECYCLE_TESTING_GUIDE.md` with out-of-scope list/detail/filter expectations and structured 403/404/422 payload checks
- [X] T028 [US1] Add vacation manual scenarios VAC-001 through VAC-012 to `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T029 [US1] Update the trip lifecycle section in `API_LIFECYCLE_TESTING_GUIDE.md` with Employee own-only, Manager own/team-only, HR/System organization-wide, and delete scope expectations
- [X] T030 [US1] Update the trip lifecycle section in `API_LIFECYCLE_TESTING_GUIDE.md` with traveler/requester compatibility wording and historical null requester behavior
- [X] T031 [US1] Update the trip lifecycle section in `API_LIFECYCLE_TESTING_GUIDE.md` with out-of-scope filters returning empty scoped pages where applicable and forbidden detail/create/delete outcomes where applicable
- [X] T032 [US1] Add trip manual scenarios TRIP-001 through TRIP-012 to `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T033 [US1] Update attendance, compensation, documents, dashboard, and audit sections in `API_LIFECYCLE_TESTING_GUIDE.md` with current role/scope expectations or an explicit "no Phase 8-11 scope change applies" note
- [X] T034 [US1] Add sensitive-module manual scenarios SENS-001 through SENS-010 to `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T035 [US1] Add error compatibility manual scenarios ERR-001 through ERR-008 to `specs/012-lifecycle-manual-retest/manual-retest-checklist.md` for 401, 403, 404, 409, and 422 `{ code, message }` checks
- [X] T036 [US1] Search `API_LIFECYCLE_TESTING_GUIDE.md` for stale broad-access wording and correct any statement implying any authenticated user can access all employee, vacation, or trip data
- [X] T037 [US1] Record the completed `API_LIFECYCLE_TESTING_GUIDE.md` section updates in `specs/012-lifecycle-manual-retest/implementation-summary.md`

**Checkpoint**: User Story 1 documentation and checklist scenarios are complete and independently reviewable.

---

## Phase 4: User Story 2 - Recreate a Fresh Manual Test Dataset (Priority: P1)

**Goal**: Prepare and execute the local fresh-database manual retest using `HrSystemDb_Phase12LifecycleTest` and the required four-role dataset.

**Independent Test**: Starting from `HrSystemDb_Phase12LifecycleTest`, apply existing approved migrations, create/verify EMP001-EMP004 with required roles/relationships, run the checklist scenarios, and record pass/fail evidence without source edits or new migrations.

### Local Database and Dataset Preparation for User Story 2

- [X] T038 [US2] Set the local `ConnectionStrings__DefaultConnection` environment variable for `HrSystemDb_Phase12LifecycleTest` and record the redacted connection source in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T039 [US2] Apply existing approved migrations with `dotnet ef database update --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj` and record the result in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T040 [US2] Run `dotnet ef migrations list --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj` and record the migration list in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T041 [US2] Create or verify the local department data required for EMP001-EMP004 setup and record the department IDs in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T042 [US2] Configure local-only initial admin bootstrap values for EMP001 without committing secrets and record the redacted configuration source in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T043 [US2] Start `HR.API/HR.API.csproj` against `HrSystemDb_Phase12LifecycleTest` and record the local API URL in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T044 [US2] Verify EMP001 exists as active SystemAdministrator and record the result in `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T045 [US2] Create or verify EMP002 as active HRAdministrator and record the result in `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T046 [US2] Create or verify EMP003 as active Manager and record the result in `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T047 [US2] Create or verify EMP004 as active Employee under EMP003 and record the manager relationship in `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T048 [US2] Create or verify at least one outside-scope employee target for Employee/Manager negative checks and record the target in `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T049 [US2] Verify local-only passwords are not written as production/customer secrets in `API_LIFECYCLE_TESTING_GUIDE.md`

### Manual Retest Execution for User Story 2

- [X] T050 [US2] Execute AUTH scenarios from `specs/012-lifecycle-manual-retest/manual-retest-checklist.md` and record actual results in `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T051 [US2] Execute EMP scenarios from `specs/012-lifecycle-manual-retest/manual-retest-checklist.md` and record actual results in `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T052 [US2] Execute VAC scenarios from `specs/012-lifecycle-manual-retest/manual-retest-checklist.md` and record actual results in `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T053 [US2] Execute TRIP scenarios from `specs/012-lifecycle-manual-retest/manual-retest-checklist.md` and record actual results in `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T054 [US2] Execute SENS scenarios from `specs/012-lifecycle-manual-retest/manual-retest-checklist.md` and record actual results in `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T055 [US2] Execute ERR scenarios from `specs/012-lifecycle-manual-retest/manual-retest-checklist.md` and record actual results in `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- [X] T056 [US2] For every failed or blocked manual scenario, record the failure and follow-up defect in `specs/012-lifecycle-manual-retest/implementation-summary.md` without editing source code
- [X] T057 [US2] Summarize manual retest totals by module and actor in `specs/012-lifecycle-manual-retest/implementation-summary.md`

**Checkpoint**: User Story 2 has local DB setup and manual retest evidence recorded.

---

## Phase 5: User Story 3 - Align Client Setup and Handoff Documentation (Priority: P2)

**Goal**: Update client-facing installation/handoff documentation so migration, bootstrap, permission, and validation instructions match completed Phase 8-11 behavior and Phase 12 evidence.

**Independent Test**: Review `CLIENT_INSTALLATION_GUIDE.md`, `specs/012-lifecycle-manual-retest/implementation-summary.md`, and `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`; the docs include latest migration context, no stale Phase 7-only migration list remains, and evidence is complete.

### Client Guide and Handoff Updates for User Story 3

- [X] T058 [US3] Update the migration list in `CLIENT_INSTALLATION_GUIDE.md` to include `20260615170903_AddVacationRequestCreatedByEmployee` and `20260615212225_AddTripRequesterEmployee`
- [X] T059 [US3] Update the migration guidance in `CLIENT_INSTALLATION_GUIDE.md` to distinguish client database updates from disposable Phase 12 local validation database work
- [X] T060 [US3] Update bootstrap guidance in `CLIENT_INSTALLATION_GUIDE.md` if it does not clearly describe local-only initial admin setup and no customer-specific committed credentials
- [X] T061 [US3] Update endpoint permission notes in `CLIENT_INSTALLATION_GUIDE.md` for employee, vacation, trip, compensation, documents, dashboard, and audit behavior where stale after Phase 8-11
- [X] T062 [US3] Add or update a manual validation section in `CLIENT_INSTALLATION_GUIDE.md` that points to the lifecycle guide for role-scope retest expectations
- [X] T063 [US3] Record every changed `CLIENT_INSTALLATION_GUIDE.md` section in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T064 [US3] Add final Phase 12 completion status, checklist totals, validation command results, and deferred Phase 13 notes to `specs/012-lifecycle-manual-retest/implementation-summary.md`

**Checkpoint**: Client installation and handoff documentation are aligned with Phase 12 evidence.

---

## Phase 6: Polish and Cross-Cutting Validation

**Purpose**: Validate artifact consistency, task completion, and repository cleanliness.

- [X] T065 Search `API_LIFECYCLE_TESTING_GUIDE.md`, `CLIENT_INSTALLATION_GUIDE.md`, and `specs/012-lifecycle-manual-retest/` for stale "authenticated user can access everything" style wording and record the result in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T066 Run `git diff --check` from the repository root and record the result in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T067 Run `git status --short` and confirm no source code or migration files were changed for Phase 12 in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T068 Re-run `dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj` and record the final result in `specs/012-lifecycle-manual-retest/implementation-summary.md`
- [X] T069 Verify all manual retest checklist rows in `specs/012-lifecycle-manual-retest/manual-retest-checklist.md` have expected and actual results or a blocked reason
- [X] T070 Mark completed Phase 12 tasks in `specs/012-lifecycle-manual-retest/tasks.md` only after each task is actually done
- [X] T071 Finalize `specs/012-lifecycle-manual-retest/implementation-summary.md` with confirmations: no source code modified, no new migrations created, no Phase 13 Swagger work, and any follow-up defects listed

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on Setup; blocks all user stories
- **US1 (Phase 3)**: Depends on Foundational; MVP documentation/checklist scope
- **US2 (Phase 4)**: Depends on Foundational and benefits from US1 checklist completion before manual execution
- **US3 (Phase 5)**: Depends on Foundational; should finish after US1/US2 evidence where possible
- **Polish (Phase 6)**: Depends on all desired user stories

### User Story Dependencies

- **US1**: Can be delivered first as MVP documentation/checklist update
- **US2**: Requires disposable DB setup and should use the US1 checklist for execution
- **US3**: Uses US1 documentation decisions and US2 retest evidence for handoff summary

### Within Each Story

- Documentation updates before manual evidence execution
- Environment/database setup before manual role-scope checks
- Manual failures recorded before any follow-up approval request
- Completion summary finalized after validation commands and manual checklist rows are complete

---

## Parallel Opportunities

- T003-T007 can run in parallel because they read different artifacts and record setup notes.
- T019-T035 can be split by module if separate workers coordinate edits to `API_LIFECYCLE_TESTING_GUIDE.md` and `manual-retest-checklist.md`.
- T045-T048 can be prepared after EMP001 exists, but actual role creation may need sequential API/session handling.
- T050-T055 can be split by module only if each worker uses isolated browser/client sessions or clears cookies before actor switching.
- T058-T062 can run after US1 documentation decisions are stable, but they all touch `CLIENT_INSTALLATION_GUIDE.md` and should be merged carefully.

## Parallel Example: User Story 1

```text
Task: "Update employee lifecycle section in API_LIFECYCLE_TESTING_GUIDE.md"
Task: "Update vacation lifecycle section in API_LIFECYCLE_TESTING_GUIDE.md"
Task: "Update trip lifecycle section in API_LIFECYCLE_TESTING_GUIDE.md"
Task: "Add scenario rows to specs/012-lifecycle-manual-retest/manual-retest-checklist.md"
```

## Parallel Example: User Story 2

```text
Task: "Execute EMP scenarios in specs/012-lifecycle-manual-retest/manual-retest-checklist.md"
Task: "Execute VAC scenarios in specs/012-lifecycle-manual-retest/manual-retest-checklist.md"
Task: "Execute TRIP scenarios in specs/012-lifecycle-manual-retest/manual-retest-checklist.md"
```

Only parallelize manual execution if cookie/session isolation is reliable.

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete US1 documentation/checklist updates.
3. Validate that the lifecycle guide and checklist contain every required Phase 8-11 role/scope expectation.
4. Stop if the user wants to review before local DB manual execution.

### Full Phase 12 Delivery

1. Complete Setup and Foundational tasks.
2. Complete US1 documentation/checklist updates.
3. Complete US2 disposable DB setup and manual retest evidence.
4. Complete US3 client installation/handoff alignment.
5. Complete Polish and cross-cutting validation.

### Defect Handling

If a manual scenario fails because implementation behavior appears wrong:

1. Record the actor, request, expected result, actual result, payload, and reproduction notes in `specs/012-lifecycle-manual-retest/implementation-summary.md`.
2. Mark the scenario failed or blocked in `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`.
3. Do not edit source code.
4. Stop and request explicit approval for a separate remediation scope.

## Notes

- [P] tasks are safe to parallelize only when file edits do not conflict.
- Every completed task should be marked in this file after the task is actually complete.
- `tasks.md` is not a runtime failure log; use `implementation-summary.md` for command output, failures, and evidence.
- Existing Phase 11 dirty worktree changes may exist; do not revert or overwrite unrelated user/previous-phase changes.
