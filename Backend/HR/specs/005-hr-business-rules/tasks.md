# Tasks: Phase 5 - HR Business Logic Improvements

**Input**: Design documents from `specs/005-hr-business-rules/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/api-contract.md`, `quickstart.md`

**Tests**: Included because the Phase 5 specification defines independent tests and measurable success criteria for every user story. Write the story tests first and confirm they fail before implementing the story.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested independently after the shared foundation is complete.

## Phase 1: Setup

**Purpose**: Confirm the working baseline and prepare Phase 5 folders.

- [X] T001 Run baseline `dotnet restore`, `dotnet build`, and `dotnet test` for `HR.slnx`; record any pre-existing failures in the implementation report, handoff notes, or final Phase 5 completion summary, not in `specs/005-hr-business-rules/tasks.md`
- [X] T002 [P] Create Phase 5 infrastructure folder `HR.Infrastructure/BusinessRules`
- [X] T003 [P] Create Phase 5 test folders `HR.Tests/BusinessRules`, `HR.Tests/VacationRequests`, `HR.Tests/Transportation`, and `HR.Tests/Departments`

---

## Phase 2: Foundational

**Purpose**: Add shared schema, mappings, deterministic date support, and test infrastructure required by all user stories.

**Critical**: Do not start user-story implementation until this phase compiles.

- [X] T004 [P] Add `VacationBalanceDays`, `IsDeleted`, and `TerminatedAt` fields with safe defaults in `HR.Domain/Entities/Employee.cs`
- [X] T005 [P] Add `WorkingDayCount`, `ReviewedByEmployeeId`, `ReviewedBy`, and `ReviewedAt` fields in `HR.Domain/Entities/VacationRequest.cs`
- [X] T006 [P] Add nullable `RequestedByEmployeeId` and `RequestedBy` fields for historical trip compatibility in `HR.Domain/Entities/Trip.cs`
- [X] T007 [P] Add Sunday-through-Thursday duration and notice tests in `HR.Tests/BusinessRules/WorkingDayCalendarTests.cs`
- [X] T008 Implement `IsWorkingDay`, inclusive working-day count, and full-working-days-between helper methods in `HR.Infrastructure/BusinessRules/WorkingDayCalendar.cs`
- [X] T009 [P] Add a fixed `TimeProvider` test helper in `HR.Tests/TestInfrastructure/TestTimeProvider.cs`
- [X] T010 Update `SqliteTestEnvironment` to accept an optional `TimeProvider` and seed Phase 5 fields in `HR.Tests/TestInfrastructure/SqliteTestEnvironment.cs`
- [X] T011 Register `TimeProvider.System` with `TryAddSingleton` and register `WorkingDayCalendar` in `HR.Infrastructure/DependencyInjection.cs`
- [X] T012 Add Phase 5 employee defaults, indexes for active email lookup, and soft-delete-friendly mapping in `HR.Infrastructure/Data/Configurations/EmployeeConfiguration.cs`
- [X] T013 Add vacation working-day, reviewer relationship, reviewer index, and delete behavior mapping in `HR.Infrastructure/Data/Configurations/VacationRequestConfiguration.cs`
- [X] T014 Add nullable trip requester relationship and requester index mapping for existing rows in `HR.Infrastructure/Data/Configurations/TripConfiguration.cs`
- [X] T015 Extend EF model parity assertions for all Phase 5 fields, relationships, indexes, defaults, and delete behaviors in `HR.Tests/Data/ApplicationDbContextModelParityTests.cs`
- [X] T016 Define and verify trip requester migration strategy for existing trip rows in `HR.Infrastructure/Data/Migrations`
- [X] T017 Generate exactly one Phase 5 EF migration and updated model snapshot in `HR.Infrastructure/Data/Migrations`
- [X] T018 [P] Add `VacationBalanceDays`, `IsDeleted`, and `TerminatedAt` response fields in `HR.Application/DTOs/Employees/EmployeeResponse.cs`
- [X] T019 [P] Add `WorkingDayCount`, `ReviewedByEmployeeId`, `ReviewedByEmployeeName`, and `ReviewedAt` response fields in `HR.Application/DTOs/VacationRequests/VacationRequestResponse.cs`

**Checkpoint**: Domain model, EF configuration, migration, date helper, and shared test infrastructure compile. Existing trips without a requester remain query-safe after migration, and new trip creation requires a requester.

