# Feature Specification: Phase 9 - Employee Access Scope Hardening

**Feature Branch**: `[009-employee-access-scope-hardening]`

**Created**: 2026-06-13

**Status**: Draft

**Input**: User description: "create specifications for phase 9."

## Boundary Definitions and Scope

- **Employee access scope hardening**: The business-rule change that prevents authenticated users from seeing or managing employee records outside their approved role and scope.
- **Employee profile data**: Employee list and detail information, including identity, department, manager, contact, employment status, soft-delete, termination, and HR notes fields currently returned by employee endpoints.
- **Self access**: A signed-in employee accessing their own employee profile.
- **Team access**: A manager accessing direct and indirect reports according to the completed Phase 8 team-scope decision.
- **Organization access**: HR administrators and system administrators accessing employee records across the organization.
- **Employee management operation**: Creating, updating, or deleting employee records through employee-management endpoints. Role assignment is a separate operation that remains system-administrator-only and is also subject to the last-active-SystemAdministrator guard when demoting an active system administrator.

Phase 9 is limited to employee endpoint access and visibility. It must not change vacation request scope, trip ownership, Swagger documentation, authentication design, database schema, compensation/document rules, or frontend behavior.

## Clarifications

### Session 2026-06-13

- Q: Should Phase 9 include employee update and delete hardening, or only the roadmap-minimum list/detail/create/role-assignment protection? -> A: Include update and delete hardening in Phase 9.
- Q: Should HR administrators be allowed to update or delete SystemAdministrator employee records? -> A: HR administrators can create, update, and delete non-SystemAdministrator employees only; only system administrators can update or delete SystemAdministrator employee records.
- Q: Should HR/System organization-wide employee list and detail access include terminated and soft-deleted employee records? -> A: HR/System list and detail include existing active, suspended, terminated, and soft-deleted employee records, subject to existing status, page, and pageSize filters.
- Q: Should Phase 9 prevent update/delete/status changes that would leave zero active SystemAdministrators? -> A: Reject any update, delete, or status change that would leave zero active SystemAdministrators.
- Q: Should the last-active-SystemAdministrator guard include role assignment/demotion? -> A: Yes. Any operation that would leave zero active SystemAdministrators must be rejected before mutation, including role assignment that demotes the last active SystemAdministrator to any other role.

## Existing Context Findings

- Phase 8 established reusable self, team, and organization scope decisions.
- Phase 8 chose direct plus indirect reports as manager team scope.
- The current employee list, detail, create, update, and delete endpoints are authenticated endpoints.
- The current role-assignment endpoint is already restricted to `SystemAdministrator` and must remain so.
- `/api/auth/me` already exists as the approved current-user endpoint for signed-in users.
- Employee list/detail responses include internal HR profile data and must not be exposed broadly to normal employees.
- The existing structured error response shape is `{ code, message }`.
- Existing routes and response DTOs should remain stable unless a narrower response is required to prevent unauthorized data exposure.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Block Broad Employee Directory Access (Priority: P1)

As a normal employee, I must not be able to list all employee profiles so that private HR profile data is not exposed to the whole workforce.

**Why this priority**: Manual testing found that an authenticated normal employee can call the employee list endpoint and see all employees. This is the primary privacy and authorization gap Phase 9 must close.

**Independent Test**: Can be tested by logging in as a normal employee and requesting the employee list; the request is rejected with a forbidden response and no employee list payload is returned.

**Acceptance Scenarios**:

1. **Given** a signed-in normal employee, **When** they request `GET /api/employees`, **Then** the system returns `403 Forbidden` with the standard structured error shape.
2. **Given** a signed-in normal employee, **When** they need their own current-user identity, **Then** they can still use `/api/auth/me`.
3. **Given** a signed-in normal employee, **When** they request another employee's detail record, **Then** access is denied and no other employee profile data is returned.

---

### User Story 2 - Allow Managers to See Only Their Team (Priority: P1)

As a manager, I need to view employees in my direct and indirect reporting chain but not peers, unrelated employees, or organization-wide records.

**Why this priority**: Managers need operational team visibility, but broader access leaks HR data outside their responsibility.

**Independent Test**: Can be tested with a reporting hierarchy containing a manager, direct report, indirect report, peer, unrelated employee, soft-deleted report, and terminated report.

**Acceptance Scenarios**:

1. **Given** a signed-in manager with active direct and indirect reports, **When** they request `GET /api/employees`, **Then** the returned list contains only active direct and indirect reports within the requested status, page, and pageSize filters.
2. **Given** a signed-in manager, **When** they request their own employee detail, **Then** the request succeeds.
3. **Given** a signed-in manager, **When** they request a direct or indirect report detail, **Then** the request succeeds.
4. **Given** a signed-in manager, **When** they request a peer, unrelated employee, soft-deleted report, or terminated report detail, **Then** the system returns `403 Forbidden` and no out-of-scope employee profile data is returned.
5. **Given** a signed-in manager with no team members, **When** they request the employee list, **Then** the request succeeds with an empty list rather than exposing organization-wide data.

---

### User Story 3 - Preserve HR and System Administrator Organization Access (Priority: P1)

As an HR administrator or system administrator, I need organization-wide employee access so that HR operations can continue without losing valid administrative capabilities.

**Why this priority**: The hardening must stop overbroad employee access without breaking legitimate HR administration workflows.

**Independent Test**: Can be tested by logging in as HR administrator and system administrator users and verifying they can list, view, and create employee records according to existing business rules.

**Acceptance Scenarios**:

1. **Given** a signed-in HR administrator, **When** they request the employee list, **Then** they receive organization-wide active, suspended, terminated, and soft-deleted employee results according to the existing status, page, and pageSize filters.
2. **Given** a signed-in system administrator, **When** they request the employee list, **Then** they receive organization-wide active, suspended, terminated, and soft-deleted employee results according to the existing status, page, and pageSize filters.
3. **Given** a signed-in HR administrator or system administrator, **When** they request any existing employee detail including terminated or soft-deleted records, **Then** the request succeeds unless the employee does not exist.
4. **Given** a signed-in HR administrator or system administrator, **When** they create an employee with data that passes existing employee business rules, **Then** employee creation follows the existing response shape.

---

### User Story 4 - Restrict Employee Management Writes (Priority: P2)

As an HR system owner, I need employee management writes to be limited to HR administrators and system administrators so that normal employees and managers cannot create, update, or delete employee records.

**Why this priority**: Employee record changes are HR management actions. Broad write access would allow unauthorized changes even if read access is fixed.

**Independent Test**: Can be tested by attempting employee create, update, delete, and role assignment operations as each role and verifying only HR administrator and system administrator users can perform permitted writes, role assignment remains system-administrator-only, and role demotion cannot remove the last active system administrator.

**Acceptance Scenarios**:

1. **Given** a signed-in normal employee, **When** they try to create, update, or delete an employee record, **Then** the system returns `403 Forbidden`.
2. **Given** a signed-in manager, **When** they try to create, update, or delete an employee record, **Then** the system returns `403 Forbidden`.
3. **Given** a signed-in HR administrator, **When** they create, update, or delete a non-SystemAdministrator employee record, **Then** the operation is allowed only if all existing employee business rules pass.
4. **Given** a signed-in HR administrator, **When** they try to update or delete a SystemAdministrator employee record, **Then** the system returns `403 Forbidden`.
5. **Given** a signed-in system administrator, **When** they create, update, or delete an employee record, **Then** the operation is allowed only if all existing employee business rules pass.
6. **Given** any user other than a system administrator, **When** they try to assign or change an employee role, **Then** the operation remains forbidden.
7. **Given** only one active system administrator remains, **When** any permitted user tries to update, delete, terminate, change status, or demote that employee's role in a way that would leave zero active system administrators, **Then** the system rejects the operation before state mutation.

### Edge Cases

