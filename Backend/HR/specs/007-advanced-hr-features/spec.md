# Feature Specification: Phase 7 - Advanced HR Features

**Feature Branch**: `[007-advanced-hr-features]`

**Created**: 2026-06-07

**Status**: Draft

**Input**: User description: "Read the existing project context and create a specification for phase 7"

## Boundary Definitions and Scope

- **Advanced HR features**: Attendance tracking, role-based access control, compensation visibility, employee document management, dashboard summary reporting, and audit-log visibility.
- **Current employee**: An authenticated employee whose profile is active and not soft-deleted.
- **Team member**: An employee who reports directly or indirectly to a manager through the existing manager relationship.
- **Sensitive HR data**: Compensation details, salary-change history, employee documents, audit logs, and role assignments.
- **Normal employee response**: Existing employee list/detail information used by non-compensation workflows. Compensation values are not part of this response.
- **Business timezone**: A deployment-configured named timezone used to derive local HR business dates, including attendance dates, while actual event timestamps remain stored in UTC.

Phase 7 introduces new HR capabilities and is allowed to add data required by those capabilities. It must build on the completed Phase 5 business rules and Phase 6 dependency-registration cleanup. It must not change the authentication mechanism, replace cookie-based sessions, weaken structured error compatibility, move infrastructure concerns into the application layer, or introduce unrelated product areas.

## Clarifications

### Session 2026-06-07

- Q: How should the first privileged administrator be selected when RBAC is activated? -> A: The system creates the initial System Administrator from secure configuration when no active System Administrator exists, using an idempotent startup/bootstrap activation path before RBAC can lock out administration.
- Q: Should employees have one role or multiple overlapping roles in Phase 7? -> A: Each employee has exactly one role: Employee, Manager, HR Administrator, or System Administrator.
- Q: How should uploaded employee document files be stored and protected? -> A: Use backend-managed local file storage with document metadata in system records; do not store file binary content in business records, use generated safe stored names, keep original names as metadata only, validate type and size, prevent path traversal, require authorized downloads, and clean up partial saves.
- Q: How should the system determine the attendance business date? -> A: Use a configurable named business timezone, store actual timestamps in UTC, derive AttendanceDate from that timezone, reject missing or invalid timezone configuration at startup, never trust client-provided dates or unnamed server local time, and pin the timezone in tests.
- Q: How should audit logs record before/after details for sensitive values? -> A: Audit logs store changed field names and non-sensitive before/after values; sensitive values are redacted or summarized.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Record Daily Attendance (Priority: P1)

As an active employee, I want to clock in and clock out for my workday so that attendance records reflect when I worked without manual spreadsheet tracking.

**Why this priority**: Attendance is the most operationally useful Phase 7 addition and can be validated independently without requiring compensation, documents, dashboard, or audit-log screens.

**Independent Test**: Can be fully tested by signing in as an active employee, recording clock-in and clock-out activity for the current workday, and checking that invalid repeated or out-of-order actions are rejected.

**Acceptance Scenarios**:

1. **Given** an active signed-in employee with no attendance record for today, **When** the employee clocks in, **Then** a dated attendance record is created with a clock-in time and no clock-out time.
2. **Given** an active signed-in employee who has clocked in today, **When** the employee clocks out later the same day, **Then** the attendance record is completed and worked time can be calculated.
3. **Given** an employee who already clocked in today, **When** the employee tries to clock in again for the same day, **Then** the duplicate clock-in is rejected.
4. **Given** an employee who has not clocked in today, **When** the employee tries to clock out, **Then** the clock-out is rejected.
5. **Given** a suspended, terminated, or soft-deleted employee, **When** the employee attempts attendance activity, **Then** the action is rejected.

---

### User Story 2 - Enforce Role-Based Access (Priority: P1)

As an HR administrator, I want employees, managers, HR administrators, and system administrators to have different access levels so that sensitive HR actions are limited to appropriate roles.

**Why this priority**: Phase 7 adds sensitive capabilities. Access control must be active before compensation, documents, audit logs, and administrative attendance views are safely exposed.