---

## Phase 3: User Story 1 - Submit Valid Vacation Requests (Priority: P1) MVP

**Goal**: Accept valid vacation submissions and reject overlap, insufficient balance, non-active employee, past-date, invalid-range, and notice-window failures before mutation.

**Independent Test**: Submit vacation requests across valid and invalid date ranges for active, suspended, terminated, and soft-deleted employees with different balances and existing requests.

### Tests for User Story 1

- [X] T020 [P] [US1] Add service tests for valid submission, rejected overlap, allowed rejected-overlap, insufficient balance, non-active employee, soft-deleted employee, past start, invalid date range, and notice-window failures in `HR.Tests/VacationRequests/VacationRequestServiceBusinessRuleTests.cs`
- [X] T021 [P] [US1] Add repository tests for pending/approved overlap detection and rejected-request exclusion in `HR.Tests/Repositories/VacationRequestRepositoryTests.cs`

### Implementation for User Story 1

- [X] T022 [US1] Add `HasOverlappingPendingOrApprovedAsync` contract to `HR.Infrastructure/Repositories/IVacationRequestRepository.cs`
- [X] T023 [US1] Implement overlap query with inclusive boundary checks in `HR.Infrastructure/Repositories/VacationRequestRepository.cs`
- [X] T024 [US1] Inject `WorkingDayCalendar` and `TimeProvider`, then enforce active employee, soft-delete, date range, past-date, notice, balance, overlap, and `WorkingDayCount` rules in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T025 [US1] Run focused vacation submission tests from `HR.Tests/VacationRequests/VacationRequestServiceBusinessRuleTests.cs` and `HR.Tests/Repositories/VacationRequestRepositoryTests.cs`

**Checkpoint**: User Story 1 is functional and testable without implementing review, employee lifecycle, trips, or department counts.

---

## Phase 4: User Story 2 - Review Vacation Requests Predictably (Priority: P1)

**Goal**: Enforce vacation-review state transitions, prevent self-review, record reviewer audit fields, adjust balances exactly once, treat same-status updates as no-op successes, and retain approved/rejected requests.

**Independent Test**: Review pending, approved, rejected, and same-status requests with requester and non-requester employees, then verify status, reviewer audit fields, deletion rules, timestamps, and balance changes.

### Tests for User Story 2

- [X] T026 [P] [US2] Add service tests for approval deduction, approved-to-rejected restoration, rejected terminal behavior, self-review rejection, any non-requester review, same-status no-op success without timestamp or balance changes, pending deletion, and approved/rejected deletion rejection in `HR.Tests/VacationRequests/VacationRequestReviewBusinessRuleTests.cs`
- [X] T027 [P] [US2] Add controller tests proving reviewer identity comes from the `employee_id` claim and invalid reviewer sessions return `401` in `HR.Tests/VacationRequests/VacationRequestsControllerTests.cs`

### Implementation for User Story 2

- [X] T028 [US2] Change `UpdateVacationStatusAsync` to accept `Guid reviewerEmployeeId` in `HR.Application/VacationRequests/IVacationRequestService.cs`
- [X] T029 [US2] Read the authenticated `employee_id` claim and pass reviewer id to the service in `HR.API/Controllers/VacationRequestsController.cs`
- [X] T030 [US2] Add tracked review lookup and reviewer include contracts to `HR.Infrastructure/Repositories/IVacationRequestRepository.cs`
- [X] T031 [US2] Implement tracked review lookup and reviewer include loading in `HR.Infrastructure/Repositories/VacationRequestRepository.cs`
- [X] T032 [US2] Enforce the vacation status state machine, same-status no-op success, self-review guard, reviewer audit fields, and exact balance deduction/restoration through the unit-of-work boundary in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T033 [US2] Restrict hard deletion to pending vacation requests in `HR.Infrastructure/VacationRequests/VacationRequestService.cs`
- [X] T034 [US2] Map reviewer fields and stored working-day counts in `HR.Application/DTOs/VacationRequests/VacationRequestResponse.cs`
- [X] T035 [US2] Run focused vacation review tests from `HR.Tests/VacationRequests/VacationRequestReviewBusinessRuleTests.cs` and `HR.Tests/VacationRequests/VacationRequestsControllerTests.cs`

**Checkpoint**: User Story 2 is functional and preserves User Story 1 submission behavior.

---

## Phase 5: User Story 3 - Protect Employee Lifecycle Integrity (Priority: P1)

