# Tasks: Phase 11 - Trip Ownership and Scope Hardening

**Input**: Design documents from `specs/011-trip-ownership-scope-hardening/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/phase11-trip-ownership-scope-contract.md`, `quickstart.md`

**Tests**: Required. The Phase 11 spec contains mandatory user scenarios and measurable success criteria, so focused tests must be written before implementation for each story.

**Organization**: Tasks are grouped by user story so each scope slice can be implemented and verified independently.

## Implementation Guardrails

- Phase 11 is trip ownership and scope hardening only.
- Do not start Phase 12 or Phase 13.
- Do not change employee endpoint behavior.
- Do not change vacation endpoint behavior.
- Do not change auth, cookies, claims, bootstrap, compensation, documents, attendance, dashboard, audit, Swagger, or frontend behavior.
- Do not normalize existing error codes.
- Do not create or apply any database migration until the explicit requester-storage migration approval gate is satisfied.
- Keep existing `requestedByEmployeeId` request field accepted as the target traveler.
- Treat authenticated requester identity as coming from the current session/claims, never from the request body.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks because it touches different files and has no dependency on unfinished tasks.
- **[Story]**: User story label from `spec.md`.
- Every task includes an exact target file path.

## Phase 1: Setup and Baseline

**Purpose**: Establish the current state before changing trip code.

- [ ] T001 Read Phase 11 artifacts `specs/011-trip-ownership-scope-hardening/spec.md`, `specs/011-trip-ownership-scope-hardening/plan.md`, `specs/011-trip-ownership-scope-hardening/data-model.md`, `specs/011-trip-ownership-scope-hardening/contracts/phase11-trip-ownership-scope-contract.md`, and `specs/011-trip-ownership-scope-hardening/quickstart.md`
- [ ] T002 Confirm the completed custom checklist `specs/011-trip-ownership-scope-hardening/checklists/trip-ownership.md` has no unchecked or unresolved items before implementation
- [ ] T003 Run baseline restore using `dotnet restore .\HR.slnx` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T004 Run baseline build using `dotnet build .\HR.slnx -c Release -p:UseSharedCompilation=false` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T005 Run baseline tests using `dotnet test .\HR.slnx -c Release --no-build` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T006 Record baseline results and any pre-existing failures in `specs/011-trip-ownership-scope-hardening/implementation-summary.md`

---

## Phase 2: Foundational Trip Scope Plumbing

**Purpose**: Shared changes required before individual user stories can be implemented.

**Critical**: No user story implementation should start until this phase is complete.

- [ ] T007 Inspect current trip controller behavior and note existing route/response compatibility in `specs/011-trip-ownership-scope-hardening/implementation-summary.md`
- [ ] T008 Inspect current trip service, repository, entity, DTO, and tests in `HR.Application/Transportation/ITripService.cs`, `HR.Infrastructure/Transportation/TripService.cs`, `HR.Infrastructure/Repositories/ITripRepository.cs`, `HR.Infrastructure/Repositories/TripRepository.cs`, `HR.Domain/Entities/Trip.cs`, `HR.Application/DTOs/Transportation/TripCreateRequest.cs`, `HR.Application/DTOs/Transportation/TripResponse.cs`, and `HR.Tests/Transportation/TripServiceBusinessRuleTests.cs`
- [ ] T009 Add requester-aware trip service method signatures returning `Result<T>` where authorization can fail in `HR.Application/Transportation/ITripService.cs`
- [ ] T010 Update all existing trip service call sites and test fakes for the new `ITripService` signatures in `HR.API/Controllers/TripsController.cs`, `HR.Infrastructure/Transportation/TripService.cs`, and `HR.Tests/Transportation/TripServiceBusinessRuleTests.cs`
- [ ] T011 Add scoped trip repository method signatures for allowed traveler IDs, optional traveler filter, detail lookup, and tracked delete lookup in `HR.Infrastructure/Repositories/ITripRepository.cs`
- [ ] T012 Implement scoped trip repository queries with `RequestedByEmployeeId` interpreted as traveler data and filtering before pagination in `HR.Infrastructure/Repositories/TripRepository.cs`
- [ ] T013 Inject and store `IEmployeeAccessService` in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T014 Add private requester-context and allowed-traveler-scope helper methods in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T015 Ensure requester validation rejects missing, soft-deleted, and terminated requesters before granting self/team/org trip scope in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T016 Preserve suspended requester behavior according to Phase 8 by not adding a global suspended-user trip-scope rejection in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T017 Add controller current employee ID extraction and structured unauthorized mapping for trip endpoints in `HR.API/Controllers/TripsController.cs`

