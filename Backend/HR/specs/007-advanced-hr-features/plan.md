# Implementation Plan: Phase 7 - Advanced HR Features

**Branch**: `007-advanced-hr-features` | **Date**: 2026-06-07 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/007-advanced-hr-features/spec.md`

## Summary

Add the planned advanced HR capabilities on top of the completed Phase 5 business rules and Phase 6 registration cleanup: attendance clock-in/out, single-role RBAC, secure compensation records, backend-managed employee document storage, dashboard summary metrics, and audit-log review.

The implementation should extend the existing five-project layered architecture. New public request/response contracts and service interfaces belong in `HR.Application`; new domain entities and enums belong in `HR.Domain`; persistence, repositories, local file storage, audit writing, timezone handling, and infrastructure-backed service implementations belong in `HR.Infrastructure`; controllers, authorization policies, cookie claims, multipart handling, and HTTP response mapping remain in `HR.API`.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core 8, ASP.NET Core Identity, Entity Framework Core 8.0.20, SQL Server provider, System.Text.Json, Microsoft.Extensions.Options, TimeProvider, TimeZoneInfo, backend local file storage via .NET file APIs, Swashbuckle

**Storage**: Existing SQL Server database through `ApplicationDbContext`; new Phase 7 tables/columns require one approved EF Core migration. Employee document binary files are stored in backend-managed local storage outside public static folders; only metadata is stored in SQL Server. SQLite in-memory remains the automated test store.

**Testing**: xUnit, SQLite integration tests, focused authorization/access-matrix tests, service-level business-rule tests, controller compatibility tests, file-storage failure/cleanup tests, EF migration pending-model checks, local SQL Server smoke validation.

**Target Platform**: Windows or Linux server hosted with Kestrel or IIS

**Project Type**: Layered ASP.NET Core Web API

**Performance Goals**: Attendance and role checks should add no noticeable user-facing latency to existing HR workflows. Dashboard summary should return within normal interactive use for the current project scale and remain paginated or bounded where lists are returned. Document upload/download size is explicitly capped by configuration.

**Constraints**: Preserve cookie-based sessions, existing login response fields, existing claims, existing routes and response shapes outside explicitly additive Phase 7 contracts, structured `{ code, message }` errors, Phase 5 business rules, Phase 6 DI ownership boundaries, pagination defaults, cancellation forwarding, scoped lifetimes, and local `HrSystemDb` compatibility after migrations. Do not introduce JWT, external storage services, payroll, benefits, recruiting, performance reviews, CQRS, MediatR, generic repositories, or microservice boundaries.

**Scale/Scope**: Six feature slices, one migration, new repositories/services/controllers/tests, configurable business timezone, configurable document storage, initial System Administrator bootstrap, and representative local SQL Server smoke validation.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Layered Architecture (I)**: PASS. New contracts and DTOs stay in `HR.Application`; new entities/enums stay in `HR.Domain`; EF, repositories, file storage, audit persistence, and option binding stay in `HR.Infrastructure`; HTTP controllers and authorization policy wiring stay in `HR.API`.
- **Cookie-Based Session Authentication (II)**: PASS. Phase 7 keeps cookie auth and adds role data as additive authorization context. JWT, token refresh, and alternate auth mechanisms remain out of scope. Existing cookie settings and JSON `401`/`403` behavior are preserved.
- **Service Layer Separation (III)**: PASS. Controllers remain thin HTTP adapters. New write workflows return `Result<T>` or `Result`. New list workflows return DTOs or `PagedList<T>`. Async methods accept and forward `CancellationToken`.
- **Domain Integrity (IV)**: PASS. Phase 5 state-machine and HR rules remain active. Phase 7 adds explicit invariants for attendance, role authorization, compensation history, document storage, and audit logging.
- **Global Error Handling (V)**: PASS. Expected failures continue using `ServiceError` and structured `{ code, message }` payloads. Existing compatibility codes are not renamed.
- **Data Access Abstraction (VI)**: PASS. New aggregate access goes through concrete repositories. New entity configurations use `IEntityTypeConfiguration<T>` and `ApplicationDbContext.OnModelCreating` remains `base.OnModelCreating(builder)` followed by `ApplyConfigurationsFromAssembly`.
- **Simplicity & YAGNI (VII)**: PASS. The plan uses explicit services and repositories only. No CQRS, MediatR, event sourcing, generic repository base, external storage integration, or new architecture boundary is introduced.

### Post-Design Re-check

The Phase 0 and Phase 1 artifacts preserve all constitution gates. The only schema change is the planned Phase 7 migration for approved Phase 7 data. The only authorization expansion is the approved RBAC feature. No complexity exceptions are required.

## Technical Approach

### 1. Role-Based Access Control Foundation

Add a single employee role value with the allowed roles: `Employee`, `Manager`, `HRAdministrator`, and `SystemAdministrator`.

Migration/backfill strategy:

1. Existing employees default to `Employee`.
2. Startup checks whether any active `SystemAdministrator` already exists.
3. If an active `SystemAdministrator` exists, bootstrap does nothing: it must not create another admin, overwrite roles, or reset passwords.
4. If no active `SystemAdministrator` exists, the primary approved path is `InitialAdminBootstrap:Mode = CreateInitialAdmin`, which creates the initial `ApplicationUser` and linked `Employee` from secure configuration and assigns `Employee.Role = SystemAdministrator`.
5. If required bootstrap configuration is missing or invalid, or configured email/employee number already exists inconsistently, startup/bootstrap validation fails with a clear configuration/business error before RBAC can lock out administration.
6. Do not grant HR administrator or system administrator permissions to all existing employees.

Bootstrap activation path:

- The initial System administrator creation must run through a clearly defined startup/bootstrap activation mechanism before administrative RBAC enforcement can lock out all administrators.
- The bootstrap mechanism must be idempotent or safely guarded against duplicate creation.
- Required configuration includes `Enabled`, `Mode`, `EmployeeNumber`, `Email`, `FullName`, `DepartmentId`, `TemporaryPassword`, and `ForcePasswordChange`.
- The current employee schema requires `DepartmentId`; bootstrap must use a configured existing department and fail clearly if it is missing.
- Sensitive values, especially `TemporaryPassword`, should come from environment variables or user secrets in real deployments and must not be committed as customer-specific source defaults.
- Missing configuration, invalid mode, duplicate employee number, duplicate employee email, invalid password, or missing department must fail clearly and must not assign administrator rights to any fallback employee.
- User creation, employee creation, role assignment, and audit writing must occur in one transactional flow; partial bootstrap records must be rolled back.
- Successful bootstrap creation must be audited with system actor marker `SYSTEM_BOOTSTRAP`, affected employee id, assigned role, employee number, email, and UTC timestamp. Temporary passwords, password hashes, tokens, security stamps, and cookies must not be audited.

Configuration:

```json
{
  "InitialAdminBootstrap": {
    "Enabled": true,
    "Mode": "CreateInitialAdmin",
    "EmployeeNumber": "set-per-deployment",
    "Email": "set-per-deployment",
    "FullName": "set-per-deployment",
    "DepartmentId": "00000000-0000-0000-0000-000000000000",
    "TemporaryPassword": "set-via-user-secrets-or-environment",
    "ForcePasswordChange": true
  }
}
```

Implementation ownership:

- Role enum belongs in `HR.Domain/Enums`.
- Employee role storage belongs on `Employee` unless implementation discovers a stronger reason for a separate one-to-one role table during task breakdown.
- System administrator role assignment endpoint belongs to the employee administration surface and is restricted to System administrators.
- Authorization policies remain host-owned in `HR.API/Program.cs` or host extension files, while the service-level authorization decisions that need manager-chain or employee-state data remain in infrastructure-backed services.
- Existing login response fields remain stable; role fields may be additive. Existing claims remain stable; a role claim may be additive.

### 2. Attendance Tracking

Add attendance records for clock-in/clock-out workflows.

Rules:

- Only active, non-soft-deleted authenticated employees can record attendance.
- Clock-in creates one open attendance record for the employee's attendance business date.
- Duplicate clock-in for the same employee/date is rejected.
- Clock-out requires an open same-date attendance record and must be after clock-in.
- Actual clock-in/out timestamps are stored in UTC.
- `AttendanceDate` is derived from a configured named business timezone, not from a client-provided date and not from unnamed server local time.
- Missing or invalid business timezone configuration fails fast.

Configuration:

```json
{
  "BusinessSettings": {
    "TimeZoneId": "Africa/Cairo"
  }
}
```

Tests must pin the configured timezone explicitly, including cases near UTC/local-day boundaries.

### 3. Compensation and Salary History

Compensation is sensitive and must remain outside normal employee list/detail DTOs.

Plan:

- Store current compensation in a dedicated compensation profile related one-to-one to an employee.
- Store accepted value-changing compensation updates in salary history.
- Keep no-compensation as a valid default state for existing employees.
- Allow only HR administrators and System administrators to view or edit compensation.
- Do not include salary values in normal employee, dashboard, document, attendance, vacation, or trip responses.
- No-change compensation updates return success without duplicate salary-history side effects.

### 4. Employee Document Management

Use backend-managed local storage for binary files and SQL Server metadata for document records.

Configuration:

```json
{
  "DocumentStorage": {
    "RootPath": "App_Data/EmployeeDocuments",
    "AllowedExtensions": [".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx"],
    "MaxFileSizeBytes": 10485760
  }
}
```

Rules:

- Store files outside public static folders unless explicitly protected.
- Downloads go through authorized API endpoints only.
- Generate safe stored file names; keep raw uploaded names only as metadata.
- Validate allowed file types and maximum size before accepting an upload.
- Prevent path traversal by never composing storage paths from raw uploaded names.
- If file saving fails, do not create metadata.
- If metadata persistence fails after a file is saved, clean up the saved file.
- Removing a document marks metadata removed or soft-deleted, deletes the physical file from backend-managed local storage, prevents future downloads, and writes an audit entry.
- Oversized uploads return `413 Payload Too Large` with the standard structured `{ code, message }` payload.

### 5. Dashboard Summary

Add a read-only dashboard summary service.

Metrics:

- total active employees
- total departments
- pending vacation requests
- approved vacations this month
- employees on vacation today
- new hires this month
- upcoming trips this week
- employees per department
- vacation requests by status

Scope:

| Metric | Manager | HR Administrator | System Administrator |
|--------|---------|------------------|----------------------|
| Total active employees | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| Total departments | Hidden / not applicable | Organization-wide | Organization-wide |
| Pending vacation requests | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| Approved vacations this month | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| Employees on vacation today | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| New hires this month | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| Upcoming trips this week | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| Employees per department | Team-scoped direct and indirect reports grouped by department | Organization-wide | Organization-wide |
| Vacation requests by status | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |

Employees without dashboard access receive structured `403`. Hidden or not applicable manager metrics must not expose organization-wide values. Dashboard data is derived from current HR records. No dashboard table is planned unless implementation later proves a real performance need.

### 6. Audit Logging

Record successful significant write actions for employee, department, vacation, trip, attendance, compensation, document, and role-assignment changes.

Rules:

- Audit entries include affected entity type, affected entity id, action type, actor employee id or documented system actor marker, action time, changed field names, and non-sensitive before/after values.
- Initial bootstrap admin creation uses system actor marker `SYSTEM_BOOTSTRAP`, not a human actor, and records affected employee id, assigned role, employee number, email, and UTC timestamp.
- Sensitive values are redacted or summarized in general audit logs, including compensation amounts, document content, and protected storage details.
- Failed validation and unauthorized attempts do not create successful-change audit entries.
- Audit-log read access is HR administrator and System administrator only.
- Audit-log results are paginated and filterable by entity type, entity id, actor, action, and date range.

## Files and Modules Likely to Change

### Existing Files

```text
HR.API/
|-- Program.cs
|-- Controllers/
|   |-- AuthController.cs
|   |-- EmployeesController.cs
|   |-- VacationRequestsController.cs
|   `-- TripsController.cs
|-- Extensions/
`-- appsettings.json

