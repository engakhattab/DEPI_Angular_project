---
description: "Actionable task list for Phase 3 - Service Layer Extraction"
---

# Tasks: Phase 3 - Service Layer Extraction

**Input**: Design documents from `specs/003-service-layer-extraction/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/api-contract.md`, `quickstart.md`

**Tests**: No automated test project is added by this task list because the feature specification does not request TDD. Each feature has a required build and manual API checkpoint. Add focused xUnit tasks in a separate approved scope if automated coverage is requested.

**Organization**: Tasks are grouped by user story and executed sequentially in the required order: departments, vacation requests, trips, then employees.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because the tasks touch different files and have no incomplete dependencies.
- **[Story]**: Identifies the user story served by the task.
- Every task includes an exact file or directory path.

## Phase 1: Setup and Baseline

**Purpose**: Confirm the completed phases compile before Phase 3 source changes begin.

- [X] T001 Run `dotnet build .\HR.slnx` from the repository root and confirm the Phase 2 baseline compiles before editing `HR.API/`, `HR.Application/`, `HR.Infrastructure/`, or `HR.Shared/`

**Checkpoint**: The existing Phase 2 source compiles before service extraction starts.

---

## Phase 2: Foundational Shared Infrastructure

**Purpose**: Add shared behavior needed by every extracted controller.

**Critical**: Complete this phase before starting any user story.

- [X] T002 [P] Normalize pagination inputs inside `HR.Shared/Pagination/PagedList.cs`: convert `page <= 0` to `1`, convert `pageSize <= 0` to `25`, cap `pageSize > 100` at `100`, and ensure returned metadata uses the normalized values
- [X] T003 [P] Create `HR.API/Extensions/ServiceErrorMappingExtensions.cs` with one API-layer mapper from `HR.Shared.Results.ServiceError.Type` to structured `{ code, message }` responses and HTTP statuses `400`, `401`, `403`, `404`, `409`, `422`, and `500`
- [X] T004 Run `dotnet build .\HR.slnx` and resolve any compilation errors introduced by `HR.Shared/Pagination/PagedList.cs` or `HR.API/Extensions/ServiceErrorMappingExtensions.cs`

**Checkpoint**: Shared pagination normalization and expected-error mapping compile and are ready for all controllers.

---

## Phase 3: User Story 1 - Manage Departments Reliably (Priority: P1) MVP

**Goal**: Move department behavior out of the controller while preserving CRUD behavior, alphabetical paging, and structured failures.

**Independent Test**: Use the department section of `specs/003-service-layer-extraction/quickstart.md` to browse paginated departments, exercise CRUD operations, and confirm duplicate-name and delete-with-employees conflicts.

### Implementation for User Story 1

- [X] T005 [US1] Create `HR.Application/Departments/IDepartmentService.cs` with paginated list, get-by-id, create, update, and delete methods; return DTOs for reads, `Result<T>` or `Result` for writes, and accept `CancellationToken` on every async method
- [X] T006 [US1] Create `HR.Infrastructure/Departments/DepartmentService.cs` using `ApplicationDbContext` to preserve alphabetical ordering, DTO mapping, unique-name conflict checks, not-found handling, delete-with-employees refusal, pagination, and cancellation forwarding
- [X] T007 [US1] Register `IDepartmentService` to `DepartmentService` in `HR.Infrastructure/DependencyInjection.cs`
- [X] T008 [US1] Rewrite `HR.API/Controllers/DepartmentsController.cs` to inject only `IDepartmentService`, add optional `page = 1` and `pageSize = 25` query parameters to the list action, forward `CancellationToken`, and map service failures through `HR.API/Extensions/ServiceErrorMappingExtensions.cs`
- [X] T009 [US1] Run `dotnet build .\HR.slnx` and resolve department-extraction compilation errors in `HR.Application/Departments/`, `HR.Infrastructure/Departments/`, `HR.Infrastructure/DependencyInjection.cs`, or `HR.API/Controllers/DepartmentsController.cs`
- [ ] T010 [US1] Execute the department validation checkpoint in `specs/003-service-layer-extraction/quickstart.md`, including pagination boundaries, CRUD operations, duplicate-name conflict, delete-with-employees conflict, and unauthenticated JSON `401`