- Authenticated user has no valid employee ID in the session.
- Requesting employee is soft-deleted or terminated after login.
- Requesting employee is suspended; Phase 9 must preserve the Phase 8 decision unless an existing employee-management rule already forbids the action.
- Target employee does not exist.
- Target employee exists but is outside the requester's allowed scope.
- Manager has no active direct or indirect reports.
- Manager has direct and indirect reports across departments.
- Manager has soft-deleted or terminated reports.
- HR administrator or system administrator lists or views terminated and soft-deleted employee records.
- Employee list uses a status filter that would otherwise match out-of-scope employees.
- Pagination is requested after scope filtering.
- Role assignment is attempted by HR administrator, manager, or normal employee.
- HR administrator attempts to update or delete a SystemAdministrator employee record.
- A permitted update, delete, termination, status change, or role demotion would leave zero active SystemAdministrators.
- The only active SystemAdministrator is targeted by `PUT /api/employees/{id}/role` with a non-SystemAdministrator role.
- A SystemAdministrator role demotion is attempted when another active SystemAdministrator remains.
- Employee create/update/delete requests fail existing business rules such as duplicate email, duplicate employee number, manager-cycle prevention, status rules, or employee-number immutability.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST use the completed Phase 8 self, team, and organization scope definitions for employee access decisions.
- **FR-002**: `GET /api/employees` MUST reject normal `Employee` role users with `403 Forbidden`; it MUST NOT return a self-only employee list.
- **FR-003**: `GET /api/employees` MUST return only active direct and indirect reports for `Manager` role users, applying the existing status, page, and pageSize filters within that scoped set.
- **FR-004**: `GET /api/employees` MUST return organization-wide active, suspended, terminated, and soft-deleted employee results for `HRAdministrator` and `SystemAdministrator` role users according to the existing status, page, and pageSize filters.
- **FR-005**: Employee list responses MUST NOT include employees outside the requester's permitted scope.
- **FR-006**: A manager with no visible team employees MUST receive a successful empty paged result, not organization-wide fallback data.
- **FR-007**: `GET /api/employees/{id}` MUST allow normal employees to view their own detail record.
- **FR-008**: `GET /api/employees/{id}` MUST deny normal employees from viewing any other employee detail record.
- **FR-009**: `GET /api/employees/{id}` MUST allow managers to view their own detail record and active direct or indirect report detail records.
- **FR-010**: `GET /api/employees/{id}` MUST deny managers from viewing peers, unrelated employees, the manager's own manager, soft-deleted reports, and terminated reports unless a later approved historical-record rule changes this.
- **FR-011**: `GET /api/employees/{id}` MUST allow HR administrators and system administrators to view any existing employee detail record, including terminated and soft-deleted records.
- **FR-012**: Employee detail requests for a missing employee ID MUST return the existing not-found behavior.
- **FR-013**: Employee detail requests for an existing but out-of-scope employee MUST return `403 Forbidden` with the standard structured error shape.
- **FR-014**: Employee create, update, and delete operations MUST be allowed only for HR administrators and system administrators.
- **FR-015**: Employee create, update, and delete operations attempted by normal employees or managers MUST return `403 Forbidden` before any state mutation.
- **FR-016**: HR administrators MUST be allowed to create, update, and delete only non-SystemAdministrator employee records, subject to all existing employee business rules.
- **FR-017**: HR administrators MUST receive `403 Forbidden` when attempting to update or delete a SystemAdministrator employee record.
- **FR-018**: System administrators MUST be allowed to update or delete SystemAdministrator employee records, subject to all existing employee business rules.
- **FR-019**: Role assignment MUST remain restricted to system administrators only.
- **FR-020**: HR administrators MUST NOT be allowed to assign roles unless a future approved phase changes this rule.
- **FR-021**: Existing employee business rules MUST remain active, including duplicate email prevention, duplicate employee number prevention, employee-number immutability, manager-cycle prevention, soft deletion, status rules, same-status idempotent behavior, and structured business-rule failures.
- **FR-022**: Phase 9 MUST preserve existing employee routes, successful response shapes, pagination envelope shape, authentication cookies, claims, status-code conventions, and error response shape except for the new forbidden outcomes required by access hardening.
- **FR-023**: Phase 9 MUST NOT add a public employee directory or a new employee self-service edit workflow.
- **FR-024**: Phase 9 MUST NOT change vacation request behavior, trip behavior, compensation behavior, document behavior, dashboard behavior, audit behavior, bootstrap behavior, or Swagger response documentation.
- **FR-025**: Phase 9 MUST NOT create a database migration unless a later approved plan proves that current employee role and manager relationship data cannot enforce the required access rules.
- **FR-026**: Phase 9 MUST update lifecycle or manual testing documentation if it currently states or implies that any authenticated user can list, create, update, delete, or view organization-wide employee records.
- **FR-027**: Phase 9 MUST include role-and-scope validation covering normal employee, manager, HR administrator, and system administrator behavior for employee list, detail, create, update, delete, protected SystemAdministrator records, and role assignment.
- **FR-028**: Employee update, delete/soft-delete, termination, status-change, and role assignment/demotion operations MUST be rejected before state mutation when the operation would leave zero active SystemAdministrator employees.

### Required Access Matrix

