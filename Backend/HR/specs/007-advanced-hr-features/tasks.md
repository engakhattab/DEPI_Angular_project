# Tasks: Phase 7 - Advanced HR Features

**Input**: Design documents from `specs/007-advanced-hr-features/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Included because Phase 7 explicitly requires focused authorization/access-matrix tests, service-level business-rule tests, controller compatibility tests, file-storage failure/cleanup tests, EF migration checks, and local SQL Server smoke validation.

**Organization**: Tasks are grouped by user story so each feature slice can be implemented and verified incrementally after the shared foundation is complete.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other independent tasks touching different files.
- **[Story]**: User story association, for example `[US1]`.
- Setup, foundational, and polish tasks do not use a story label.
- Every task includes an exact target file or artifact path.

## Phase 1: Setup

**Purpose**: Confirm Phase 7 readiness and create empty feature/test locations without changing behavior.

- [X] T001 Run baseline `dotnet restore .\HR.slnx`, `dotnet build .\HR.slnx -c Release`, and `dotnet test .\HR.slnx -c Release --no-build`; record baseline failures in `specs/007-advanced-hr-features/implementation-summary.md`, not in `specs/007-advanced-hr-features/tasks.md`.
- [X] T002 Review `specs/007-advanced-hr-features/checklists/requirements.md`, `specs/007-advanced-hr-features/spec.md`, `specs/007-advanced-hr-features/plan.md`, and `.specify/memory/constitution.md`; update docs before implementation if any Phase 7 checklist item is incomplete.
- [X] T003 Verify Phase 6 registration cleanup is present by confirming `HR.API/Program.cs` calls `AddApplication()` and `AddInfrastructure(builder.Configuration)` and has no direct application/infrastructure `AddScoped<>` registrations.
- [X] T004 Verify no running `HR.API` process is locking build outputs before implementation, using `HR.API/bin/Debug/net8.0/HR.API.exe` as the lock target.
- [X] T005 [P] Create Phase 7 test folders `HR.Tests/Attendance/`, `HR.Tests/Authorization/`, `HR.Tests/Compensation/`, `HR.Tests/Documents/`, `HR.Tests/Dashboard/`, `HR.Tests/Audit/`, `HR.Tests/Configuration/`, and `HR.Tests/Migration/`.
- [X] T006 [P] Create Phase 7 application feature folders `HR.Application/Attendance/`, `HR.Application/Authorization/`, `HR.Application/Compensation/`, `HR.Application/Documents/`, `HR.Application/Dashboard/`, `HR.Application/Audit/`, and matching `HR.Application/DTOs/*/` folders.
- [X] T007 [P] Create Phase 7 infrastructure feature folders `HR.Infrastructure/Attendance/`, `HR.Infrastructure/Authorization/`, `HR.Infrastructure/Compensation/`, `HR.Infrastructure/Documents/`, `HR.Infrastructure/Dashboard/`, `HR.Infrastructure/Audit/`, `HR.Infrastructure/FileStorage/`, and `HR.Infrastructure/Configuration/`.

---

## Phase 2: Foundational

**Purpose**: Add shared entities, configuration, service contracts, repositories, authorization support, and registration surfaces that block all Phase 7 user stories.

**CRITICAL**: No user story implementation should begin until this phase is complete.

### Tests

- [X] T008 [P] Add failing configuration validation tests for missing/invalid `BusinessSettings:TimeZoneId`, missing document storage options, and invalid document storage root in `HR.Tests/Configuration/Phase7ConfigurationTests.cs`.
- [X] T009 [P] Add failing migration/model shape tests covering Employee role, Phase 7 DbSets, required indexes, and no edits to existing migrations in `HR.Tests/Migration/Phase7ModelTests.cs`.
- [X] T010 [P] Add failing static architecture tests proving `HR.Application` does not reference `HR.Infrastructure`, normal employee DTOs do not expose compensation fields, and `Program.cs` remains free of direct service registrations in `HR.Tests/DependencyInjection/Phase7BoundaryTests.cs`.

### Domain Model and Configuration

- [X] T011 [P] Add `EmployeeRole` enum with `Employee`, `Manager`, `HRAdministrator`, and `SystemAdministrator` in `HR.Domain/Enums/EmployeeRole.cs`.
- [X] T012 [P] Add `EmployeeDocumentCategory` enum with `Identity`, `Contract`, `Certificate`, and `Other` in `HR.Domain/Enums/EmployeeDocumentCategory.cs`.
- [X] T013 [P] Add `AuditActionType` enum with Phase 7 action values in `HR.Domain/Enums/AuditActionType.cs`.
- [X] T014 Add required `Role` property defaulting to `EmployeeRole.Employee` in `HR.Domain/Entities/Employee.cs`.
- [X] T015 [P] Add `AttendanceRecord` entity in `HR.Domain/Entities/AttendanceRecord.cs`.
- [X] T016 [P] Add `EmployeeCompensation` entity in `HR.Domain/Entities/EmployeeCompensation.cs`.
- [X] T017 [P] Add `SalaryHistoryEntry` entity in `HR.Domain/Entities/SalaryHistoryEntry.cs`.
- [X] T018 [P] Add `EmployeeDocument` entity in `HR.Domain/Entities/EmployeeDocument.cs`.
- [X] T019 [P] Add `AuditLogEntry` entity in `HR.Domain/Entities/AuditLogEntry.cs`.
- [X] T020 [P] Add `BusinessSettings` options class in `HR.Infrastructure/Configuration/BusinessSettings.cs`.
- [X] T021 [P] Add `DocumentStorageOptions` options class in `HR.Infrastructure/Configuration/DocumentStorageOptions.cs`.
- [X] T022 [P] Add `InitialAdminBootstrapOptions` options class in `HR.Infrastructure/Configuration/InitialAdminBootstrapOptions.cs`.

### EF Model and Repositories

- [X] T023 Add Phase 7 DbSets for attendance, compensation, salary history, documents, and audit logs in `HR.Infrastructure/Data/ApplicationDbContext.cs`.
- [X] T024 Update `HR.Infrastructure/Data/Configurations/EmployeeConfiguration.cs` to persist `Employee.Role` as a required string with default `Employee`.
- [X] T025 [P] Add attendance EF configuration with unique `(EmployeeId, AttendanceDate)` index in `HR.Infrastructure/Data/Configurations/AttendanceRecordConfiguration.cs`.
- [X] T026 [P] Add compensation EF configuration with unique `(EmployeeId)` index in `HR.Infrastructure/Data/Configurations/EmployeeCompensationConfiguration.cs`.
- [X] T027 [P] Add salary history EF configuration with employee and actor indexes in `HR.Infrastructure/Data/Configurations/SalaryHistoryEntryConfiguration.cs`.
- [X] T028 [P] Add employee document EF configuration with employee/listing indexes and stored-file-name index in `HR.Infrastructure/Data/Configurations/EmployeeDocumentConfiguration.cs`.
- [X] T029 [P] Add audit log EF configuration with entity, actor, action, and performed-at indexes in `HR.Infrastructure/Data/Configurations/AuditLogEntryConfiguration.cs`.
- [X] T030 [P] Add attendance repository interface in `HR.Infrastructure/Repositories/IAttendanceRepository.cs`.
- [X] T031 [P] Add compensation repository interface in `HR.Infrastructure/Repositories/ICompensationRepository.cs`.
- [X] T032 [P] Add employee document repository interface in `HR.Infrastructure/Repositories/IEmployeeDocumentRepository.cs`.
- [X] T033 [P] Add audit log repository interface in `HR.Infrastructure/Repositories/IAuditLogRepository.cs`.
- [X] T034 [P] Add dashboard query repository interface in `HR.Infrastructure/Repositories/IDashboardRepository.cs`.
- [X] T035 [P] Implement attendance repository in `HR.Infrastructure/Repositories/AttendanceRepository.cs`.
- [X] T036 [P] Implement compensation repository in `HR.Infrastructure/Repositories/CompensationRepository.cs`.
- [X] T037 [P] Implement employee document repository in `HR.Infrastructure/Repositories/EmployeeDocumentRepository.cs`.
- [X] T038 [P] Implement audit log repository in `HR.Infrastructure/Repositories/AuditLogRepository.cs`.
- [X] T039 [P] Implement dashboard query repository in `HR.Infrastructure/Repositories/DashboardRepository.cs`.
- [X] T040 Extend `HR.Infrastructure/Repositories/IEmployeeRepository.cs` and `HR.Infrastructure/Repositories/EmployeeRepository.cs` with role lookup/update, exact active employee lookup by email or employee number, and direct/indirect report query helpers.

### Shared Application Contracts

- [X] T041 [P] Add `IEmployeeAccessService` contract for current employee context, role checks, and team-scope checks in `HR.Application/Authorization/IEmployeeAccessService.cs`.
- [X] T042 [P] Add `IAttendanceService` contract in `HR.Application/Attendance/IAttendanceService.cs`.
- [X] T043 [P] Add `ICompensationService` contract in `HR.Application/Compensation/ICompensationService.cs`.
- [X] T044 [P] Add `IEmployeeDocumentService` contract in `HR.Application/Documents/IEmployeeDocumentService.cs`.
- [X] T045 [P] Add `IDashboardService` contract in `HR.Application/Dashboard/IDashboardService.cs`.
- [X] T046 [P] Add `IAuditLogService` contract in `HR.Application/Audit/IAuditLogService.cs`.
- [X] T047 [P] Add attendance DTOs in `HR.Application/DTOs/Attendance/AttendanceClockInRequest.cs`, `AttendanceClockOutRequest.cs`, `AttendanceRecordResponse.cs`, and `AttendanceQueryRequest.cs`.
- [X] T048 [P] Add role DTOs in `HR.Application/DTOs/Employees/EmployeeRoleUpdateRequest.cs` and `EmployeeRoleResponse.cs`.
- [X] T049 [P] Add compensation DTOs in `HR.Application/DTOs/Compensation/CompensationUpdateRequest.cs`, `CompensationResponse.cs`, and `SalaryHistoryEntryResponse.cs`.
- [X] T050 [P] Add document DTOs in `HR.Application/DTOs/Documents/EmployeeDocumentUploadRequest.cs`, `EmployeeDocumentResponse.cs`, and `EmployeeDocumentQueryRequest.cs`.
- [X] T051 [P] Add dashboard DTO in `HR.Application/DTOs/Dashboard/DashboardSummaryResponse.cs`.
- [X] T052 [P] Add audit DTOs in `HR.Application/DTOs/Audit/AuditLogQueryRequest.cs` and `AuditLogEntryResponse.cs`.

### Authorization, Timezone, Storage, Registration

- [X] T053 Add `BusinessTimeProvider` or equivalent timezone service in `HR.Infrastructure/Configuration/BusinessTimeProvider.cs` to validate configured timezone and derive business dates from UTC timestamps.
- [X] T054 Add `EmployeeAccessService` in `HR.Infrastructure/Authorization/EmployeeAccessService.cs` to resolve authenticated employee id, role, active state, and manager-chain/team-scope authorization.
- [X] T055 Add idempotent initial System Administrator bootstrap service in `HR.Infrastructure/Authorization/InitialSystemAdminBootstrapper.cs` that checks for an existing active System administrator, creates the initial `ApplicationUser` and linked `Employee` from secure `InitialAdminBootstrap` configuration when none exists, fails clearly for missing/invalid config, duplicate employee number/email, invalid password, or missing department, assigns no fallback administrator, rolls back partial records, and writes the `SYSTEM_BOOTSTRAP` audit record on success.
- [X] T056 Add employee document storage abstraction in `HR.Application/Documents/IEmployeeDocumentStorage.cs`.
- [X] T057 Implement backend local document storage in `HR.Infrastructure/FileStorage/LocalEmployeeDocumentStorage.cs` with generated safe names, allowed extension checks, max size checks, and path traversal protection.
- [X] T058 Add host-owned authorization policy helpers in `HR.API/Extensions/AuthorizationPolicyExtensions.cs` for Employee, Manager, HRAdministrator, and SystemAdministrator policies.
- [X] T059 Update `HR.API/Program.cs` to register Phase 7 authorization policies while preserving existing cookie auth, JSON 401/403 handlers, CORS, Swagger, JSON options, and middleware order.
- [X] T060 Update `HR.Infrastructure/DependencyInjection.cs` to bind/validate Phase 7 options and register Phase 7 repositories, services, bootstrapper, business-time provider, and local document storage.
- [X] T060A Add the startup/bootstrap activation invocation path for `InitialSystemAdminBootstrapper` so bootstrap is executed and validated before administrative RBAC enforcement can lock out all administrators.
- [X] T061 Update `HR.API/appsettings.json` and `HR.API/appsettings.Development.json` with development defaults for `BusinessSettings`, `InitialAdminBootstrap`, and `DocumentStorage`, without committing customer-specific initial admin passwords.
- [X] T062 Update `HR.Tests/TestInfrastructure/SqliteTestEnvironment.cs` to seed Phase 7 options, pin `BusinessSettings:TimeZoneId`, support role setup, and expose helper methods for Phase 7 entities.

**Checkpoint**: Shared model, configuration, contracts, repositories, and registration support exist; user story implementation can begin.

---

## Phase 3: User Story 1 - Record Daily Attendance (Priority: P1) MVP

**Goal**: Active employees can clock in once and clock out once for their configured business day, with UTC timestamps and deterministic timezone behavior.

**Independent Test**: Sign in as an active employee, clock in, clock out later, confirm duplicate clock-in and clock-out-without-clock-in are rejected, and confirm suspended/terminated/soft-deleted employees are denied.

### Tests for User Story 1

- [X] T063 [P] [US1] Add attendance service tests for clock-in success, duplicate clock-in conflict, clock-out success, missing open record, and clock-out before/equal clock-in in `HR.Tests/Attendance/AttendanceServiceTests.cs`.
- [X] T064 [P] [US1] Add attendance timezone tests for `Africa/Cairo` and UTC boundary cases in `HR.Tests/Attendance/AttendanceTimezoneTests.cs`.
- [X] T065 [P] [US1] Add attendance eligibility tests for suspended, terminated, soft-deleted, missing, and unauthenticated employees in `HR.Tests/Attendance/AttendanceEligibilityTests.cs`.
- [X] T066 [P] [US1] Add attendance controller contract tests for `POST /api/attendance/clock-in`, `POST /api/attendance/clock-out`, and `GET /api/attendance` in `HR.Tests/Attendance/AttendanceControllerTests.cs`.

### Implementation for User Story 1

- [X] T067 [US1] Implement attendance service logic in `HR.Infrastructure/Attendance/AttendanceService.cs`.
- [X] T068 [US1] Register `IAttendanceService` in `HR.Infrastructure/DependencyInjection.cs`.
- [X] T069 [US1] Add `AttendanceController` with clock-in, clock-out, and list actions in `HR.API/Controllers/AttendanceController.cs`.
- [X] T070 [US1] Add current employee claim extraction helper for controllers in `HR.API/Extensions/ClaimsPrincipalExtensions.cs`.
- [X] T071 [US1] Ensure attendance responses map UTC timestamps, business date, worked hours, and notes in `HR.Infrastructure/Attendance/AttendanceService.cs`.
- [X] T072 [US1] Run focused attendance tests from `HR.Tests/Attendance/` and fix only Phase 7 attendance issues.

**Checkpoint**: Attendance can be demonstrated independently after foundation.

---

## Phase 4: User Story 2 - Enforce Role-Based Access (Priority: P1)

**Goal**: Employee, Manager, HR administrator, and System administrator access is enforced consistently, with single-role assignment and safe initial admin bootstrap.

**Independent Test**: Sign in as each role and verify representative allowed and denied actions from the access matrix, including manager team boundaries and System administrator-only role assignment.

### Tests for User Story 2

- [X] T073 [P] [US2] Add access-matrix authorization tests for Employee, Manager, HRAdministrator, and SystemAdministrator in `HR.Tests/Authorization/AccessMatrixTests.cs`.
- [X] T074 [P] [US2] Add manager team-scope tests for direct reports, indirect reports, peers, unrelated employees, and soft-deleted employees in `HR.Tests/Authorization/ManagerScopeTests.cs`.
- [X] T075 [P] [US2] Add initial System Administrator bootstrap tests proving valid `CreateInitialAdmin` config creates `ApplicationUser`, linked `Employee`, `SystemAdministrator` role, and `SYSTEM_BOOTSTRAP` audit row; repeated execution creates no duplicates; existing System administrator causes no-op; missing config, invalid password, duplicate employee number, duplicate email, and missing department fail without partial records; bootstrap never falls back to the first active employee in `HR.Tests/Authorization/InitialSystemAdminBootstrapTests.cs`.
- [X] T075A [P] [US2] Add startup-level integration validation proving the system cannot lock out all administrators before bootstrap succeeds, invalid bootstrap configuration prevents RBAC activation without assigning any administrator, and normal employee creation endpoints remain RBAC-protected and unavailable as public pre-bootstrap setup paths in `HR.Tests/Authorization/BootstrapStartupValidationTests.cs`.
- [X] T076 [P] [US2] Add role assignment controller tests for `PUT /api/employees/{id}/role` in `HR.Tests/Authorization/EmployeeRoleControllerTests.cs`.
- [X] T077 [P] [US2] Add auth compatibility tests proving login/current-user existing fields remain stable and role is additive in `HR.Tests/Auth/AuthRoleCompatibilityTests.cs`.

### Implementation for User Story 2

- [X] T078 [US2] Update auth response DTOs to expose additive role data without removing existing fields in `HR.Application/DTOs/Auth/CurrentUserResponse.cs` and `HR.Application/DTOs/Auth/LoginResponse.cs`.
- [X] T079 [US2] Update employee response mapping to include additive role data without compensation fields in `HR.Application/DTOs/Employees/EmployeeResponse.cs` and `HR.Infrastructure/Employees/EmployeeService.cs`.
- [X] T080 [US2] Update `HR.Infrastructure/Auth/AuthService.cs` and `HR.API/Controllers/AuthController.cs` to include additive role claim and role response data while preserving existing claims.
- [X] T081 [US2] Implement role update service method in `HR.Infrastructure/Employees/EmployeeService.cs` and contract method in `HR.Application/Employees/IEmployeeService.cs`.
- [X] T082 [US2] Add `PUT /api/employees/{id}/role` action restricted to System administrators in `HR.API/Controllers/EmployeesController.cs`.
- [X] T083 [US2] Integrate `IEmployeeAccessService` checks into existing employee, department, vacation, and trip service methods without changing Phase 5 business rules in `HR.Infrastructure/Employees/EmployeeService.cs`, `HR.Infrastructure/Departments/DepartmentService.cs`, `HR.Infrastructure/VacationRequests/VacationRequestService.cs`, and `HR.Infrastructure/Transportation/TripService.cs`.
- [X] T084 [US2] Run focused authorization and auth compatibility tests from `HR.Tests/Authorization/` and `HR.Tests/Auth/AuthRoleCompatibilityTests.cs`.

**Checkpoint**: RBAC can be demonstrated independently and protects existing privileged workflows.

---

## Phase 5: User Story 3 - Manage Compensation Securely (Priority: P2)

**Goal**: HR administrators and System administrators can view/update compensation while normal employee responses never expose salary data.

**Independent Test**: View and update compensation as an authorized role, confirm salary history only appears after value changes, and confirm employees/managers without HR privileges cannot access compensation.

### Tests for User Story 3

- [X] T085 [P] [US3] Add compensation service tests for create, update, no-change no-history, invalid salary, invalid currency, and missing employee in `HR.Tests/Compensation/CompensationServiceTests.cs`.
- [X] T086 [P] [US3] Add compensation authorization tests for Employee, Manager, HRAdministrator, and SystemAdministrator in `HR.Tests/Compensation/CompensationAuthorizationTests.cs`.
- [X] T087 [P] [US3] Add static and response-shape tests proving normal employee DTOs and employee endpoints do not expose compensation fields in `HR.Tests/Compensation/CompensationIsolationTests.cs`.
- [X] T088 [P] [US3] Add compensation controller contract tests for `GET /api/employees/{id}/compensation` and `PUT /api/employees/{id}/compensation` in `HR.Tests/Compensation/CompensationControllerTests.cs`.

### Implementation for User Story 3

- [X] T089 [US3] Implement compensation service and salary-history mapping in `HR.Infrastructure/Compensation/CompensationService.cs`.
- [X] T090 [US3] Register `ICompensationService` in `HR.Infrastructure/DependencyInjection.cs`.
- [X] T091 [US3] Add `CompensationController` with the `GET /api/employees/{id}/compensation` and `PUT /api/employees/{id}/compensation` actions in `HR.API/Controllers/CompensationController.cs`.
- [X] T092 [US3] Add compensation audit-write hook calls for value-changing updates in `HR.Infrastructure/Compensation/CompensationService.cs`.
- [X] T093 [US3] Run focused compensation tests from `HR.Tests/Compensation/` and fix only Phase 7 compensation issues.

**Checkpoint**: Compensation is usable by HR roles and invisible to normal employee workflows.

---

## Phase 6: User Story 4 - Manage Employee Documents (Priority: P2)

**Goal**: HR administrators and System administrators can upload, list, download, and remove employee documents through authorized endpoints backed by local protected storage.

**Independent Test**: Upload a valid document, list it, download it through an authorized endpoint, remove it from normal lists, and confirm unauthorized, invalid type, oversize, path traversal, and partial-failure cases are rejected safely.

### Tests for User Story 4

- [X] T094 [P] [US4] Add local file storage tests for generated safe names, extension allowlist, max size rejection, non-public root validation, and path traversal rejection in `HR.Tests/Documents/LocalEmployeeDocumentStorageTests.cs`.
- [X] T095 [P] [US4] Add document service tests for upload success, metadata failure cleanup, file save failure no-metadata, list current documents, download authorization, removal metadata soft-delete, physical file deletion, post-removal download unavailability, and no unrelated document deletion in `HR.Tests/Documents/EmployeeDocumentServiceTests.cs`.
- [X] T096 [P] [US4] Add document authorization tests for Employee, Manager, HRAdministrator, and SystemAdministrator in `HR.Tests/Documents/EmployeeDocumentAuthorizationTests.cs`.
- [X] T097 [P] [US4] Add document controller contract tests for multipart upload, oversized upload returning `413 Payload Too Large` with `{ code, message }`, list, download, removed-document download rejection, and delete endpoints in `HR.Tests/Documents/EmployeeDocumentsControllerTests.cs`.

### Implementation for User Story 4

- [X] T098 [US4] Implement document service upload/list/download/remove logic in `HR.Infrastructure/Documents/EmployeeDocumentService.cs`, including metadata soft-delete, physical file deletion from backend-managed local storage, and no download after removal.
- [X] T099 [US4] Register `IEmployeeDocumentService` and local storage dependencies in `HR.Infrastructure/DependencyInjection.cs`.
- [X] T100 [US4] Add `EmployeeDocumentsController` with multipart upload, list, download, and delete actions in `HR.API/Controllers/EmployeeDocumentsController.cs`.
- [X] T101 [US4] Add document audit-write hook calls for upload and removal in `HR.Infrastructure/Documents/EmployeeDocumentService.cs`, ensuring removal audit records identify the actor, affected employee, affected document, action, and timestamp.
- [X] T102 [US4] Run focused document tests from `HR.Tests/Documents/` and verify temporary test files are cleaned up.

**Checkpoint**: Document management is usable by HR roles and protected from direct/public access.

---

## Phase 7: User Story 5 - View HR Dashboard Summary (Priority: P3)

**Goal**: Managers and HR roles can view a read-only dashboard summary scoped to their permissions.

**Independent Test**: Seed representative employees, departments, vacation requests, and trips, then verify dashboard counts match source data for manager-scoped and organization-wide roles.

### Tests for User Story 5

- [X] T103 [P] [US5] Add dashboard repository tests for active employee count, department count, pending vacations, approved vacations this month, employees on vacation today, new hires this month, upcoming trips this week, employees per department, and vacation status counts according to the metric-by-metric role scope table in `HR.Tests/Dashboard/DashboardRepositoryTests.cs`.
- [X] T104 [P] [US5] Add dashboard authorization and manager-scope tests proving Manager metrics are team-scoped or hidden/not applicable and HRAdministrator/SystemAdministrator metrics are organization-wide in `HR.Tests/Dashboard/DashboardAuthorizationTests.cs`.
- [X] T105 [P] [US5] Add dashboard controller contract tests for `GET /api/dashboard/summary`, including hidden/not applicable manager metrics not exposing organization-wide values in `HR.Tests/Dashboard/DashboardControllerTests.cs`.

### Implementation for User Story 5

- [X] T106 [US5] Implement dashboard service in `HR.Infrastructure/Dashboard/DashboardService.cs`.
- [X] T107 [US5] Register `IDashboardService` in `HR.Infrastructure/DependencyInjection.cs`.
- [X] T108 [US5] Add `DashboardController` with summary action in `HR.API/Controllers/DashboardController.cs`.
- [X] T109 [US5] Ensure dashboard responses exclude compensation values and soft-deleted employees from current workforce metrics in `HR.Infrastructure/Dashboard/DashboardService.cs`.
- [X] T110 [US5] Run focused dashboard tests from `HR.Tests/Dashboard/`.

**Checkpoint**: Dashboard summary is available and role-scoped without persisted summary tables.

---

## Phase 8: User Story 6 - Review Audit History (Priority: P3)

**Goal**: HR administrators and System administrators can search paginated audit history for significant successful write actions, with sensitive values redacted or summarized.

**Independent Test**: Perform representative successful writes, confirm audit entries identify actor/action/entity/time/changed fields, confirm filters work, and confirm unauthorized users cannot view audit logs.

### Tests for User Story 6

- [X] T111 [P] [US6] Add audit writer tests for non-sensitive before/after values, sensitive redaction, `SYSTEM_BOOTSTRAP` system actor audit records, no audit entry on failed validation, and no audit entry on unauthorized attempts in `HR.Tests/Audit/AuditWriterTests.cs`.
- [X] T112 [P] [US6] Add audit query tests for entity type, entity id, actor, action, date range, pagination, and ordering in `HR.Tests/Audit/AuditLogQueryTests.cs`.
- [X] T113 [P] [US6] Add audit authorization tests for Employee, Manager, HRAdministrator, and SystemAdministrator in `HR.Tests/Audit/AuditAuthorizationTests.cs`.
- [X] T114 [P] [US6] Add audit controller contract tests for `GET /api/audit-logs` in `HR.Tests/Audit/AuditLogsControllerTests.cs`.

### Implementation for User Story 6

- [X] T115 [US6] Implement audit log service and mapping in `HR.Infrastructure/Audit/AuditLogService.cs`.
- [X] T116 [US6] Implement audit writer helper for successful write operations in `HR.Infrastructure/Audit/AuditWriter.cs`.
- [X] T117 [US6] Register `IAuditLogService` and audit writer dependencies in `HR.Infrastructure/DependencyInjection.cs`.
- [X] T118 [US6] Add `AuditLogsController` with filterable paginated search in `HR.API/Controllers/AuditLogsController.cs`.
- [X] T119 [US6] Integrate audit writer calls into initial bootstrap, Phase 7 role assignment, attendance, compensation, document, and existing employee/department/vacation/trip write services in `HR.Infrastructure/Authorization/InitialSystemAdminBootstrapper.cs`, `HR.Infrastructure/Employees/EmployeeService.cs`, `HR.Infrastructure/Attendance/AttendanceService.cs`, `HR.Infrastructure/Compensation/CompensationService.cs`, `HR.Infrastructure/Documents/EmployeeDocumentService.cs`, `HR.Infrastructure/Departments/DepartmentService.cs`, `HR.Infrastructure/VacationRequests/VacationRequestService.cs`, and `HR.Infrastructure/Transportation/TripService.cs`.
- [X] T120 [US6] Run focused audit tests from `HR.Tests/Audit/`.

**Checkpoint**: Audit history is searchable by authorized users and sensitive values are not duplicated raw.

---

## Phase 9: Migration, Integration, and Polish

**Purpose**: Create the approved Phase 7 migration, validate cross-story behavior, and prepare implementation handoff.

- [X] T121 Create the Phase 7 EF migration with `dotnet ef migrations add Phase7AdvancedHrFeatures --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj`; expected files are under `HR.Infrastructure/Data/Migrations/`.
- [X] T122 Review the generated migration in `HR.Infrastructure/Data/Migrations/*Phase7AdvancedHrFeatures*.cs` to confirm existing employees default to `Employee`, no existing migrations are edited, and the initial System Administrator bootstrap is not implemented as fake seed data.
- [X] T123 Run `dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj` after migration creation and resolve pending model mismatches.
- [X] T124 Add migration/backfill documentation for Phase 7 role bootstrap and existing-row behavior in `HR.Infrastructure/Data/Migrations/Phase7AdvancedHrFeaturesMigrationStrategy.md`, including the idempotent `CreateInitialAdmin` startup/bootstrap activation path, invalid configuration failure behavior, no fallback administrator assignment, no partial records, and `SYSTEM_BOOTSTRAP` audit expectation.
- [X] T125 Run full `dotnet restore .\HR.slnx`, `dotnet build .\HR.slnx -c Release`, and `dotnet test .\HR.slnx -c Release --no-build` from the repository root.
- [X] T126 Run static checks from `specs/007-advanced-hr-features/quickstart.md` and confirm `HR.Application` has no infrastructure reference, normal employee DTOs expose no compensation values, and `Program.cs` has no direct application/infrastructure service registrations.
- [X] T127 Apply Phase 7 migration to local `HrSystemDb` with `dotnet ef database update --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj`.
- [X] T128 Start `HR.API/HR.API.csproj` against local `HrSystemDb` and complete the local SQL Server smoke flow in `specs/007-advanced-hr-features/quickstart.md`.
- [X] T129 Re-run representative Phase 5 regression flows for vacations, employee status/soft-delete/auth revocation, trips, and department counts using existing tests under `HR.Tests/`.
- [X] T130 Run `git diff --check` for changed Phase 7 files and source files.
- [X] T131 Review final changed files against `specs/007-advanced-hr-features/plan.md` and confirm changes are limited to Phase 7 advanced HR features, the approved migration, tests, and Spec Kit docs.
- [X] T132 Update `specs/007-advanced-hr-features/implementation-summary.md` with commands run, migration name, local database update status, initial System Administrator bootstrap configuration used locally, business timezone, document storage root, smoke results, and remaining risks.

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user stories.
- **US1 Attendance (Phase 3)**: Depends on Foundational.
- **US2 RBAC (Phase 4)**: Depends on Foundational. Should complete before US3-US6 are exposed in a real environment because those stories contain sensitive access.
- **US3 Compensation (Phase 5)**: Depends on Foundational and should use US2 authorization.
- **US4 Documents (Phase 6)**: Depends on Foundational and should use US2 authorization.
- **US5 Dashboard (Phase 7)**: Depends on Foundational and should use US2 authorization.
- **US6 Audit Logs (Phase 8)**: Depends on Foundational and integrates with completed write workflows from US2-US4 plus existing services.
- **Migration/Polish (Phase 9)**: Depends on selected user stories being complete.

### User Story Dependencies

- **US1 Record Daily Attendance**: MVP candidate after Foundation; does not require compensation, documents, dashboard, or audit-log read UI.
- **US2 Enforce Role-Based Access**: Parallelizable with US1 after Foundation, but should be completed before sensitive US3-US6 endpoints are manually exposed.
- **US3 Manage Compensation Securely**: Depends on US2 authorization behavior.
- **US4 Manage Employee Documents**: Depends on US2 authorization behavior and document storage foundation.
- **US5 View HR Dashboard Summary**: Depends on US2 authorization behavior and existing Phase 5 data.
- **US6 Review Audit History**: Best after US2-US4 so audit integration can cover real successful write actions.

### Within Each User Story

- Tests are written before implementation tasks.
- DTOs/contracts and entities are available from Foundation before services.
- Repositories are available before infrastructure services.
- Services are implemented before controllers.
- Controller contract tests pass after service and endpoint wiring.
- Story checkpoint should pass before moving to the next priority in single-developer flow.

## Parallel Execution Examples

### Setup and Foundation

```text
Parallel tasks:
- T005 create test folders
- T006 create application folders
- T007 create infrastructure folders
- T011/T012/T013 add independent enums
- T015/T016/T017/T018/T019 add independent entities
- T025/T026/T027/T028/T029 add independent EF configurations
- T030/T031/T032/T033/T034 add independent repository interfaces
- T041/T042/T043/T044/T045/T046 add independent service contracts
```

### User Story 1

```text
Parallel tasks:
- T063 attendance service tests
- T064 attendance timezone tests
- T065 attendance eligibility tests
- T066 attendance controller tests
Then:
- T067 service
- T069 controller
```

### User Story 2

```text
Parallel tasks:
- T073 access matrix tests
- T074 manager scope tests
- T075 bootstrap tests
- T076 role controller tests
- T077 auth compatibility tests
Then:
- T080 auth role integration
- T081 role update service
- T082 role endpoint
```

### User Stories 3 and 4

```text
After US2 authorization is stable, different developers can work in parallel:
- US3 compensation tasks T085-T093
- US4 document tasks T094-T102
```

### User Stories 5 and 6

```text
Dashboard query tests and audit query tests can be developed in parallel:
- T103/T104/T105 dashboard tests
- T111/T112/T113/T114 audit tests
Audit integration task T119 should wait until target write services exist.
```

## Implementation Strategy

### MVP First

1. Complete Setup.
2. Complete Foundational.
3. Complete US1 Attendance.
4. Stop and validate attendance independently with focused tests.
5. Continue to US2 before exposing sensitive Phase 7 features.

### Security-First Increment

1. Complete Setup and Foundational.
2. Complete US2 RBAC.
3. Validate initial System Administrator bootstrap and access matrix.
4. Implement US3-US6 using RBAC from the start.

### Incremental Delivery

1. Setup + Foundation.
2. US1 Attendance.
3. US2 RBAC.
4. US3 Compensation.
5. US4 Documents.
6. US5 Dashboard.
7. US6 Audit.
8. Migration, full validation, local SQL Server smoke, and handoff.

## Notes

- Phase 7 intentionally creates one new migration during implementation; do not modify existing migration files.
- Do not store document binary content in SQL Server business records.
- Do not expose compensation values through normal employee DTOs.
- Do not replace cookie authentication or introduce JWT.
- Do not move infrastructure-backed implementations into `HR.Application`.
- Do not run implementation before Phase 7 checklist review if a new checklist is generated and incomplete.