**Checkpoint**: Departments are independently functional and `DepartmentsController` no longer references `ApplicationDbContext`.

---

## Phase 4: User Story 2 - Process Vacation Requests Reliably (Priority: P2)

**Goal**: Move vacation request behavior out of the controller while preserving filters, newest-first paging, existing validation, and CRUD behavior.

**Independent Test**: Use the vacation request section of `specs/003-service-layer-extraction/quickstart.md` to browse filtered pages, submit valid and invalid requests, change status, and delete a request.

### Implementation for User Story 2

- [X] T011 [US2] Create `HR.Application/VacationRequests/IVacationRequestService.cs` with paginated filtered list, get-by-id, create, status-update, and delete methods; return DTOs for reads, `Result<T>` or `Result` for writes, and accept `CancellationToken` on every async method
- [X] T012 [US2] Create `HR.Infrastructure/VacationRequests/VacationRequestService.cs` using `ApplicationDbContext` to preserve optional status and employee filters, newest-first ordering, DTO mapping, start-date/end-date validation, employee existence checks, pending status on creation, unrestricted existing status updates, pagination, and cancellation forwarding
- [X] T013 [US2] Register `IVacationRequestService` to `VacationRequestService` in `HR.Infrastructure/DependencyInjection.cs`
- [X] T014 [US2] Rewrite `HR.API/Controllers/VacationRequestsController.cs` to inject only `IVacationRequestService`, retain `status` and `employeeId` filters, add optional `page = 1` and `pageSize = 25` query parameters, forward `CancellationToken`, and map failures through `HR.API/Extensions/ServiceErrorMappingExtensions.cs`
- [X] T015 [US2] Run `dotnet build .\HR.slnx` and resolve vacation-request extraction compilation errors in `HR.Application/VacationRequests/`, `HR.Infrastructure/VacationRequests/`, `HR.Infrastructure/DependencyInjection.cs`, or `HR.API/Controllers/VacationRequestsController.cs`
- [ ] T016 [US2] Execute the vacation request validation checkpoint in `specs/003-service-layer-extraction/quickstart.md`, including pagination, combined filters, valid creation, invalid date range, unknown employee, status update, deletion, and unauthenticated JSON `401`

**Checkpoint**: Vacation requests are independently functional and `VacationRequestsController` no longer references `ApplicationDbContext`.

---

## Phase 5: User Story 3 - Manage Trips Reliably (Priority: P3)

**Goal**: Move trip behavior out of the controller while preserving generated identifiers, newest-first paging, and CRUD behavior.

**Independent Test**: Use the trip section of `specs/003-service-layer-extraction/quickstart.md` to browse pages, create a trip, inspect generated identifiers, retrieve the trip, and delete it.

### Implementation for User Story 3

- [X] T017 [US3] Create `HR.Application/Transportation/ITripService.cs` with paginated list, get-by-id, create, and delete methods; return DTOs for reads, `Result<T>` or `Result` for writes, and accept `CancellationToken` on every async method
- [X] T018 [US3] Create `HR.Infrastructure/Transportation/TripService.cs` using `ApplicationDbContext` to preserve newest-first ordering, DTO mapping, `TRIP-xxxxxx` and `REQ-xxxxxx` identifier generation, not-found handling, pagination, and cancellation forwarding
- [X] T019 [US3] Register `ITripService` to `TripService` in `HR.Infrastructure/DependencyInjection.cs`
- [X] T020 [US3] Rewrite `HR.API/Controllers/TripsController.cs` to inject only `ITripService`, add optional `page = 1` and `pageSize = 25` query parameters, forward `CancellationToken`, and map failures through `HR.API/Extensions/ServiceErrorMappingExtensions.cs`
- [X] T021 [US3] Run `dotnet build .\HR.slnx` and resolve trip-extraction compilation errors in `HR.Application/Transportation/`, `HR.Infrastructure/Transportation/`, `HR.Infrastructure/DependencyInjection.cs`, or `HR.API/Controllers/TripsController.cs`
- [ ] T022 [US3] Execute the trip validation checkpoint in `specs/003-service-layer-extraction/quickstart.md`, including pagination, generated identifier shapes, retrieval, deletion, unknown-trip structured failures, and unauthenticated JSON `401`