**Independent Test**: Can be fully tested by signing in as users assigned to each role and attempting representative actions that should be allowed or denied by the access matrix.

**Acceptance Scenarios**:

1. **Given** an employee role user, **When** the user views their own profile and records their own attendance, **Then** the actions are allowed.
2. **Given** an employee role user, **When** the user attempts to view all employees, edit employees, approve vacations, manage trips, view compensation, manage documents, or view audit logs, **Then** access is denied.
3. **Given** a manager role user, **When** the user views team members or reviews vacation requests for their team, **Then** the action is allowed within the team boundary.
4. **Given** a manager role user, **When** the user attempts HR-only actions outside the team boundary, **Then** access is denied.
5. **Given** an HR administrator or system administrator, **When** the user performs employee administration, department administration, trip management, compensation access, document management, dashboard review, or audit-log review, **Then** the action is allowed according to the Phase 7 access matrix.
6. **Given** no active System administrator exists and secure `InitialAdminBootstrap` configuration is valid, **When** the startup/bootstrap activation path runs, **Then** the system creates one linked `ApplicationUser` and `Employee`, assigns `SystemAdministrator`, and writes a `SYSTEM_BOOTSTRAP` audit entry before administrative RBAC enforcement can lock out all administrators.
7. **Given** an active System administrator already exists, **When** startup/bootstrap activation runs again, **Then** bootstrap does nothing, creates no duplicate admin, does not overwrite roles, and does not reset passwords.
8. **Given** no active System administrator exists and required bootstrap configuration is missing, invalid, duplicated, or uses a password that fails Identity policy, **When** startup/bootstrap activation runs, **Then** bootstrap fails clearly, creates no partial user or employee records, and assigns no fallback employee administrator rights.

---

### User Story 3 - Manage Compensation Securely (Priority: P2)

As an HR administrator, I want compensation details to be stored separately from regular employee profile details so that salary information is available to authorized staff without exposing it to regular employee views.

**Why this priority**: Compensation is sensitive and depends on role-based access being reliable. It delivers high business value but must remain isolated from normal profile workflows.

**Independent Test**: Can be fully tested by viewing and editing compensation as HR-authorized users, then confirming that regular employee profile responses and lower-privilege users do not expose compensation values.

**Acceptance Scenarios**:

1. **Given** an HR administrator or system administrator, **When** the user opens an employee's compensation details, **Then** base salary, currency, last review date, and salary history are available.
2. **Given** an HR administrator or system administrator, **When** the user updates an employee's compensation details, **Then** the current compensation changes and the previous and new values are recorded in salary history.
3. **Given** an employee or manager without compensation access, **When** the user views employee profile information, **Then** salary values are not included.
4. **Given** an unauthorized user, **When** the user requests compensation details or attempts a compensation update, **Then** access is denied.

---

### User Story 4 - Manage Employee Documents (Priority: P2)

As an HR administrator, I want to upload, list, retrieve, and remove employee documents so that contracts, certificates, and identity-related files are associated with the correct employee record.

**Why this priority**: Document management is a core HR workflow and must use the same access-control and audit expectations as other sensitive HR data.

**Independent Test**: Can be fully tested by adding a document for an employee, confirming authorized retrieval, confirming unauthorized denial, and removing the document from normal lists.

**Acceptance Scenarios**:

1. **Given** an HR administrator or system administrator, **When** the user uploads a valid document for an employee, **Then** the document is associated with that employee and records its name, category, size, uploader, and upload time.
2. **Given** an authorized user, **When** the user lists an employee's documents, **Then** the current documents for that employee are shown without exposing documents for other employees outside the user's permissions.
3. **Given** an authorized user, **When** the user retrieves a listed document, **Then** the document content and metadata are available.
4. **Given** an authorized user, **When** the user removes a document, **Then** document metadata is marked removed, the physical file is deleted from backend-managed local storage, downloads are no longer available, and the removal remains traceable.
5. **Given** an unauthorized user, **When** the user attempts to upload, retrieve, or remove employee documents, **Then** access is denied.