**Checkpoint**: Trip service and repository can receive requester context, but story-specific rules may still be incomplete.

---

## Phase 3: User Story 1 - Employee Own Trip Privacy (Priority: P1) MVP

**Goal**: Normal employees can list, view, create, and delete only their own trips.

**Independent Test**: Login or simulate a normal employee with own and other-employee trips; list returns only own trips, own detail/create/delete succeeds, other employee detail/create/delete is denied.

### Tests for User Story 1

- [ ] T018 [US1] Create `HR.Tests/Transportation/TripAccessScopeTests.cs` with reusable test data builders for employee, manager, HR administrator, system administrator, active employees, soft-deleted employees, terminated employees, and trips
- [ ] T019 [US1] Add failing employee list tests proving own trips are returned, other employees' trips are excluded, pagination counts are scoped, and out-of-scope `travelerEmployeeId` filter returns an empty page in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T020 [US1] Add failing employee detail tests proving own trip returns success, missing trip returns `NOT_FOUND`, and existing out-of-scope trip returns `FORBIDDEN` in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T021 [US1] Add failing employee create tests proving self create succeeds, another employee target returns `FORBIDDEN` before creation, and `requestedByEmployeeId` is treated as traveler rather than requester in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T022 [US1] Add failing employee delete tests proving own trip hard-delete succeeds, missing trip returns `NOT_FOUND`, and existing out-of-scope trip returns `FORBIDDEN` before mutation in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T023 [US1] Add failing controller tests for employee claim propagation, `401` missing employee claim, `403` structured forbidden payload, and `404` structured not-found payload in `HR.Tests/Transportation/TripsControllerScopeTests.cs`

### Implementation for User Story 1

- [ ] T024 [US1] Implement employee-only scoped list behavior in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T025 [US1] Implement employee detail authorization with missing trip `NOT_FOUND` and existing out-of-scope trip `FORBIDDEN` in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T026 [US1] Implement employee create authorization so only self target traveler is allowed and non-self targets return `FORBIDDEN` before creating trip data in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T027 [US1] Preserve existing trip create validations for active non-deleted target, future trip date, working day, required fields, trip code, and request code in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T028 [US1] Implement employee delete authorization so only own trips are hard-deleted and out-of-scope trips return `FORBIDDEN` before `Remove` in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T029 [US1] Update `GET /api/trips` to accept optional `travelerEmployeeId` query filter and pass requester context to the service in `HR.API/Controllers/TripsController.cs`
- [ ] T030 [US1] Update `GET /api/trips/{id}` to map service `Result<TripResponse>` failures through existing `ToActionResult` in `HR.API/Controllers/TripsController.cs`
- [ ] T031 [US1] Update `POST /api/trips` to pass requester employee ID separately from `TripCreateRequest.RequestedByEmployeeId` in `HR.API/Controllers/TripsController.cs`
- [ ] T032 [US1] Update `DELETE /api/trips/{id}` to pass requester employee ID and preserve `204 NoContent` for successful hard-delete in `HR.API/Controllers/TripsController.cs`
- [ ] T033 [US1] Update existing trip business-rule tests for the new requester-aware service method signatures in `HR.Tests/Transportation/TripServiceBusinessRuleTests.cs`
- [ ] T034 [US1] Run focused employee trip tests using `dotnet test .\HR.Tests\HR.Tests.csproj -c Release --filter "FullyQualifiedName~TripAccessScopeTests"` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T035 [US1] Record User Story 1 test results in `specs/011-trip-ownership-scope-hardening/implementation-summary.md`

**Checkpoint**: User Story 1 should be fully functional and independently testable.

---

## Phase 4: User Story 2 - Manager Own and Team Trip Operations (Priority: P1)

**Goal**: Managers can operate on their own and active direct/indirect team trips only.