| Operation | Employee | Manager | HRAdministrator | SystemAdministrator |
|-----------|----------|---------|-----------------|---------------------|
| `GET /api/employees` | `403 Forbidden` | Direct and indirect active reports only | Organization-wide, including terminated and soft-deleted records | Organization-wide, including terminated and soft-deleted records |
| `GET /api/employees/{id}` | Self only | Self plus direct and indirect active reports | Any existing employee, including terminated and soft-deleted records | Any existing employee, including terminated and soft-deleted records |
| `POST /api/employees` | `403 Forbidden` | `403 Forbidden` | Allowed | Allowed |
| `PUT /api/employees/{id}` | `403 Forbidden` | `403 Forbidden` | Non-SystemAdministrator targets only | Allowed, except last-active-SystemAdministrator removal |
| `DELETE /api/employees/{id}` | `403 Forbidden` | `403 Forbidden` | Non-SystemAdministrator targets only | Allowed, except last-active-SystemAdministrator removal |
| `PUT /api/employees/{id}/role` | `403 Forbidden` | `403 Forbidden` | `403 Forbidden` | Allowed, except last-active-SystemAdministrator demotion |

### Out of Scope

- Vacation request visibility, ownership, on-behalf creation, or review authorization. This belongs to Phase 10.
- Trip visibility, ownership, requester/traveler distinction, or trip creation authorization. This belongs to Phase 11.
- Lifecycle documentation and full manual retest beyond employee-access wording updates. This belongs to Phase 12.
- Swagger/OpenAPI response documentation pass. This belongs to Phase 13.
- Authentication redesign, JWT, token refresh, SSO, or public setup endpoints.
- New role names, multi-role employees, temporary grants, permission unions, or public employee directory features.
- Employee self-service profile editing unless a future approved phase defines it.
- Compensation, document, dashboard, attendance, audit-log, or bootstrap behavior changes.

### Key Entities *(include if feature involves data)*

- **Requesting Employee**: The signed-in employee whose role and scope determine access.
- **Target Employee**: The employee record being viewed or managed.
- **Employee List Result**: A paged employee-profile result constrained by the requester's role and scope.
- **Manager Team Scope**: Direct and indirect active reports under a manager according to Phase 8.
- **Employee Management Operation**: Create, update, or delete employee records, allowed only for HR administrators and system administrators.
- **Role Assignment Operation**: Change an employee role, allowed only for system administrators and rejected before mutation when demoting the last active SystemAdministrator.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of normal-employee list attempts return `403 Forbidden` and no employee list payload.
- **SC-002**: 100% of normal-employee detail checks allow self detail and deny other employee detail.
- **SC-003**: 100% of manager list checks contain zero peers, unrelated employees, soft-deleted reports, or terminated reports.
- **SC-004**: 100% of manager detail checks allow self, direct reports, and indirect reports while denying outside-scope employees.
- **SC-005**: 100% of HR administrator and system administrator employee list/detail/create/update/delete checks preserve valid organization-wide management access, including terminated and soft-deleted list/detail visibility, while denying HR administrator update/delete attempts against SystemAdministrator records.
- **SC-006**: 100% of role-assignment checks continue to allow only system administrators, reject HR administrator role assignment, reject last-active-SystemAdministrator demotion before mutation, and allow SystemAdministrator demotion only when at least one other active SystemAdministrator remains and existing role-assignment rules pass.
- **SC-007**: 100% of tested employee create, update, and delete attempts by normal employees and managers return `403 Forbidden` before state mutation.
- **SC-008**: Existing pagination, successful employee response shapes, structured error response shape, cookie behavior, and auth claims remain compatible outside the new access denials.
- **SC-009**: Phase 9 completes with zero database migrations and zero vacation, trip, compensation, document, dashboard, audit, bootstrap, or Swagger behavior changes.
- **SC-010**: Lifecycle or manual testing documentation no longer describes employee list/create/update/delete access as available to any authenticated user.
- **SC-011**: 100% of tested operations that would leave zero active system administrators, including role demotion, are rejected before state mutation.

## Assumptions

- Phase 8 authorization scope foundation is complete and available before Phase 9 implementation.
- Manager team scope means direct plus indirect active reports, matching the Phase 8 decision.
- Normal employees use `/api/auth/me` for current-user identity and may use employee detail only for their own profile.
- Authenticated out-of-scope employee access returns `403 Forbidden`; missing employee IDs continue to return the existing not-found response.
- Employee list scope filtering happens before pagination so pages do not leak counts or items from outside the requester's scope.
- Organization-wide employee access includes active, suspended, terminated, and soft-deleted employee records for HR administrator and system administrator users.
- Existing employee business rules and DTO shapes remain the source of truth for what a permitted management operation may change.
- SystemAdministrator employee records require system-administrator authority for update and delete operations.
- The system must keep at least one active SystemAdministrator employee after any employee update, delete/soft-delete, termination, status-change, or role assignment/demotion operation.
- No schema change is expected because employee roles and manager relationships already exist.