---

### User Story 5 - View HR Dashboard Summary (Priority: P3)

As an HR administrator or manager, I want a dashboard summary of workforce, vacation, and trip activity so that I can quickly understand the current HR state without running several separate lookups.

**Why this priority**: The dashboard is valuable once the underlying employee, vacation, trip, attendance, and role data is reliable. It is read-only and can be validated independently.

**Independent Test**: Can be fully tested by preparing representative employees, departments, vacation requests, and trips, then verifying that dashboard counts match the underlying records for an authorized user.

**Acceptance Scenarios**:

1. **Given** an HR administrator or system administrator, **When** the user opens the dashboard summary, **Then** organization-wide employee, department, vacation, trip, and distribution metrics are shown.
2. **Given** a manager, **When** the manager opens the dashboard summary, **Then** each metric is returned only according to the dashboard metric scope table.
3. **Given** an employee without dashboard access, **When** the employee attempts to view dashboard summary information, **Then** access is denied.
4. **Given** underlying employee, vacation, department, or trip records change, **When** an authorized user refreshes the dashboard summary, **Then** the visible metrics reflect the current records.

---

### User Story 6 - Review Audit History (Priority: P3)

As an HR administrator or system administrator, I want significant HR write actions to be recorded and searchable so that sensitive changes can be reviewed later for accountability.

**Why this priority**: Audit logging supports compliance and traceability across Phase 7 features, but it is most useful after the write workflows and access roles are defined.

**Independent Test**: Can be fully tested by performing representative create, update, delete, status, role, compensation, attendance, document, and trip actions, then verifying that authorized audit review shows who acted, what changed, when it happened, and which record was affected.

**Acceptance Scenarios**:

1. **Given** an employee, department, vacation, trip, attendance, compensation, document, or role assignment is created, updated, removed, or status-changed, **When** the operation succeeds, **Then** an audit entry is recorded.
2. **Given** the initial System administrator is created by bootstrap, **When** creation succeeds, **Then** an audit entry is recorded with system actor marker `SYSTEM_BOOTSTRAP`, the affected employee, assigned role, employee number, email, and timestamp, without recording temporary passwords, hashes, tokens, security stamps, or cookies.
3. **Given** an HR administrator or system administrator, **When** the user searches audit history by entity type, entity identifier, actor, action, or date range, **Then** matching audit entries are returned in a paginated result.
4. **Given** an unauthorized user, **When** the user attempts to view audit history, **Then** access is denied.
5. **Given** a failed validation or unauthorized attempt, **When** the operation does not change business data, **Then** no misleading successful-change audit entry is recorded.

### Edge Cases