HR.Application/
|-- DependencyInjection.cs
|-- Auth/IAuthService.cs
|-- DTOs/Auth/
|-- DTOs/Employees/
|-- Employees/IEmployeeService.cs
|-- Transportation/ITripService.cs
`-- VacationRequests/IVacationRequestService.cs

HR.Domain/
|-- Entities/
`-- Enums/

HR.Infrastructure/
|-- DependencyInjection.cs
|-- Data/ApplicationDbContext.cs
|-- Data/Configurations/
|-- Data/Migrations/
|-- Repositories/
|-- Auth/
|-- Employees/
|-- Departments/
|-- Transportation/
`-- VacationRequests/

HR.Tests/
|-- Auth/
|-- Compatibility/
|-- TestInfrastructure/SqliteTestEnvironment.cs
`-- DependencyInjection/

AGENTS.md
PLAN.md
```

### New Files

```text
HR.API/
|-- Controllers/
|   |-- AttendanceController.cs
|   |-- CompensationController.cs
|   |-- EmployeeDocumentsController.cs
|   |-- DashboardController.cs
|   `-- AuditLogsController.cs
`-- Extensions/AuthorizationPolicyExtensions.cs

HR.Application/
|-- Attendance/
|   `-- IAttendanceService.cs
|-- Authorization/
|   `-- IEmployeeAccessService.cs
|-- Compensation/
|   `-- ICompensationService.cs
|-- Documents/
|   `-- IEmployeeDocumentService.cs
|-- Dashboard/
|   `-- IDashboardService.cs
|-- Audit/
|   `-- IAuditLogService.cs
`-- DTOs/
    |-- Attendance/
    |-- Compensation/
    |-- Documents/
    |-- Dashboard/
    |-- Audit/
    `-- Employees/EmployeeRoleUpdateRequest.cs