**Goal**: Enforce employee lifecycle rules, soft deletion, duplicate active email, circular-manager prevention, immutable employee numbers, termination side effects, retained Identity users, and immediate access revocation.

**Independent Test**: Update employee status, manager, email, and deletion state while verifying retained history, rejected pending vacations, normal-result visibility, denied sign-in, rejected existing sessions, same-status no-op behavior, and unchanged employee numbers.

### Tests for User Story 3

- [X] T036 [P] [US3] Add service tests for employee status transitions, same-status no-op success, duplicate active email, circular manager chain, cross-department manager warning, termination side effects, soft deletion, retained Identity user, and normal-result visibility in `HR.Tests/Employees/EmployeeServiceBusinessRuleTests.cs`
- [X] T037 [P] [US3] Add auth tests for terminated login denial, soft-deleted login denial, and session-validator rejection in `HR.Tests/Auth/AuthRevocationTests.cs`
- [X] T038 [P] [US3] Add repository tests for excluding soft-deleted employees, active-email lookup, manager-chain lookup, and authentication eligibility lookup in `HR.Tests/Repositories/EmployeeRepositoryTests.cs`
- [X] T039 [P] [US3] Add employee number immutability regression tests proving update mappings cannot mutate `EmployeeNumber` in `HR.Tests/Employees/EmployeeServiceBusinessRuleTests.cs`

### Implementation for User Story 3

- [X] T040 [US3] Add active-email, manager-chain, soft-delete-aware detail/list, including-deleted lookup, and authentication eligibility contracts to `HR.Infrastructure/Repositories/IEmployeeRepository.cs`
- [X] T041 [US3] Implement soft-delete-aware list/detail, active-email, manager-chain, including-deleted, and authentication eligibility queries in `HR.Infrastructure/Repositories/EmployeeRepository.cs`
- [X] T042 [US3] Add pending-vacation lookup and batch rejection contracts to `HR.Infrastructure/Repositories/IVacationRequestRepository.cs`
- [X] T043 [US3] Implement pending-vacation lookup and batch rejection support in `HR.Infrastructure/Repositories/VacationRequestRepository.cs`
- [X] T044 [US3] Enforce duplicate active email, manager existence, circular manager detection, cross-department manager warning, and default vacation balance during create in `HR.Infrastructure/Employees/EmployeeService.cs`
- [X] T045 [US3] Enforce employee status state machine, same-status no-op success, immutable `EmployeeNumber`, duplicate active email, circular manager detection, termination timestamp, pending-vacation rejection, and access revocation side effects atomically during update in `HR.Infrastructure/Employees/EmployeeService.cs`
- [X] T046 [US3] Replace hard employee deletion with soft deletion, retained Identity user, direct-report cleanup, termination timestamp, and pending-vacation rejection atomically in `HR.Infrastructure/Employees/EmployeeService.cs`
- [X] T047 [US3] Map `VacationBalanceDays`, `IsDeleted`, and `TerminatedAt` in employee responses from `HR.Infrastructure/Employees/EmployeeService.cs`
- [X] T048 [US3] Deny login for terminated or soft-deleted employees and map Phase 5 employee fields in `HR.Infrastructure/Auth/AuthService.cs`
- [X] T049 [P] [US3] Add `IEmployeeSessionValidator` contract in `HR.Infrastructure/Auth/IEmployeeSessionValidator.cs`
- [X] T050 [US3] Implement active, non-deleted employee session validation from the `employee_id` claim in `HR.Infrastructure/Auth/EmployeeSessionValidator.cs`
- [X] T051 [US3] Register `IEmployeeSessionValidator` in `HR.Infrastructure/DependencyInjection.cs`
- [X] T052 [US3] Wire cookie `OnValidatePrincipal` to reject terminated or soft-deleted employee sessions in `HR.API/Program.cs`
- [X] T053 [US3] Run focused employee lifecycle and auth revocation tests from `HR.Tests/Employees/EmployeeServiceBusinessRuleTests.cs`, `HR.Tests/Auth/AuthRevocationTests.cs`, and `HR.Tests/Repositories/EmployeeRepositoryTests.cs`

**Checkpoint**: User Story 3 is functional and preserves User Stories 1 and 2.

---

## Phase 6: User Story 4 - Record Traceable Trips (Priority: P2)