- Existing employees must not be locked out when role-based access is activated; the startup/bootstrap activation path must ensure an active System administrator exists before privileged operations require roles.
- If no active System administrator exists, the primary approved bootstrap path is `InitialAdminBootstrap:Mode = CreateInitialAdmin`, which reads employee number, email, full name, department id, temporary password, and related settings from configuration or environment variables.
- If an active System administrator already exists, bootstrap must do nothing and must not create another administrator, overwrite roles, or reset passwords.
- Initial administrator bootstrap must be idempotent or safely guarded against duplicate creation, and it must not assign administrator rights to any fallback employee.
- If configured initial admin email or employee number already exists inconsistently, or the configured department is missing, bootstrap must fail clearly without partial user, employee, role, or audit records.
- Role activation must not silently grant broad administrative privileges to every existing employee.
- Each employee must have exactly one current role; Phase 7 does not support multiple simultaneous roles, temporary elevated grants, or permission union behavior.
- A soft-deleted or terminated employee must not gain access through a retained role assignment.
- Role changes for the currently signed-in user must take effect on subsequent protected actions without requiring a manual data reset.
- Manager-scoped access must prevent managers from viewing or approving records outside their reporting chain.
- Existing Phase 5 same-status, termination, vacation balance, trip requester, and soft-delete rules must remain active after Phase 7 access checks are added.
- Compensation values must not appear in normal employee profile, employee list, dashboard, document, attendance, vacation, or trip results.
- Salary changes with no actual value change must not create duplicate salary-history side effects.
- Attendance clock-out cannot occur before clock-in for the same attendance day.
- Attendance records must not be created for future dates.
- Attendance dates must be derived from the configured business timezone, not from client-provided local dates or unnamed server local time.
- Actual attendance clock-in and clock-out event timestamps must be retained in UTC.
- Missing or invalid business timezone configuration must prevent application startup with a clear configuration error.
- Attendance actions must not mutate vacation balances or vacation-request statuses.
- Dashboard counts must exclude soft-deleted employees from current workforce metrics and must include terminated-but-not-soft-deleted employees only where the metric explicitly concerns retained historical records.
- Document upload attempts for missing, soft-deleted, or unauthorized employee records must be rejected.
- Removing a document must not remove the employee record or unrelated documents.
- Employee document files must be stored outside public static folders unless access is explicitly protected.
- Employee document downloads must go through authorized system actions; users must not be able to download documents outside their permitted access.
- Employee document storage must use generated safe stored file names. Raw uploaded file names may be retained only as metadata.
- Document file type, file size, and storage path inputs must be validated so invalid files and path traversal attempts are rejected.
- Oversized document uploads must return `413 Payload Too Large` with the standard structured `{ code, message }` payload.
- If a file is saved but metadata persistence fails, the saved file must be cleaned up. If file saving fails, metadata must not be created.
- Removing a document must mark metadata as removed or soft-deleted, delete the physical file from backend-managed local storage, audit the removal, and prevent all later downloads for that document.
- Audit entries for sensitive changes must remain reviewable by authorized users while avoiding broad duplication of raw sensitive values.
- General audit logs must record changed field names and non-sensitive before/after values; sensitive values such as compensation amounts, document content, and protected storage details must be redacted or summarized.
- New Phase 7 rule failures must preserve the existing structured error response shape and must not rename existing compatibility error codes unless a separate compatibility phase approves it.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow an active authenticated employee to record one clock-in for the current attendance date.
- **FR-002**: The system MUST reject duplicate clock-in attempts for the same employee and attendance date.
- **FR-003**: The system MUST allow an active authenticated employee with an open attendance record to record a clock-out for the same attendance date.
- **FR-004**: The system MUST reject clock-out attempts when the employee has not clocked in for that attendance date.
- **FR-005**: The system MUST reject attendance actions for suspended, terminated, soft-deleted, missing, or unauthenticated employees.
- **FR-006**: The system MUST reject attendance records for future dates.
- **FR-007**: The system MUST calculate worked time only when both clock-in and clock-out are present and ordered correctly.
- **FR-007A**: The system MUST store actual attendance clock-in and clock-out event timestamps in UTC.
- **FR-007B**: The system MUST derive attendance business dates from a deployment-configured named business timezone.
- **FR-007C**: The system MUST NOT trust client-provided local dates or unnamed server local time to determine attendance business dates.
- **FR-007D**: The system MUST fail startup with a clear configuration error when the business timezone is missing or invalid.
- **FR-008**: The system MUST assign each employee exactly one current role: Employee, Manager, HR administrator, or System administrator.
- **FR-009**: The system MUST create the initial System administrator from secure startup/bootstrap configuration when no active System administrator exists.
- **FR-009A**: The system MUST invoke a clearly defined startup/bootstrap activation mechanism before administrative RBAC enforcement can lock out all administrators.
- **FR-009B**: Initial System administrator bootstrap MUST be idempotent or safely guarded against duplicate creation, skip without side effects when an active System administrator already exists, fail clearly for missing or invalid required configuration, duplicate configured employee number or email, invalid password, or missing configured department, and MUST NOT assign administrator rights to any fallback employee.
- **FR-009C**: Initial System administrator bootstrap MUST create the `ApplicationUser` and linked `Employee` in one transactional flow; if user creation, employee creation, role assignment, or audit writing fails, the system MUST roll back and leave no partial bootstrap records.
- **FR-009D**: Initial System administrator bootstrap MUST read sensitive values such as temporary password from configuration, environment variables, or user secrets; customer-specific employee numbers, email addresses, and passwords MUST NOT be hardcoded in source code.
- **FR-010**: The system MUST NOT assign HR administrator or system administrator permissions to all existing employees by default.
- **FR-011**: The system MUST deny protected actions when the signed-in employee's role does not allow the action.
- **FR-011A**: Phase 7 MUST NOT support multiple simultaneous employee roles, temporary elevated role grants, or permission union behavior.
- **FR-012**: The system MUST allow employees to view their own profile and record their own attendance.
- **FR-013**: The system MUST allow managers to view team members and perform team-scoped vacation review according to the reporting hierarchy.
- **FR-014**: The system MUST prevent managers from using manager permissions outside their reporting hierarchy.
- **FR-015**: The system MUST allow HR administrators and system administrators to view all employees, create and edit employees, manage departments, manage trips, view dashboard summaries, manage documents, view compensation, update compensation, and review audit history.
- **FR-016**: The system MUST deny compensation access to employees and managers unless they also hold an explicitly authorized HR administrator or system administrator role.
- **FR-017**: The system MUST store current compensation details for an employee, including base salary, currency, and last salary review date.
- **FR-018**: The system MUST keep compensation values out of normal employee profile and employee list results.
- **FR-019**: The system MUST record salary history whenever an accepted compensation change modifies salary, currency, or review date.
- **FR-020**: The system MUST avoid duplicate salary-history entries when an accepted compensation request does not change any compensation value.
- **FR-021**: The system MUST allow authorized users to upload employee documents into backend-managed local file storage while recording document category, original file name, generated safe stored file name, file size, uploader, and upload time as metadata.
- **FR-021A**: The system MUST NOT store employee document binary content directly in business records.
- **FR-021B**: The system MUST validate allowed document file types and maximum file size before accepting an upload.
- **FR-021C**: The system MUST prevent path traversal by ignoring unsafe uploaded names for storage paths and using generated safe stored file names.
- **FR-021D**: Oversized document uploads MUST return `413 Payload Too Large` with the standard structured `{ code, message }` payload.
- **FR-022**: The system MUST allow authorized users to list current documents for an employee.
- **FR-023**: The system MUST allow authorized users to retrieve current employee document content and metadata only through authorized system actions.
- **FR-024**: The system MUST allow authorized users to remove an employee document by marking metadata removed or soft-deleted, deleting the physical file from backend-managed local storage, auditing the removal, preventing later downloads, and preserving traceability of the removal.
- **FR-025**: The system MUST reject document operations for missing employees, soft-deleted employees, unauthorized users, and invalid document metadata.
- **FR-025A**: If document metadata persistence fails after a file is saved, the system MUST clean up the saved file; if file saving fails, the system MUST NOT create document metadata.
- **FR-026**: The system MUST provide a dashboard summary for authorized users showing total active employees, total departments, pending vacation requests, approved vacations this month, employees on vacation today, new hires this month, upcoming trips this week, employees per department, and vacation requests by status.
- **FR-027**: The system MUST scope dashboard metrics to the signed-in user's role and permissions.
- **FR-028**: The system MUST compute dashboard metrics from current HR records without requiring manual count entry.
- **FR-029**: The system MUST record audit entries for successful significant write actions, including employee, department, vacation, trip, attendance, compensation, document, and role-assignment changes.
- **FR-030**: Each audit entry MUST identify the affected record, action type, acting employee or documented system actor, action time, changed field names, and non-sensitive before/after values for authorized review.
- **FR-030A**: General audit entries MUST redact or summarize sensitive before/after values, including compensation amounts, document content, and protected document storage details.
- **FR-030B**: Initial System administrator bootstrap audit entries MUST use system actor marker `SYSTEM_BOOTSTRAP` and identify the affected employee, assigned role, employee number, email, and timestamp without recording temporary passwords, password hashes, tokens, security stamps, or cookies.
- **FR-031**: The system MUST allow authorized users to search audit entries by entity type, entity identifier, actor, action, and date range.
- **FR-032**: Audit-log results MUST be paginated.
- **FR-033**: The system MUST deny audit-log access to users without HR administrator or system administrator permission.
- **FR-034**: Phase 7 MUST preserve completed Phase 5 business rules for vacation overlap, vacation notice, vacation balance, self-approval prevention, same-status idempotency, employee termination, soft deletion, manager cycle prevention, duplicate email prevention, employee number immutability, trip ownership, trip date validation, and department employee counts.
- **FR-035**: Phase 7 MUST preserve Phase 6 ownership boundaries and MUST NOT move infrastructure-owned data, identity, document-storage, or audit responsibilities into the application layer.
- **FR-036**: New Phase 7 expected failures MUST use the existing structured error response shape with code and message fields and MUST preserve existing compatibility behavior unless a separate approved compatibility phase changes it.
- **FR-037**: Phase 7 MUST continue using cookie-based session authentication and MUST NOT introduce JWT, token refresh flows, or a new authentication mechanism.
- **FR-038**: Phase 7 MUST include a safe data-transition plan for new role, attendance, compensation, document, dashboard-supporting, and audit data while preserving existing records through safe defaults or explicit backfill rules.
- **FR-039**: Phase 7 MUST NOT introduce unrelated domains such as payroll processing, benefits enrollment, recruiting, performance reviews, time-off policy configuration, or external accounting integrations.