HR.Domain/
|-- Entities/
|   |-- AttendanceRecord.cs
|   |-- EmployeeCompensation.cs
|   |-- SalaryHistoryEntry.cs
|   |-- EmployeeDocument.cs
|   `-- AuditLogEntry.cs
`-- Enums/
    |-- EmployeeRole.cs
    |-- EmployeeDocumentCategory.cs
    `-- AuditActionType.cs

HR.Infrastructure/
|-- Attendance/
|-- Authorization/
|-- Compensation/
|-- Documents/
|-- Dashboard/
|-- Audit/
|-- Configuration/
|   |-- BusinessSettings.cs
|   `-- DocumentStorageOptions.cs
|-- FileStorage/
|   `-- LocalEmployeeDocumentStorage.cs
|-- Data/Configurations/
`-- Repositories/

HR.Tests/
|-- Attendance/
|-- Authorization/
|-- Compensation/
|-- Documents/
|-- Dashboard/
|-- Audit/
`-- Migration/
```

### Files Expected to Remain Unchanged Unless Compatibility Requires It

```text
HR.Shared/Serialization/
HR.Shared/Pagination/
HR.Infrastructure/Data/Migrations/20251114215718_InitialCreate.*
HR.Infrastructure/Data/Migrations/20260603014628_Phase5HrBusinessRules.*
```