**Goal**: Require each new trip to identify an active, non-deleted employee, reject past or non-working trip dates, and safely handle existing trips with no historical requester.

**Independent Test**: Submit trips for active, suspended, terminated, missing, and soft-deleted employees across future working, past, and Friday/Saturday dates; list/detail existing trips with null requester without failures.

### Tests for User Story 4

- [X] T054 [P] [US4] Add trip service tests for active requester success, missing requester, suspended requester, terminated requester, soft-deleted requester, past date, Friday/Saturday date rejection, and existing null-requester response safety in `HR.Tests/Transportation/TripServiceBusinessRuleTests.cs`
- [X] T055 [P] [US4] Add repository tests for nullable requester include loading and requester-filtered persistence in `HR.Tests/Repositories/TripRepositoryTests.cs`

### Implementation for User Story 4

- [X] T056 [US4] Add required `RequestedByEmployeeId` to trip create requests in `HR.Application/DTOs/Transportation/TripCreateRequest.cs`
- [X] T057 [US4] Add nullable requester fields to trip responses in `HR.Application/DTOs/Transportation/TripResponse.cs`
- [X] T058 [US4] Add requester include contracts to `HR.Infrastructure/Repositories/ITripRepository.cs`
- [X] T059 [US4] Include requester employee data and safely handle null requester rows for trip list/detail queries in `HR.Infrastructure/Repositories/TripRepository.cs`
- [X] T060 [US4] Inject `IEmployeeRepository`, `WorkingDayCalendar`, and `TimeProvider`, then enforce requester and trip-date rules for new submissions in `HR.Infrastructure/Transportation/TripService.cs`
- [X] T061 [US4] Run focused trip tests from `HR.Tests/Transportation/TripServiceBusinessRuleTests.cs` and `HR.Tests/Repositories/TripRepositoryTests.cs`

**Checkpoint**: User Story 4 is functional and does not require Phase 7 transportation features.

---

## Phase 7: User Story 5 - See Department Staffing Counts (Priority: P3)

**Goal**: Return current department employee counts that exclude soft-deleted employee profiles.

**Independent Test**: View departments before and after employee creation, movement, termination, and soft deletion.

### Tests for User Story 5

- [X] T062 [P] [US5] Add department service tests for counts after employee creation, movement, termination, and soft deletion in `HR.Tests/Departments/DepartmentServiceBusinessRuleTests.cs`
- [X] T063 [P] [US5] Add repository tests for count projection excluding soft-deleted employees and retaining terminated visible employees in `HR.Tests/Repositories/DepartmentRepositoryTests.cs`

### Implementation for User Story 5

- [X] T064 [US5] Add `EmployeeCount` to `HR.Application/DTOs/Departments/DepartmentResponse.cs`
- [X] T065 [US5] Add employee-count projection contracts to `HR.Infrastructure/Repositories/IDepartmentRepository.cs`
- [X] T066 [US5] Implement department list/detail loading with non-deleted employee counts in `HR.Infrastructure/Repositories/DepartmentRepository.cs`
- [X] T067 [US5] Map `EmployeeCount` in department list/detail responses in `HR.Infrastructure/Departments/DepartmentService.cs`
- [X] T068 [US5] Run focused department count tests from `HR.Tests/Departments/DepartmentServiceBusinessRuleTests.cs` and `HR.Tests/Repositories/DepartmentRepositoryTests.cs`

**Checkpoint**: User Story 5 is functional and preserves the existing department routes.

---

## Final Phase: Polish and Cross-Cutting Validation

**Purpose**: Verify full Phase 5 behavior, compatibility, and scope boundaries.