### Access Matrix

| Action | Employee | Manager | HR Administrator | System Administrator |
|--------|----------|---------|------------------|----------------------|
| View own profile | Allowed | Allowed | Allowed | Allowed |
| Record own attendance | Allowed | Allowed | Allowed | Allowed |
| View team members | Denied | Team only | Allowed | Allowed |
| View all employees | Denied | Denied | Allowed | Allowed |
| Create or edit employees | Denied | Denied | Allowed | Allowed |
| Approve or reject vacations | Denied | Team only | Allowed | Allowed |
| View all departments | Allowed | Allowed | Allowed | Allowed |
| Create or edit departments | Denied | Denied | Allowed | Allowed |
| Manage trips | Denied | Team only | Allowed | Allowed |
| View or edit compensation | Denied | Denied | Allowed | Allowed |
| Manage employee documents | Denied | Denied | Allowed | Allowed |
| View dashboard summary | Denied | Team-scoped | Allowed | Allowed |
| View audit logs | Denied | Denied | Allowed | Allowed |
| Assign roles | Denied | Denied | Denied | Allowed |

### Dashboard Metric Scope

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

Hidden or not applicable manager metrics must not expose organization-wide values.

### Key Entities *(include if feature involves data)*

- **Attendance Record**: A daily record for one employee with a business attendance date derived from the configured business timezone, UTC clock-in timestamp, optional UTC clock-out timestamp, optional notes, and creation/update tracking.
- **Employee Role Assignment**: The single current role level that determines what an authenticated employee may view or change.
- **Compensation Profile**: Sensitive employee compensation details, including base salary, currency, and last review date.
- **Salary History Entry**: A traceable compensation-change record with previous values, new values, actor, and change time.
- **Employee Document**: A document associated with an employee, including category, original file name, generated safe stored file name, file size, backend-managed local storage location information, uploader, upload time, and removal state.
- **Dashboard Summary**: A read model of current HR metrics derived from employees, departments, vacations, and trips according to the requesting user's permissions.
- **Audit Log Entry**: A trace record for a significant successful write action, including affected record, action type, human actor or documented system actor marker, time, changed field names, non-sensitive before/after details, and redacted or summarized sensitive details.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested active employees can clock in once and clock out once for a valid attendance day, and 100% of duplicate clock-in or clock-out-before-clock-in attempts are rejected.
- **SC-002**: 100% of tested suspended, terminated, soft-deleted, missing, or unauthenticated employees are denied attendance actions.
- **SC-002A**: 100% of attendance-date tests produce deterministic results when the business timezone is pinned, including cases near midnight UTC and local day boundaries.
- **SC-003**: 100% of representative access-matrix checks allow permitted actions and deny forbidden actions for Employee, Manager, HR administrator, and System administrator roles.
- **SC-004**: When no active System administrator exists and bootstrap configuration is valid, startup creates exactly one initial System administrator; ordinary existing employees do not receive broad administrative privileges by default.
- **SC-004A**: Bootstrap startup validation proves the system cannot lock out all administrators before bootstrap succeeds, invalid bootstrap configuration assigns no fallback administrator, and repeated startup does not create duplicate administrators.
- **SC-005**: 100% of tested normal employee profile and list results exclude compensation values.
- **SC-006**: 100% of accepted compensation changes that alter a value create a salary-history entry, and 100% of no-change compensation submissions avoid duplicate history entries.
- **SC-007**: 100% of tested authorized document uploads, lists, retrievals, and removals work for valid employees, removed documents cannot be downloaded, and unauthorized or invalid document operations are rejected.
- **SC-008**: Dashboard summary counts match prepared source records in 100% of representative workforce, department, vacation, and trip test cases according to the metric-by-metric role scope table.
- **SC-009**: 100% of representative successful write actions create audit entries that identify actor or system actor marker, affected record, action type, action time, changed field names, non-sensitive before/after values, and redacted or summarized sensitive changes.
- **SC-010**: Audit-log search returns matching paginated entries for entity type, entity identifier, actor, action, and date range in 100% of representative test cases.
- **SC-011**: Existing Phase 5 and Phase 6 regression checks continue to pass after Phase 7 changes.
- **SC-012**: Existing client-facing error responses for expected failures continue to provide structured code and message fields for 100% of tested Phase 7 validation, authorization, and conflict failures.