**Independent Test**: Simulate a manager with direct report, indirect report, peer, unrelated employee, soft-deleted report, terminated report, and related trips; verify list/detail/create/delete scope.

### Tests for User Story 2

- [ ] T036 [US2] Add failing manager list tests for own trip, active direct report trip, active indirect report trip, peer trip, unrelated trip, soft-deleted report trip, and terminated report trip in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T037 [US2] Add failing manager list filter tests proving active report filter returns that report's trips and peer/unrelated/deleted/terminated report filters return empty scoped pages in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T038 [US2] Add failing manager detail tests proving own/direct/indirect trips succeed and peer/unrelated/deleted/terminated report trips return `FORBIDDEN` in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T039 [US2] Add failing manager create tests proving self/direct/indirect target travelers succeed and peer/unrelated/deleted/terminated targets are rejected before trip creation in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T040 [US2] Add failing manager delete tests proving own/direct/indirect trips hard-delete after authorization and peer/unrelated/deleted/terminated report trips return `FORBIDDEN` before mutation in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T041 [US2] Add failing manager edge-case tests for manager with no active team and manager with only soft-deleted or terminated reports in `HR.Tests/Transportation/TripAccessScopeTests.cs`

### Implementation for User Story 2

- [ ] T042 [US2] Implement manager allowed traveler set using Phase 8 visible employee IDs plus manager self in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T043 [US2] Implement manager scoped list so own, active direct report, and active indirect report trips are included before pagination in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T044 [US2] Implement manager out-of-scope list filter behavior returning empty paged results without target existence probing in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T045 [US2] Implement manager detail authorization for own and active team trips in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T046 [US2] Implement manager create authorization for self and active direct/indirect target travelers in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T047 [US2] Implement manager delete authorization for own and active team trips while preserving current hard-delete behavior in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T048 [US2] Ensure soft-deleted and terminated reports are excluded from manager scope by repository/service tests in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T049 [US2] Ensure manager out-of-scope detail/delete returns `FORBIDDEN` only after confirming the trip exists and before mutation in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T050 [US2] Run focused manager trip tests using `dotnet test .\HR.Tests\HR.Tests.csproj -c Release --filter "FullyQualifiedName~TripAccessScopeTests"` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T051 [US2] Add scoped repository paging/filter tests for allowed traveler IDs, optional `travelerEmployeeId`, empty out-of-scope filters, and counts after scope filtering in `HR.Tests/Repositories/TripRepositoryTests.cs`, then run `dotnet test .\HR.Tests\HR.Tests.csproj -c Release --filter "FullyQualifiedName~TripRepositoryTests"` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T052 [US2] Record User Story 2 test results in `specs/011-trip-ownership-scope-hardening/implementation-summary.md`

**Checkpoint**: User Story 2 should work independently and must not weaken User Story 1.

---

## Phase 5: User Story 3 - HR and System Organization Trip Operations (Priority: P1)

**Goal**: HR administrators and system administrators retain organization-wide trip list/detail/create/delete behavior for eligible employees.

**Independent Test**: Simulate HR administrator and system administrator users; verify organization-wide list/detail/delete and valid on-behalf create for eligible targets.

### Tests for User Story 3

- [ ] T053 [US3] Add failing HR administrator tests for organization-wide list, any existing detail, eligible target create, missing target create failure, ineligible target create failure, and any existing delete in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T054 [US3] Add failing system administrator tests for organization-wide list, any existing detail, eligible target create, missing target create failure, ineligible target create failure, and any existing delete in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T055 [US3] Add failing organization-scope compatibility tests proving existing successful `TripResponse` fields remain stable in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T056 [US3] Add failing structured error compatibility tests for `401`, `403`, `404`, and trip business-rule validation failures in `HR.Tests/Transportation/TripsControllerScopeTests.cs`

### Implementation for User Story 3

