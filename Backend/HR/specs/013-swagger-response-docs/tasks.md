# Tasks: Phase 13 - Swagger/OpenAPI Response Documentation Pass

**Input**: Design documents from `specs/013-swagger-response-docs/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/openapi-response-documentation-contract.md](./contracts/openapi-response-documentation-contract.md), [quickstart.md](./quickstart.md)

**Tests**: No new automated tests are required by the spec. Validation tasks run the existing build/test suite and manually verify Swagger/OpenAPI output.

**Organization**: Tasks are grouped by user story so Phase 13 can be implemented incrementally. User Story 1 is the MVP and removes undocumented success responses. User Story 2 documents auth/error outcomes. User Story 3 verifies behavior-neutral delivery.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel when the prerequisite phase is complete because it edits a different file or only reads/records evidence.
- **[Story]**: Maps to User Story 1, User Story 2, or User Story 3 from [spec.md](./spec.md).
- Every task includes at least one exact file path.

## Guardrails For Implementation Agents

- Do not change controller routes, HTTP methods, request DTOs, response DTO runtime shapes, service calls, validation logic, authorization policies, cookie authentication, database code, migrations, or seed data.
- Prefer explicit `[ProducesResponseType]`, `[Produces]`, and related API metadata on existing controller actions.
- Use the current controller code, `HR.API/Extensions/ServiceErrorMappingExtensions.cs`, and `HR.API/Middleware/GlobalExceptionMiddleware.cs` as the source of truth.
- If one endpoint has multiple possible payload shapes for the same status code, do not invent runtime behavior. Pick the closest currently documented schema, record the limitation in `specs/013-swagger-response-docs/implementation-summary.md`, and continue.
- Do not add a bearer/JWT Swagger security scheme in `HR.API/Program.cs`.
- If a runtime behavior mismatch is discovered, record it as follow-up work in `specs/013-swagger-response-docs/implementation-summary.md` instead of fixing behavior.

## Phase 1: Setup (Shared Documentation Infrastructure)

**Purpose**: Create implementation evidence files and the minimal documentation-only schema needed by later controller metadata tasks.

- [X] T001 Create `specs/013-swagger-response-docs/response-documentation-matrix.md` with a table for controller, action, route, success statuses, error statuses, payload schema, and verification result.
- [X] T002 Create `specs/013-swagger-response-docs/implementation-summary.md` with sections for scope guardrails, files changed, validation commands, Swagger/OpenAPI verification, route preservation, and follow-up findings.
- [X] T003 [P] Create documentation-only structured error schema `HR.API/Documentation/ErrorResponseDocumentation.cs` with nullable-disabled string properties `Code` and `Message`; do not use this class in runtime responses.
- [X] T004 Record the current service-error status mapping from `HR.API/Extensions/ServiceErrorMappingExtensions.cs` into `specs/013-swagger-response-docs/response-documentation-matrix.md` after T001 creates the matrix.
- [X] T005 Record the current global exception status mapping from `HR.API/Middleware/GlobalExceptionMiddleware.cs` into `specs/013-swagger-response-docs/response-documentation-matrix.md` after T004 to avoid concurrent writes to the matrix.
- [X] T006 Record current Swagger and cookie-auth setup from `HR.API/Program.cs` in `specs/013-swagger-response-docs/implementation-summary.md` after T002 creates the summary, including that no bearer/JWT Swagger scheme should be added.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the response documentation matrix before editing controller metadata.

**Critical**: Do not start user story tasks until the response matrix covers every controller action from the contract.

- [X] T007 Fill success-response rows in `specs/013-swagger-response-docs/response-documentation-matrix.md` from `specs/013-swagger-response-docs/contracts/openapi-response-documentation-contract.md`.
- [X] T008 Fill error-response rows in `specs/013-swagger-response-docs/response-documentation-matrix.md` from `specs/013-swagger-response-docs/contracts/openapi-response-documentation-contract.md`.
- [X] T009 Mark each matrix row in `specs/013-swagger-response-docs/response-documentation-matrix.md` with the source file and source action from `HR.API/Controllers/AuthController.cs`, `HR.API/Controllers/EmployeesController.cs`, `HR.API/Controllers/DepartmentsController.cs`, `HR.API/Controllers/AttendanceController.cs`, `HR.API/Controllers/VacationRequestsController.cs`, `HR.API/Controllers/TripsController.cs`, `HR.API/Controllers/CompensationController.cs`, `HR.API/Controllers/EmployeeDocumentsController.cs`, `HR.API/Controllers/DashboardController.cs`, and `HR.API/Controllers/AuditLogsController.cs`.
- [X] T010 Add a schema decision note to `specs/013-swagger-response-docs/response-documentation-matrix.md` stating that model-validation `400` responses should be documented with `ValidationProblemDetails`, while service error responses should be documented with `ErrorResponseDocumentation` where a single status schema is possible.
- [X] T011 Run `dotnet build .\HR.slnx -c Release` from `HR.slnx` and record the pre-implementation result in `specs/013-swagger-response-docs/implementation-summary.md`.

**Checkpoint**: Matrix complete and baseline build recorded.

---

## Phase 3: User Story 1 - Document Common API Responses (Priority: P1) MVP

**Goal**: Expected success responses no longer appear as `Undocumented` in Swagger/OpenAPI.

**Independent Test**: Open Swagger/OpenAPI and verify reviewed endpoints document `200 OK`, `201 Created`, and `204 No Content` with the current payload shape or no-body response.

### Implementation for User Story 1

- [X] T012 [P] [US1] Add success response metadata for `POST /api/auth/login` `200 LoginResponse`, `POST /api/auth/logout` `204 No Content`, and `GET /api/auth/me` `200 CurrentUserResponse` in `HR.API/Controllers/AuthController.cs`.
- [X] T013 [P] [US1] Add success response metadata for list/detail/create/update/delete/role employee actions in `HR.API/Controllers/EmployeesController.cs`, including `PagedList<EmployeeResponse>`, `EmployeeResponse`, `EmployeeCreatedResponse`, `EmployeeRoleResponse`, and `204 No Content`.
- [X] T014 [P] [US1] Add success response metadata for list/detail/create/update/delete department actions in `HR.API/Controllers/DepartmentsController.cs`, including `PagedList<DepartmentResponse>`, `DepartmentResponse`, `201 Created`, and `204 No Content`.
- [X] T015 [P] [US1] Add success response metadata for `clock-in` `201 AttendanceRecordResponse`, `clock-out` `200 AttendanceRecordResponse`, and attendance list `200 PagedList<AttendanceRecordResponse>` in `HR.API/Controllers/AttendanceController.cs`.
- [X] T016 [P] [US1] Add success response metadata for list/detail/create/status-update/delete vacation request actions in `HR.API/Controllers/VacationRequestsController.cs`, including `VacationRequestResponse`, `PagedList<VacationRequestResponse>`, `201 Created`, and `204 No Content`.
- [X] T017 [P] [US1] Add success response metadata for list/detail/create/delete trip actions in `HR.API/Controllers/TripsController.cs`, including `PagedList<TripResponse>`, `TripResponse`, `201 Created`, and `204 No Content`.
- [X] T018 [P] [US1] Add success response metadata for compensation get/update actions in `HR.API/Controllers/CompensationController.cs`, including `200 CompensationResponse`.
- [X] T019 [P] [US1] Add success response metadata for document upload/list/download/delete actions in `HR.API/Controllers/EmployeeDocumentsController.cs`, including `201 EmployeeDocumentResponse`, `PagedList<EmployeeDocumentResponse>`, file download `200`, and `204 No Content`.
- [X] T020 [P] [US1] Add success response metadata for dashboard summary in `HR.API/Controllers/DashboardController.cs`, including `200 DashboardSummaryResponse`.
- [X] T021 [P] [US1] Add success response metadata for audit log search in `HR.API/Controllers/AuditLogsController.cs`, including `200 PagedList<AuditLogEntryResponse>`.
- [X] T022 [US1] Run `dotnet build .\HR.slnx -c Release` from `HR.slnx` and record the User Story 1 build result in `specs/013-swagger-response-docs/implementation-summary.md`.
- [X] T023 [US1] Start `HR.API/HR.API.csproj`, inspect `/swagger/v1/swagger.json`, and record success status verification for all User Story 1 rows in `specs/013-swagger-response-docs/response-documentation-matrix.md`.

**Checkpoint**: User Story 1 independently complete when success statuses from the matrix are documented and no route disappears.

---

## Phase 4: User Story 2 - Document Authorization and Validation Outcomes (Priority: P1)

**Goal**: Protected, role-scoped, validation, conflict, not-found, business-rule, and payload-too-large outcomes are visible in Swagger/OpenAPI where current behavior supports them.

**Independent Test**: Review Swagger/OpenAPI and confirm expected `400`, `401`, `403`, `404`, `409`, `413`, and `422` entries appear for the current endpoint behaviors without adding bearer/JWT assumptions.

### Implementation for User Story 2

- [X] T024 [P] [US2] Add error response metadata for auth validation and unauthorized outcomes in `HR.API/Controllers/AuthController.cs`, using `ValidationProblemDetails` for model validation and `ErrorResponseDocumentation` for structured unauthorized responses.
- [X] T025 [P] [US2] Add error response metadata for employee list/detail/create/update/delete/role actions in `HR.API/Controllers/EmployeesController.cs`, covering applicable `400`, `401`, `403`, `404`, `409`, and `422` statuses from the matrix.
- [X] T026 [P] [US2] Add error response metadata for department list/detail/create/update/delete actions in `HR.API/Controllers/DepartmentsController.cs`, covering applicable `400`, `401`, `403`, `404`, `409`, and `422` statuses from the matrix.
- [X] T027 [P] [US2] Add error response metadata for attendance clock-in/clock-out/list actions in `HR.API/Controllers/AttendanceController.cs`, covering applicable `400`, `401`, `403`, `404`, `409`, and `422` statuses from the matrix.
- [X] T028 [P] [US2] Add error response metadata for vacation request list/detail/create/status-update/delete actions in `HR.API/Controllers/VacationRequestsController.cs`, covering applicable `400`, `401`, `403`, `404`, `409`, and `422` statuses from the matrix.
- [X] T029 [P] [US2] Add error response metadata for trip list/detail/create/delete actions in `HR.API/Controllers/TripsController.cs`, covering applicable `400`, `401`, `403`, `404`, `409`, and `422` statuses from the matrix.
- [X] T030 [P] [US2] Add error response metadata for compensation get/update actions in `HR.API/Controllers/CompensationController.cs`, covering applicable `400`, `401`, `403`, `404`, `409`, and `422` statuses from the matrix.
- [X] T031 [P] [US2] Add error response metadata for document upload/list/download/delete actions in `HR.API/Controllers/EmployeeDocumentsController.cs`, covering applicable `400`, `401`, `403`, `404`, `413`, and `422` statuses from the matrix.
- [X] T032 [P] [US2] Add error response metadata for dashboard summary in `HR.API/Controllers/DashboardController.cs`, covering applicable `401`, `403`, and service error statuses from the matrix.
- [X] T033 [P] [US2] Add error response metadata for audit log search in `HR.API/Controllers/AuditLogsController.cs`, covering applicable `401`, `403`, and service error statuses from the matrix.
- [X] T034 [US2] Review `HR.API/Controllers/EmployeeDocumentsController.cs` to ensure upload documents `413 Payload Too Large` and download documents file output without changing upload size checks or download behavior.
- [X] T035 [US2] Review `HR.API/Program.cs` and `/swagger/v1/swagger.json` to confirm no bearer/JWT security scheme was added; record the result in `specs/013-swagger-response-docs/implementation-summary.md`.
- [X] T036 [US2] Run `dotnet build .\HR.slnx -c Release` from `HR.slnx` and record the User Story 2 build result in `specs/013-swagger-response-docs/implementation-summary.md`.
- [X] T037 [US2] Start `HR.API/HR.API.csproj`, inspect `/swagger/v1/swagger.json`, and record error status verification for all User Story 2 rows in `specs/013-swagger-response-docs/response-documentation-matrix.md`.

**Checkpoint**: User Story 2 independently complete when expected auth/error statuses are documented and cookie-auth semantics remain unchanged.

---

## Phase 5: User Story 3 - Preserve Existing API Behavior While Improving Documentation (Priority: P2)

**Goal**: Demonstrate Phase 13 is a behavior-neutral Swagger/OpenAPI documentation pass.

**Independent Test**: Build, run the existing tests, inspect Swagger/OpenAPI route preservation, and review diffs for metadata-only changes.

### Implementation for User Story 3

- [X] T038 [US3] Run `dotnet test .\HR.slnx -c Release` from `HR.slnx` and record the result in `specs/013-swagger-response-docs/implementation-summary.md`.
- [X] T039 [US3] Review `git diff -- HR.API/Controllers HR.API/Documentation` and record in `specs/013-swagger-response-docs/implementation-summary.md` that changes are response-documentation metadata and documentation-only schemas.
- [X] T040 [US3] Review `git diff -- HR.API/Program.cs HR.Application HR.Domain HR.Infrastructure HR.Shared` and record in `specs/013-swagger-response-docs/implementation-summary.md` that no runtime auth, service, domain, data-access, shared DTO, migration, or database behavior changed.
- [X] T041 [US3] Inspect `/swagger/v1/swagger.json` from `HR.API/HR.API.csproj` and record in `specs/013-swagger-response-docs/implementation-summary.md` that every route listed in `specs/013-swagger-response-docs/contracts/openapi-response-documentation-contract.md` remains present; compare generated paths case-insensitively if controller-token casing differs and do not change route attributes.
- [X] T042 [US3] Inspect Swagger UI from `HR.API/HR.API.csproj` and record in `specs/013-swagger-response-docs/implementation-summary.md` that `POST /api/attendance/clock-in` no longer shows `201 Created` as `Undocumented`.
- [X] T043 [US3] Record any runtime mismatch or Swagger limitation found during implementation as follow-up work in `specs/013-swagger-response-docs/implementation-summary.md` without changing behavior.

**Checkpoint**: User Story 3 complete when validation evidence proves behavior-neutral documentation work.

---

## Phase 6: Polish and Cross-Cutting Validation

**Purpose**: Final verification and documentation cleanup.

- [X] T044 Run `git diff --check` from the repository root and record the result in `specs/013-swagger-response-docs/implementation-summary.md`.
- [X] T045 [P] Review `specs/013-swagger-response-docs/response-documentation-matrix.md` and ensure every matrix row is marked `Verified` or `Follow-up`.
- [X] T046 Review `specs/013-swagger-response-docs/implementation-summary.md` after T044 and T045 and ensure it includes command results, endpoint groups reviewed, route preservation, no bearer/JWT confirmation, and behavior-neutral confirmation.
- [X] T047 Run the quickstart validation from `specs/013-swagger-response-docs/quickstart.md` and record the final pass/fail summary in `specs/013-swagger-response-docs/implementation-summary.md`.
- [X] T048 Review `git diff -- HR.API HR.Application HR.Domain HR.Infrastructure HR.Shared HR.Tests specs/013-swagger-response-docs AGENTS.md`; remove any accidental out-of-scope runtime/API change, or stop and record that separate approval is required in `specs/013-swagger-response-docs/implementation-summary.md`.

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1 because the matrix and documentation-only error schema must exist first.
- **Phase 3 User Story 1**: Depends on Phase 2 and is the MVP.
- **Phase 4 User Story 2**: Depends on Phase 2. It can start after Phase 2, but it is simpler and safer after User Story 1 because it edits the same controller files.
- **Phase 5 User Story 3**: Depends on User Stories 1 and 2.
- **Phase 6 Polish**: Depends on selected user stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Phase 2. Delivers documented success responses.
- **User Story 2 (P1)**: Can start after Phase 2. Delivers documented auth/error responses. It should merge carefully with User Story 1 because both touch controller files.
- **User Story 3 (P2)**: Depends on User Stories 1 and 2 because it validates the complete behavior-neutral pass.

### Parallel Opportunities

- T003 can run in parallel during setup after the implementer confirms `HR.API/Documentation/` exists or creates it.
- T012 through T021 can run in parallel by controller file after Phase 2.
- T024 through T033 can run in parallel by controller file after Phase 2, but avoid editing the same controller at the same time as User Story 1.
- T045 can run in parallel with T044 during final validation; T046 must run after T044 and T045 because it reviews the final evidence.

## Parallel Example: User Story 1

```text
Task: "T012 Add success response metadata in HR.API/Controllers/AuthController.cs"
Task: "T013 Add success response metadata in HR.API/Controllers/EmployeesController.cs"
Task: "T014 Add success response metadata in HR.API/Controllers/DepartmentsController.cs"
Task: "T015 Add success response metadata in HR.API/Controllers/AttendanceController.cs"
Task: "T016 Add success response metadata in HR.API/Controllers/VacationRequestsController.cs"
Task: "T017 Add success response metadata in HR.API/Controllers/TripsController.cs"
Task: "T018 Add success response metadata in HR.API/Controllers/CompensationController.cs"
Task: "T019 Add success response metadata in HR.API/Controllers/EmployeeDocumentsController.cs"
Task: "T020 Add success response metadata in HR.API/Controllers/DashboardController.cs"
Task: "T021 Add success response metadata in HR.API/Controllers/AuditLogsController.cs"
```

## Parallel Example: User Story 2

```text
Task: "T024 Add error response metadata in HR.API/Controllers/AuthController.cs"
Task: "T025 Add error response metadata in HR.API/Controllers/EmployeesController.cs"
Task: "T026 Add error response metadata in HR.API/Controllers/DepartmentsController.cs"
Task: "T027 Add error response metadata in HR.API/Controllers/AttendanceController.cs"
Task: "T028 Add error response metadata in HR.API/Controllers/VacationRequestsController.cs"
Task: "T029 Add error response metadata in HR.API/Controllers/TripsController.cs"
Task: "T030 Add error response metadata in HR.API/Controllers/CompensationController.cs"
Task: "T031 Add error response metadata in HR.API/Controllers/EmployeeDocumentsController.cs"
Task: "T032 Add error response metadata in HR.API/Controllers/DashboardController.cs"
Task: "T033 Add error response metadata in HR.API/Controllers/AuditLogsController.cs"
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete User Story 1 only.
3. Build and inspect Swagger/OpenAPI for success responses.
4. Stop and validate that common success responses, especially `POST /api/attendance/clock-in` `201 Created`, no longer show as `Undocumented`.

### Incremental Delivery

1. Add success response metadata first.
2. Add auth/error response metadata next.
3. Validate behavior neutrality last.
4. Record any mismatch as follow-up work instead of changing runtime behavior.

### Lower-Cost Model Execution Notes

- Work one controller file at a time.
- After each controller, compare the action methods against `specs/013-swagger-response-docs/contracts/openapi-response-documentation-contract.md`.
- If unsure whether a status is currently returned, inspect the controller action and service result mapping before adding metadata.
- Do not modify service calls, `if` branches, return statements, route attributes, authorization attributes, DTO definitions, migrations, or `Program.cs` authentication setup.
- Keep evidence files updated as tasks complete so a reviewer can audit what was verified.