## Data Model Changes

See [data-model.md](./data-model.md).

Expected schema changes:

- Add `Employee.Role` with safe default `Employee`.
- Add attendance records.
- Add compensation profiles.
- Add salary history entries.
- Add employee document metadata.
- Add audit log entries.
- Add indexes required for authorization, attendance date lookups, dashboard aggregation, document listing, and audit filtering.

The implementation must create a new Phase 7 EF Core migration. Existing migrations must not be edited.

## Internal and External Contracts

See [phase7-api-contract.md](./contracts/phase7-api-contract.md).

## API and Route Changes

Phase 7 adds new endpoints for attendance, role assignment, compensation, employee documents, dashboard summary, and audit logs. Compensation endpoints are implemented only on `CompensationController` using `GET /api/employees/{id}/compensation` and `PUT /api/employees/{id}/compensation`; do not add parallel employee-controller compensation actions. Existing Phase 0-6 routes remain stable except for additive response fields needed for RBAC, such as employee role on auth/current-user or employee responses. Compensation fields must not be added to normal employee responses.

## UI and Component Changes

No frontend implementation is included in this backend plan. Backend contracts should be sufficient for an Angular frontend to add attendance controls, role-aware navigation, compensation screens, document screens, dashboard summary, and audit-log review in a later frontend task set.

