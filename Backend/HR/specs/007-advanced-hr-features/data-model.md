# Data Model: Phase 7 - Advanced HR Features

## Existing Entity Updates

### Employee

Existing employee profile. Phase 7 adds role-based authorization data.

**New fields**:

- `Role`: EmployeeRole, required, default `Employee`

**Validation and rules**:

- Existing employees are backfilled to `Employee`.
- When no active System administrator exists, one configured initial admin employee is created and assigned `SystemAdministrator` during idempotent startup/bootstrap activation.
- Each employee has exactly one current role.
- Soft-deleted or terminated employees remain ineligible for authentication and cannot regain access through retained role data.
- Employee number remains immutable.
- Existing Phase 5 status and soft-delete rules remain unchanged.

**Relationships**:

- One employee may have many attendance records.
- One employee may have zero or one compensation profile.
- One employee may have many salary history entries.
- One employee may have many document records.
- One employee may be an actor on many audit log entries.
- System bootstrap actions may create audit log entries using `SYSTEM_BOOTSTRAP` instead of a human employee actor.

## New Entities

### AttendanceRecord

A daily attendance record for one employee.

**Fields**:

- `Id`: Guid, required
- `EmployeeId`: Guid, required
- `Employee`: Employee navigation
- `AttendanceDate`: DateOnly, required; derived from configured business timezone
- `ClockInAtUtc`: DateTimeOffset, required
- `ClockOutAtUtc`: DateTimeOffset?, optional
- `Notes`: string?, optional, max 500
- `CreatedAt`: DateTimeOffset, required UTC
- `UpdatedAt`: DateTimeOffset?, optional UTC

**Validation and rules**:

- Only active, non-soft-deleted, non-terminated employees may create or complete attendance.
- One attendance record per employee per attendance date.
- Clock-out requires an existing open same-date record.
- Clock-out must be later than clock-in.
- Future attendance dates are rejected.
- Attendance date is never accepted from the client.
- Actual clock-in/out timestamps are stored in UTC.

**Indexes**:

- Unique: `(EmployeeId, AttendanceDate)`
- Query: `(AttendanceDate)`

### EmployeeCompensation

Sensitive current compensation details for an employee.

**Fields**:

- `Id`: Guid, required
- `EmployeeId`: Guid, required
- `Employee`: Employee navigation
- `BaseSalary`: decimal, required, non-negative
- `SalaryCurrency`: string, required, max 3 or deployment-approved currency code length
- `LastSalaryReviewDate`: DateOnly?, optional
- `CreatedAt`: DateTimeOffset, required UTC
- `UpdatedAt`: DateTimeOffset?, optional UTC

**Validation and rules**:

- Only HR administrators and System administrators may view or update.
- Existing employees may have no compensation profile.
- Compensation values must not appear in normal employee responses.
- Salary currency defaults to the deployment policy, expected `EGP` for local development unless changed.

**Indexes**:

- Unique: `(EmployeeId)`

### SalaryHistoryEntry

A traceable compensation-change record.

**Fields**:

- `Id`: Guid, required
- `EmployeeId`: Guid, required
- `Employee`: Employee navigation
- `PreviousBaseSalary`: decimal?, optional
- `NewBaseSalary`: decimal?, optional
- `PreviousCurrency`: string?, optional
- `NewCurrency`: string?, optional
- `PreviousReviewDate`: DateOnly?, optional
- `NewReviewDate`: DateOnly?, optional
- `ChangedByEmployeeId`: Guid, required
- `ChangedBy`: Employee navigation
- `ChangedAt`: DateTimeOffset, required UTC

**Validation and rules**:

- Created only when an accepted compensation request changes at least one compensation value.
- No-change compensation requests do not create duplicate history.
- Accessible only through authorized compensation workflows.

**Indexes**:

- Query: `(EmployeeId, ChangedAt)`
- Query: `(ChangedByEmployeeId, ChangedAt)`

### EmployeeDocument

Metadata for a file stored in backend-managed local document storage.

**Fields**:

- `Id`: Guid, required
- `EmployeeId`: Guid, required
- `Employee`: Employee navigation
- `Category`: EmployeeDocumentCategory, required
- `OriginalFileName`: string, required, max 255
- `StoredFileName`: string, required, max 255, generated safe name
- `ContentType`: string, required, max 100
- `FileExtension`: string, required, max 16
- `FileSizeBytes`: long, required
- `StorageRelativePath`: string, required, max 500
- `UploadedByEmployeeId`: Guid, required
- `UploadedBy`: Employee navigation
- `UploadedAt`: DateTimeOffset, required UTC
- `RemovedAt`: DateTimeOffset?, optional UTC
- `RemovedByEmployeeId`: Guid?, optional
- `RemovedBy`: Employee navigation

**Validation and rules**:

- Only HR administrators and System administrators may manage documents in Phase 7.
- Missing, soft-deleted, or unauthorized employee records reject document operations.
- Binary content is never stored in business records.
- Raw uploaded file names are metadata only; storage uses generated safe names.
- Allowed extensions and maximum file size come from configuration.
- Path traversal attempts are rejected.
- Downloads go through authorized endpoints only.
- If file save fails, metadata is not created.
- If metadata save fails after file save, the saved file is removed.
- Removed documents are soft-deleted in metadata through `RemovedAt` and `RemovedByEmployeeId`.
- Removing a document deletes the physical file from backend-managed local storage.
- Removed documents are excluded from normal lists and retrieval, and downloads after removal are unavailable.
- Document upload and removal actions are audited.

**Indexes**:

- Query: `(EmployeeId, RemovedAt, UploadedAt)`
- Unique or indexed: `(StoredFileName)`

### AuditLogEntry

A trace record for a significant successful write action.

**Fields**:

- `Id`: Guid, required
- `EntityType`: string, required, max 100
- `EntityId`: Guid, required
- `ActionType`: AuditActionType or string, required, max 64
- `ActorEmployeeId`: Guid?, required for authenticated human actions and null for system actor actions
- `Actor`: Employee navigation, optional for system actor actions
- `ActorMarker`: string?, optional max 64; required for system actor actions, with `SYSTEM_BOOTSTRAP` used for initial bootstrap
- `PerformedAt`: DateTimeOffset, required UTC
- `ChangedFields`: string, required; serialized list of changed field names
- `OldValues`: string?, optional; serialized non-sensitive before values
- `NewValues`: string?, optional; serialized non-sensitive after values
- `SensitiveSummary`: string?, optional; redacted or summarized sensitive change detail

**Validation and rules**:

- Created only for successful significant write operations.
- Failed validation and unauthorized attempts do not create successful-change audit entries.
- Sensitive values are redacted or summarized, not duplicated raw.
- Initial System administrator bootstrap creates an audit entry using `ActorMarker = SYSTEM_BOOTSTRAP`, affected employee id, assigned role, employee number, email, and UTC timestamp, without recording temporary passwords, password hashes, tokens, security stamps, or cookies.
- Audit-log reads are HR administrator and System administrator only.
- Results are paginated.

**Indexes**:

- Query: `(EntityType, EntityId, PerformedAt)`
- Query: `(ActorEmployeeId, PerformedAt)`
- Query: `(ActorMarker, PerformedAt)`
- Query: `(ActionType, PerformedAt)`

## New Enums

### EmployeeRole

- `Employee`
- `Manager`
- `HRAdministrator`
- `SystemAdministrator`

### EmployeeDocumentCategory

Suggested values:

- `Identity`
- `Contract`
- `Certificate`
- `Other`

Planning note: keep the category list small for Phase 7 unless tasks identify existing frontend values that must be preserved.

### AuditActionType

Suggested values:

- `Created`
- `Updated`
- `Deleted`
- `StatusChanged`
- `RoleChanged`
- `ClockedIn`
- `ClockedOut`
- `CompensationChanged`
- `DocumentUploaded`
- `DocumentRemoved`
- `SystemAdministratorBootstrapped`

## Read Models

### DashboardSummary

Derived read model, not a persisted table.

**Fields**:

- `TotalActiveEmployees`: int
- `TotalDepartments`: int
- `PendingVacationRequests`: int
- `ApprovedVacationsThisMonth`: int
- `EmployeesOnVacationToday`: int
- `NewHiresThisMonth`: int
- `UpcomingTripsThisWeek`: int
- `EmployeesPerDepartment`: dictionary of department name to count
- `VacationRequestsByStatus`: dictionary of status to count

**Rules**:

Dashboard metric scope:

| Metric | Manager | HR Administrator | System Administrator |
|--------|---------|------------------|----------------------|
| TotalActiveEmployees | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| TotalDepartments | Hidden / not applicable | Organization-wide | Organization-wide |
| PendingVacationRequests | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| ApprovedVacationsThisMonth | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| EmployeesOnVacationToday | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| NewHiresThisMonth | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| UpcomingTripsThisWeek | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |
| EmployeesPerDepartment | Team-scoped direct and indirect reports grouped by department | Organization-wide | Organization-wide |
| VacationRequestsByStatus | Team-scoped direct and indirect reports | Organization-wide | Organization-wide |

- Hidden or not applicable manager metrics must not expose organization-wide values.
- Employees without dashboard access are denied.
- Soft-deleted employees are excluded from current workforce metrics.

## Configuration Models

### BusinessSettings

- `TimeZoneId`: string, required named timezone

Rules:

- Must be configurable per deployment.
- Must be validated at startup.
- Missing or invalid value fails fast.
- Tests must pin the value explicitly.

### DocumentStorageOptions

- `RootPath`: string, required
- `AllowedExtensions`: string collection, required
- `MaxFileSizeBytes`: long, required positive value

Rules:

- Root path must not be a public static folder unless explicitly protected.
- Extensions are compared case-insensitively.
- Max size is enforced before saving.

### InitialAdminBootstrapOptions

- `Enabled`: bool, controls whether bootstrap creation is allowed when no active System administrator exists
- `Mode`: string, primary supported value `CreateInitialAdmin`
- `EmployeeNumber`: string, required for created employee
- `Email`: string, required for created identity user and employee
- `FullName`: string, required for created employee
- `DepartmentId`: Guid, required existing department id because the current employee model requires department membership
- `TemporaryPassword`: string, required when creating the first admin; should come from environment variables or user secrets in real deployments
- `ForcePasswordChange`: bool, accepted configuration flag; first-login enforcement is a follow-up unless existing Identity support can enforce it without schema changes

Rules:

- If an active System administrator already exists, bootstrap does nothing and does not require the creation fields.
- If no active System administrator exists, `CreateInitialAdmin` creates one linked `ApplicationUser` and `Employee`.
- Fails clearly for missing required fields, invalid mode, duplicate employee number, duplicate email, invalid password, or missing department.
- Does not assign administrator rights to any fallback employee.
- Successful bootstrap writes a `SYSTEM_BOOTSTRAP` audit record identifying the affected employee, assigned role, employee number, email, and timestamp.
- Temporary passwords, password hashes, tokens, security stamps, and cookies are never audited.

## Migration Notes

- Create a new Phase 7 migration only; do not edit existing migrations.
- Existing employee rows receive `Role = Employee`.
- Initial System Administrator creation is explicit, verified, idempotently bootstrapped from configuration, and not implemented as fake seed data.
- Existing employees do not receive compensation, attendance, document, salary-history, or audit-log backfill by default.
- Existing Phase 5 trip and vacation data remains unchanged.