- [ ] T057 [US3] Implement HR administrator organization-wide list/detail/create/delete branches in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T058 [US3] Implement system administrator organization-wide list/detail/create/delete branches in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T059 [US3] Preserve existing target employee eligibility failures for HR/System create attempts against missing, inactive, soft-deleted, or terminated target travelers in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T060 [US3] Preserve existing future-date and working-day trip validation behavior for HR/System creates after authorization passes in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T061 [US3] Ensure HR/System delete any existing trip still uses current hard-delete behavior after authorization succeeds in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T062 [US3] Verify `TripResponse.FromEntity` still emits existing fields without removing or renaming properties in `HR.Application/DTOs/Transportation/TripResponse.cs`
- [ ] T063 [US3] Run focused HR/System trip tests using `dotnet test .\HR.Tests\HR.Tests.csproj -c Release --filter "FullyQualifiedName~TripAccessScopeTests"` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T064 [US3] Run controller compatibility tests using `dotnet test .\HR.Tests\HR.Tests.csproj -c Release --filter "FullyQualifiedName~TripsControllerScopeTests"` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T065 [US3] Record User Story 3 test results in `specs/011-trip-ownership-scope-hardening/implementation-summary.md`

**Checkpoint**: User Story 3 should work independently and must not weaken employee or manager scope.

---

## Phase 6: User Story 4 - Requester and Traveler Clarity (Priority: P2)

**Goal**: New trips distinguish traveler from authenticated requester after explicitly approved requester storage exists.

**Independent Test**: Create trips as employee, manager, HR administrator, and system administrator; verify traveler metadata and requester metadata are correct for new rows and null-safe for historical rows after approved migration.

### Migration Approval Gate

- [ ] T066 [US4] Prepare requester storage approval packet with table, column, FK, index, backfill, local database impact, and test plan in `specs/011-trip-ownership-scope-hardening/implementation-summary.md`
- [ ] T067 [US4] STOP: do not create or apply `AddTripRequesterEmployee` migration until explicit user migration approval is recorded in `specs/011-trip-ownership-scope-hardening/implementation-summary.md`

If explicit approval is not granted, leave T068 through T078 unchecked and stop Phase 11 implementation at this gate.

### Tests for User Story 4

- [ ] T068 [US4] After explicit migration approval only, add failing tests proving self-created trips set traveler and requester to the same employee in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T069 [US4] After explicit migration approval only, add failing tests proving manager-created team trips set traveler to the team member and requester to the manager in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T070 [US4] After explicit migration approval only, add failing tests proving HR/System on-behalf trips set traveler to target employee and requester to authenticated administrator in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T071 [US4] After explicit migration approval only, add failing tests proving historical trips with null requester metadata remain readable and safe in `HR.Tests/Transportation/TripAccessScopeTests.cs`
- [ ] T072 [US4] After explicit migration approval only, add failing repository tests for including requester and traveler navigation data in `HR.Tests/Repositories/TripRepositoryTests.cs`

### Implementation for User Story 4

- [ ] T073 [US4] After explicit migration approval only, add nullable `RequesterEmployeeId` and `Requester` navigation to `HR.Domain/Entities/Trip.cs`
- [ ] T074 [US4] After explicit migration approval only, configure optional requester FK, restrict delete behavior, and requester index in `HR.Infrastructure/Data/Configurations/TripConfiguration.cs`
- [ ] T075 [US4] After explicit migration approval only, create EF migration `AddTripRequesterEmployee` in `HR.Infrastructure/Data/Migrations/*_AddTripRequesterEmployee.cs`
- [ ] T076 [US4] After explicit migration approval only, update repository includes for requester and traveler navigation in `HR.Infrastructure/Repositories/TripRepository.cs`
- [ ] T077 [US4] After explicit migration approval only, set `RequesterEmployeeId` from authenticated requester during trip creation in `HR.Infrastructure/Transportation/TripService.cs`
- [ ] T078 [US4] After explicit migration approval only, add additive requester/traveler metadata to `TripResponse` without removing existing fields in `HR.Application/DTOs/Transportation/TripResponse.cs`

**Checkpoint**: User Story 4 is blocked until explicit migration approval is granted.

---

## Phase 7: Polish, Compatibility, and Validation

**Purpose**: Verify Phase 11 is complete without unrelated behavior changes.