## Validation and Error Handling

- Preserve `GlobalExceptionMiddleware` as the first middleware.
- Preserve JSON `401` and `403` behavior for cookie authentication and authorization.
- Map expected Phase 7 failures through `ServiceError` to the existing `{ code, message }` shape.
- Use `403` for denied permissions, `401` for missing/invalid authentication, `404` for missing resources, `409` for conflicts such as duplicate attendance records or storage conflicts where appropriate, and `422` for domain/business-rule violations where appropriate.
- Use `413 Payload Too Large` with the existing structured `{ code, message }` shape for oversized document uploads.
- Do not rename existing compatibility error codes as part of Phase 7.
- Validate all preconditions before state mutation.
- For multi-step document upload, file save and metadata persistence must cleanly handle partial failure.
- For document removal, metadata removal, physical file deletion, download unavailability, and audit writing must be validated as one policy.
- For multi-entity writes, use the existing unit-of-work/transaction pattern.

## Testing and Check Strategy

### Automated Checks

1. Run `dotnet restore .\HR.slnx`.
2. Run `dotnet build .\HR.slnx -c Release`.
3. Run `dotnet test .\HR.slnx -c Release --no-build`.
4. Run focused service tests for attendance, RBAC, compensation, documents, dashboard, and audit logs.
5. Run controller/contract tests for new endpoints and existing compatibility responses.
6. Run migration tests or EF checks to confirm the model matches the new migration.

### Static Checks

```powershell
rg -n "ApplicationDbContext" .\HR.Infrastructure -g "*Service.cs"
rg -n "HR.Infrastructure" .\HR.Application
rg -n "BaseSalary|Salary|Compensation" .\HR.Application\DTOs\Employees
rg -n "AddScoped<" .\HR.API\Program.cs
git status --short .\HR.Infrastructure\Data\Migrations
git diff --check
```

Expected results:

- Existing service classes do not newly depend directly on `ApplicationDbContext`; repository access remains the default pattern.
- `HR.Application` does not reference `HR.Infrastructure`.
- Normal employee DTOs do not expose compensation values.
- `Program.cs` remains free of direct application/infrastructure service registrations; authorization policy configuration is allowed as host-owned behavior.
- Exactly one new Phase 7 migration pair and snapshot update are present after implementation.
- `git diff --check` reports no whitespace errors, allowing existing line-ending warnings if present.

### Manual Local SQL Server Smoke Validation

Use [quickstart.md](./quickstart.md) after the Phase 7 migration is created and applied to local `HrSystemDb`.

Smoke coverage:

- startup with valid and invalid business timezone config
- initial System Administrator bootstrap creation, including startup-level no-lockout validation before RBAC enforcement succeeds
- login/current user with additive role data
- attendance clock-in/out
- role-denied and role-allowed actions
- compensation access isolation
- document upload/download/delete with local storage
- dashboard summary
- audit-log search
- Phase 5 regression workflows for vacations, employees, departments, and trips

## Migration and Backfill Strategy