**Checkpoint**: Trips are independently functional and `TripsController` no longer references `ApplicationDbContext`.

---

## Phase 6: User Story 4 - Manage Employees Reliably (Priority: P4)

**Goal**: Move employee and Identity coordination out of the controller while preserving paginated browsing, validation, transactional behavior, profile updates, and cleanup.

**Independent Test**: Use the employee section of `specs/003-service-layer-extraction/quickstart.md` to browse filtered pages, create employees with supplied and generated passwords, update email and profile data, exercise expected failures, and delete an employee with related records.

### Implementation for User Story 4

- [X] T023 [US4] Create `HR.Application/Employees/IEmployeeService.cs` with paginated filtered list, get-by-id, create, update, and delete methods; return DTOs for reads, `Result<T>` or `Result` for writes, and accept `CancellationToken` on every async method
- [X] T024 [US4] Create the read and mapping portion of `HR.Infrastructure/Employees/EmployeeService.cs` using `ApplicationDbContext` to preserve optional status filtering, employee-number ordering, page-scoped Identity user lookup, get-by-id behavior, `EmployeeResponse` mapping, pagination, and cancellation forwarding
- [X] T025 [US4] Add employee creation behavior to `HR.Infrastructure/Employees/EmployeeService.cs` using `UserManager<ApplicationUser>` to preserve employee-number uniqueness, department and manager existence checks, supplied or generated temporary passwords, execution strategy, transaction boundaries, Identity validation errors, DTO mapping, logging, and cancellation forwarding
- [X] T026 [US4] Add employee update and delete behavior to `HR.Infrastructure/Employees/EmployeeService.cs` to preserve self-manager rejection, relationship checks, Identity email synchronization, direct-report cleanup, vacation-request cleanup, Identity deletion, transaction boundaries, logging of unexpected failures, structured expected failures, and cancellation forwarding
- [X] T027 [US4] Register `IEmployeeService` to `EmployeeService` in `HR.Infrastructure/DependencyInjection.cs`
- [X] T028 [US4] Rewrite `HR.API/Controllers/EmployeesController.cs` to inject only `IEmployeeService`, retain the optional `status` filter, add optional `page = 1` and `pageSize = 25` query parameters, add and forward `CancellationToken` on every async action, and map failures through `HR.API/Extensions/ServiceErrorMappingExtensions.cs`
- [X] T029 [US4] Run `dotnet build .\HR.slnx` and resolve employee-extraction compilation errors in `HR.Application/Employees/`, `HR.Infrastructure/Employees/`, `HR.Infrastructure/DependencyInjection.cs`, or `HR.API/Controllers/EmployeesController.cs`
- [ ] T030 [US4] Execute the employee validation checkpoint in `specs/003-service-layer-extraction/quickstart.md`, including pagination, status filtering, supplied and generated passwords, duplicate employee number, unknown department, unknown manager, self-manager rejection, profile and email update, deletion cleanup, and unauthenticated JSON `401`

**Checkpoint**: Employees are independently functional and `EmployeesController` no longer references `ApplicationDbContext` or `UserManager<ApplicationUser>`.

---

## Phase 7: Polish and Cross-Cutting Verification

**Purpose**: Verify the complete Phase 3 boundary without pulling later-phase work forward.