## Assumptions

- Phases 0 through 6 are complete before Phase 7 implementation begins.
- Phase 7 uses the existing employee identity and cookie-based session model.
- The development deployment may use `Africa/Cairo` as its business timezone, but each deployment can change the configured timezone without code changes.
- Existing active employees begin with the lowest Employee role unless explicitly promoted through an approved initial role-assignment strategy.
- Each employee has exactly one current role in Phase 7.
- The primary first-run path creates the initial System administrator from secure `InitialAdminBootstrap` configuration when no active System administrator exists.
- Because the current employee model requires a department, initial admin creation requires a configured existing `DepartmentId`; bootstrap fails clearly if that department is missing.
- `ForcePasswordChange` is accepted in bootstrap configuration, but first-login password-change enforcement requires separate support if the current Identity model does not already provide it without schema changes.
- Managers are determined by the existing employee manager relationship and inherited reporting chain.
- Team-scoped manager permissions apply only to direct and indirect reports, not to peers, the manager's own manager, or unrelated departments.
- Compensation defaults to no recorded salary until an authorized user enters compensation data.
- Salary currency defaults should follow the deployment's current HR policy, with EGP expected for the current local project unless planning documents approve a different default.
- Employee documents in Phase 7 are HR-managed records, not a public employee self-service upload portal.
- Document files are stored in backend-managed local storage outside public static folders unless access is explicitly protected.
- Document removal means metadata is marked removed or soft-deleted, the physical file is deleted from backend-managed local storage, future downloads are unavailable, and the removal is audited.
- Dashboard summary is read-only and does not replace detailed employee, department, vacation, trip, attendance, compensation, document, or audit views.
- Audit logging records successful business changes. Security monitoring for failed login attempts or intrusion detection is outside Phase 7 unless separately approved.
- Detailed compensation records and document records remain the authorized sources for sensitive values; general audit logs provide traceability without duplicating raw sensitive values.
- Payroll processing, benefit deductions, tax calculations, payslip generation, recruiting, performance reviews, policy configuration, external storage integrations, and external accounting integrations remain outside Phase 7.