- [X] T069 [P] Update authentication compatibility tests for additive employee response fields, stable login response wrapper, existing login fields, and unchanged claims in `HR.Tests/Auth/AuthControllerCompatibilityTests.cs`
- [X] T070 [P] Add structured error compatibility tests for representative Phase 5 `422`, `409`, and `401` failures while preserving `code`, `message`, and status behavior in `HR.Tests/Compatibility/ErrorResponseParityTests.cs`
- [X] T071 Run full `dotnet restore`, `dotnet build -c Release`, and `dotnet test -c Release --no-build` for `HR.slnx`
- [X] T072 Run static dependency and scope checks from `specs/005-hr-business-rules/plan.md`
- [X] T073 Verify the Phase 5 migration and model snapshot are the only migration changes in `HR.Infrastructure/Data/Migrations`
- [ ] T074 Run manual quickstart validation and record any environment-specific notes in implementation report, handoff notes, `CODEX_HANDOFF.md`, or final Phase 5 completion summary
- [X] T075 Run `git diff --check` and review the final changed file set in `specs/005-hr-business-rules/tasks.md`

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1 and blocks every user story.
- **US1 Submit Valid Vacation Requests**: Depends on Phase 2.
- **US2 Review Vacation Requests Predictably**: Depends on Phase 2 and can be implemented after US1 for simpler balance validation.
- **US3 Protect Employee Lifecycle Integrity**: Depends on Phase 2. It integrates with vacation pending-request rejection and auth revocation, so validate against US1/US2 once those are complete.
- **US4 Record Traceable Trips**: Depends on Phase 2 and benefits from US3 soft-delete repository queries.
- **US5 See Department Staffing Counts**: Depends on Phase 2 and benefits from US3 soft-delete semantics.
- **Final Polish**: Depends on all desired user stories being complete.

### Suggested User Story Order

1. US1: MVP vacation submission rules.
2. US2: Vacation review and deletion rules.
3. US3: Employee lifecycle and auth revocation.
4. US4: Trip requester and date rules.
5. US5: Department employee counts.

### Parallel Opportunities

- T002 and T003 can run in parallel.
- T004, T005, T006, T007, T009, T018, and T019 touch different files and can be prepared in parallel.
- T020 and T021 can be written in parallel.
- T026 and T027 can be written in parallel.
- T036, T037, T038, and T039 can be written in parallel.
- T054 and T055 can be written in parallel.
- T062 and T063 can be written in parallel.
- US4 and US5 can proceed in parallel after Phase 2 if US3 soft-delete query contracts are stable.

---

## Parallel Execution Examples

### User Story 1

```text
Task: "T020 Add service tests in HR.Tests/VacationRequests/VacationRequestServiceBusinessRuleTests.cs"
Task: "T021 Add repository tests in HR.Tests/Repositories/VacationRequestRepositoryTests.cs"
```

### User Story 2

```text
Task: "T026 Add review service tests in HR.Tests/VacationRequests/VacationRequestReviewBusinessRuleTests.cs"
Task: "T027 Add reviewer-claim controller tests in HR.Tests/VacationRequests/VacationRequestsControllerTests.cs"
```

### User Story 3

```text
Task: "T036 Add employee lifecycle service tests in HR.Tests/Employees/EmployeeServiceBusinessRuleTests.cs"
Task: "T037 Add auth revocation tests in HR.Tests/Auth/AuthRevocationTests.cs"
Task: "T038 Add employee repository tests in HR.Tests/Repositories/EmployeeRepositoryTests.cs"
Task: "T039 Add employee number immutability tests in HR.Tests/Employees/EmployeeServiceBusinessRuleTests.cs"
```

### User Story 4

```text
Task: "T054 Add trip service tests in HR.Tests/Transportation/TripServiceBusinessRuleTests.cs"
Task: "T055 Add trip repository tests in HR.Tests/Repositories/TripRepositoryTests.cs"
```

### User Story 5

```text
Task: "T062 Add department service tests in HR.Tests/Departments/DepartmentServiceBusinessRuleTests.cs"
Task: "T063 Add department repository tests in HR.Tests/Repositories/DepartmentRepositoryTests.cs"
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete US1 only.
3. Run T025 and confirm vacation submission rules pass.
4. Stop for review or hand off to the implementation model.

### Incremental Delivery

1. Complete foundation once.
2. Deliver US1 and validate independently.
3. Deliver US2 and re-run US1 plus US2 tests.
4. Deliver US3 and re-run vacation plus auth/employee tests.
5. Deliver US4 and US5.
6. Run final full validation tasks T069 through T075.

### Scope Guardrails

- Do not add Phase 6 `HR.Application.DependencyInjection`.
- Do not add Phase 7 RBAC, attendance, salary, documents, dashboards, or audit-log entities.
- Do not change existing route names, cookie scheme, claim names, or structured error response shape.
- Do not normalize existing error codes unless explicitly required by the spec.
- Do not reintroduce direct `ApplicationDbContext` dependencies into service classes.
- Do not edit existing migration files; add one new Phase 5 migration only.
- Do not invent fake requester data for existing trip rows; keep existing null requester rows safe unless a reliable historical requester source exists.