- [X] T031 Run `rg -n "ApplicationDbContext|UserManager<" .\HR.API\Controllers` and remove any remaining direct data-access or Identity dependencies from `HR.API/Controllers/DepartmentsController.cs`, `HR.API/Controllers/VacationRequestsController.cs`, `HR.API/Controllers/TripsController.cs`, and `HR.API/Controllers/EmployeesController.cs`
- [X] T032 Run `rg -n "async Task|CancellationToken" .\HR.API\Controllers .\HR.Application .\HR.Infrastructure` and review Phase 3 controller and service paths to ensure cancellation tokens are accepted and forwarded wherever the called async API supports them
- [X] T033 Run `dotnet build .\HR.slnx` and resolve any remaining Phase 3 compilation errors across `HR.API/`, `HR.Application/`, `HR.Infrastructure/`, and `HR.Shared/`
- [ ] T034 Execute the authentication and final structural regression sections in `specs/003-service-layer-extraction/quickstart.md`, including login, logout, `/api/auth/me`, JSON `401`, JSON `403`, pagination envelopes, and existing global exception behavior
- [X] T035 Review the final diff under `HR.API/`, `HR.Application/`, `HR.Infrastructure/`, `HR.Shared/`, and `HR.Domain/` and confirm Phase 3 did not add repositories, migrations, entity changes, `ApplicationDbContext.OnModelCreating` changes, frontend changes, or new Phase 5 business rules

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup and Baseline (Phase 1)**: No dependencies.
- **Foundational Shared Infrastructure (Phase 2)**: Depends on baseline build completion and blocks every user story.
- **Departments (Phase 3 / US1)**: Depends on Phase 2.
- **Vacation Requests (Phase 4 / US2)**: Depends on the US1 checkpoint.
- **Trips (Phase 5 / US3)**: Depends on the US2 checkpoint.
- **Employees (Phase 6 / US4)**: Depends on the US3 checkpoint.
- **Polish and Verification (Phase 7)**: Depends on all four story checkpoints.

### User Story Dependencies

- **US1 - Departments**: First extraction and MVP pattern.
- **US2 - Vacation Requests**: Starts only after department extraction is verified.
- **US3 - Trips**: Starts only after vacation request extraction is verified.
- **US4 - Employees**: Starts only after trip extraction is verified; it is intentionally last because it coordinates EF Core and Identity transactions.

### Within Each User Story

1. Define the application-layer service interface.
2. Implement the infrastructure-layer service.
3. Register the service in `HR.Infrastructure/DependencyInjection.cs`.
4. Rewrite the API controller.
5. Run `dotnet build`.
6. Execute the manual API checkpoint before moving to the next story.

### Parallel Opportunities

- T002 and T003 can run in parallel because they touch separate foundational files.
- Research, review, or manual test preparation for a later story may happen in parallel, but source extraction must remain sequential to satisfy FR-015.
- No story-specific DI registration tasks should run concurrently because they modify `HR.Infrastructure/DependencyInjection.cs`.

---

## Parallel Example: Foundational Shared Infrastructure

```text
Task T002: Normalize pagination in HR.Shared/Pagination/PagedList.cs
Task T003: Create expected-error mapping in HR.API/Extensions/ServiceErrorMappingExtensions.cs
```

---

## Implementation Strategy

### MVP First: Departments

1. Complete T001-T004.
2. Complete T005-T010.
3. Stop and validate department behavior independently.
4. Use the verified department extraction as the pattern for subsequent services.

### Incremental Delivery

1. Complete shared foundations.
2. Extract and verify departments.
3. Extract and verify vacation requests.
4. Extract and verify trips.
5. Extract and verify employees.
6. Complete cross-cutting structural and authentication regression checks.

### Phase Boundary

Do not add repositories, EF configuration classes, migrations, entity changes, frontend changes, or new HR business rules while completing these tasks. Those changes belong to later phases.

## Notes

- Mark a task complete only after its implementation and required verification succeed.
- Keep controllers as thin HTTP adapters.
- Keep service interfaces in `HR.Application` and EF/Identity-backed implementations in `HR.Infrastructure`.
- Preserve the existing secure cookie-authentication behavior.
- Stop at each checkpoint if behavior differs from the completed phases.