1. Add new schema in a single Phase 7 migration.
2. Default existing employees to `Employee` role.
3. Create exactly one initial `SystemAdministrator` from secure `InitialAdminBootstrap` configuration through an explicit idempotent startup/bootstrap step when no active System administrator exists.
4. Existing employees start with no compensation profile unless one is entered later.
5. Existing employees start with no attendance records, document records, salary history, or audit-log backfill unless separately approved.
6. Existing Phase 5 trip requester data and employee soft-delete behavior remain untouched.
7. Existing rows must remain query-safe after the migration.
8. If any future non-null or uniqueness hardening depends on data cleanup, it must be done in a separate approved migration after backfill is possible.

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| RBAC locks out all administrators | Invoke idempotent bootstrap validation before administrative RBAC can lock out all administrators; create one configured initial admin when none exists, skip when one already exists, and fail clearly with no fallback assignment on invalid configuration. |
| Role changes break existing cookie/session behavior | Preserve existing claims and add role context additively; validate session eligibility on each request as Phase 5 already requires. |
| Manager-scoped authorization leaks cross-team data | Centralize manager-chain checks in a service and cover direct, indirect, unrelated, and soft-deleted employee cases. |
| Compensation leaks through normal employee DTOs | Keep compensation in dedicated DTOs/services and add static tests scanning normal employee DTOs. |
| File upload leaves orphan files or metadata | Use generated safe names, validate before save, and explicitly clean up saved files if metadata persistence fails. |
| Dashboard queries become too broad | Keep dashboard derived and bounded; add indexes and role-scoped query tests before considering cached summary tables. |
| Audit logs duplicate sensitive values | Redact or summarize sensitive values in general audit entries and keep detailed sensitive values in dedicated authorized records. |
| Timezone behavior varies by environment | Use a configured named timezone, validate it at startup, and pin tests to deterministic timezone values. |

## Dependencies on Previous Phases

- **Phase 0**: Five-project layered structure.
- **Phase 1**: Global exception middleware and pagination helpers.
- **Phase 2**: Cookie/session authentication.
- **Phase 3**: Service interfaces and thin controllers.
- **Phase 4**: Repository pattern, unit-of-work boundary, and entity configurations.
- **Phase 5**: HR business rules, soft deletion, employee session revocation, working-day calendar, trip requester ownership, and department employee counts.
- **Phase 6**: `AddApplication()` / `AddInfrastructure(configuration)` registration ownership and clean host composition.

## Out of Scope

- JWT, token refresh flows, OAuth/SSO, or new authentication mechanisms.
- Multiple simultaneous employee roles, temporary elevated grants, or permission union behavior.
- Payroll processing, benefit deductions, tax calculations, payslip generation, recruiting, performance reviews, or policy configuration.
- External document storage services, virus scanning services, OCR, document versioning, or document sharing links.
- Dashboard caching tables or background jobs unless a later performance review proves they are needed.
- Audit logging of failed login/security-monitoring events unless separately approved.
- Broad error-code normalization.
- Moving infrastructure-backed implementations into `HR.Application`.
- Generic repository base classes, MediatR, CQRS, event sourcing, microservices, or reflection-based registration scanners.

## Project Structure

### Documentation (this feature)

```text
specs/007-advanced-hr-features/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- phase7-api-contract.md
|-- checklists/
|   `-- requirements.md
`-- tasks.md                 # generated later by /speckit-tasks
```

### Source Code (repository root)

```text
HR.API/
|-- Controllers/             # HTTP adapters for new Phase 7 endpoints
|-- Extensions/              # host-owned authorization policy helpers
`-- Program.cs               # host composition, auth, authorization, pipeline

HR.Application/
|-- Attendance/              # service contracts
|-- Authorization/
|-- Compensation/
|-- Documents/
|-- Dashboard/
|-- Audit/
`-- DTOs/                    # request/response contracts

HR.Domain/
|-- Entities/                # Phase 7 entities
`-- Enums/                   # role/document/audit enums

HR.Infrastructure/
|-- Attendance/              # infrastructure-backed services
|-- Authorization/
|-- Compensation/
|-- Documents/
|-- Dashboard/
|-- Audit/
|-- FileStorage/
|-- Configuration/
|-- Repositories/
`-- Data/
    |-- Configurations/
    `-- Migrations/

HR.Shared/
|-- Pagination/
|-- Results/
`-- Serialization/

HR.Tests/
|-- Attendance/
|-- Authorization/
|-- Compensation/
|-- Documents/
|-- Dashboard/
|-- Audit/
|-- Auth/
|-- Compatibility/
`-- TestInfrastructure/
```

**Structure Decision**: Continue the existing five-project layered solution. Phase 7 adds feature folders that mirror the existing service/repository/controller pattern and preserves Phase 6 registration ownership.

## Complexity Tracking

No constitution violations or complexity exceptions are required.