- [ ] T079 Run full restore using `dotnet restore .\HR.slnx` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T080 Run full release build using `dotnet build .\HR.slnx -c Release -p:UseSharedCompilation=false` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T081 Run full release tests using `dotnet test .\HR.slnx -c Release --no-build` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T082 If T081 is unsafe because build output is stale, run full release tests using `dotnet test .\HR.slnx -c Release` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T083 Run EF pending model check using `dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T084 Confirm no unapproved migration files exist under `HR.Infrastructure/Data/Migrations`
- [ ] T085 Confirm `HR.Application` still does not reference `HR.Infrastructure` by running `rg -n "HR.Infrastructure" .\HR.Application` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T086 Confirm `Program.cs` still delegates service registration through `AddApplication()` and `AddInfrastructure(builder.Configuration)` in `HR.API/Program.cs`
- [ ] T087 Confirm no employee, vacation, compensation, document, attendance, dashboard, audit, bootstrap, Swagger, auth cookie, or claim behavior was intentionally changed by reviewing `git diff -- HR.API HR.Application HR.Infrastructure HR.Domain HR.Tests` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T088 Update narrow trip manual testing notes only if stale in `API_LIFECYCLE_TESTING_GUIDE.md`
- [ ] T089 Update Phase 11 completion notes, changed files, validation commands, migration approval status, and residual risks in `specs/011-trip-ownership-scope-hardening/implementation-summary.md`
- [ ] T090 Run repository-wide whitespace check using `git diff --check` from `D:\DEPI_Angular_project_Github\DEPI_Angular_project\Backend\HR`
- [ ] T091 Mark only actually completed tasks in `specs/011-trip-ownership-scope-hardening/tasks.md`

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1 and blocks all user stories.
- **User Story 1**: Depends on Phase 2 and is the MVP.
- **User Story 2**: Depends on Phase 2; should be implemented after US1 for simpler validation.
- **User Story 3**: Depends on Phase 2; can be implemented after US1 or US2.
- **User Story 4**: Depends on explicit migration approval after T066-T067.
- **Polish**: Depends on completed intended user stories.

### User Story Dependencies

- **US1 Employee Own Trip Privacy**: No dependency on other user stories after foundation.
- **US2 Manager Own and Team Trip Operations**: Reuses foundation and can build on US1 service helpers.
- **US3 HR/System Organization Trip Operations**: Reuses foundation and can build on US1 service helpers.
- **US4 Requester and Traveler Clarity**: Blocked by explicit migration approval.

### Within Each User Story

- Write focused failing tests first.
- Implement service rules before controller mapping.
- Keep repository filtering before pagination.
- Preserve existing route and response compatibility.
- Run focused tests before moving to the next story.

## Parallel Opportunities

- T003, T004, and T005 are sequential validation commands and should not run in parallel.
- T018 and T023 can be split between different implementers because they create different test files.
- T036 through T041 all edit `HR.Tests/Transportation/TripAccessScopeTests.cs`; coordinate if parallelized.
- T053 through T056 can be split because T056 targets controller tests while T053-T055 target service/access tests.
- T083 through T087 are validation/static checks and can be performed independently after build/test succeeds.

## Parallel Example: User Story 1

```text
Task: "T018 [US1] Create HR.Tests/Transportation/TripAccessScopeTests.cs"
Task: "T023 [US1] Add controller tests in HR.Tests/Transportation/TripsControllerScopeTests.cs"
```

## Parallel Example: User Story 3

```text
Task: "T053 [US3] Add HR administrator tests in HR.Tests/Transportation/TripAccessScopeTests.cs"
Task: "T056 [US3] Add structured error compatibility tests in HR.Tests/Transportation/TripsControllerScopeTests.cs"
```

## Implementation Strategy

### MVP First

1. Complete Phase 1.
2. Complete Phase 2.
3. Complete User Story 1.
4. Stop and validate employee own-trip privacy independently.

### Incremental Delivery

1. Add employee scope first.
2. Add manager scope second.
3. Add HR/System organization scope third.
4. Stop at requester-storage migration gate.
5. Continue requester/traveler persistence only after explicit migration approval.

### Cheap Model Execution Notes

- Do not infer requirements outside the listed files.
- If a test exposes a real defect outside trip scope, stop and report before patching unrelated code.
- If a migration seems necessary before T067 approval, stop and ask for explicit approval.
- Mark tasks complete only after the described file change or command result is actually done.
