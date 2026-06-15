# Feature Specification: Phase 10 - Vacation Request Scope Hardening

**Feature Branch**: `[010-vacation-scope-hardening]`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "Read the existing project context and create a specification for phase 10."

## Boundary Definitions and Scope

- **Vacation request scope hardening**: The business-rule change that prevents authenticated users from seeing, creating, deleting, or reviewing vacation requests outside their approved role and scope.
- **Vacation request owner**: The employee whose leave balance and absence period are affected by a vacation request. This is represented by the vacation request employee reference.
- **Vacation request creator**: The authenticated employee who creates the request, including HR administrators or system administrators creating a request on behalf of another employee.
- **Vacation reviewer**: The authenticated employee who approves or rejects a pending vacation request.
- **Self scope**: A signed-in employee accessing or creating a vacation request for their own employee record.
- **Team scope**: A manager accessing or reviewing vacation requests owned by active direct and indirect reports according to the completed Phase 8 team-scope decision.
- **Organization scope**: HR administrators and system administrators accessing vacation requests across the organization.
- **On-behalf creation**: HR administrators and system administrators creating a vacation request whose owner is another employee.

Phase 10 is limited to vacation request visibility, ownership, creation, review authorization, delete authorization, and creator tracking analysis. It must not change employee endpoint hardening, trip ownership, Swagger documentation, authentication design, database schema without explicit approval, compensation, documents, attendance, dashboard, audit, or frontend behavior.

## Clarifications

### Session 2026-06-15

- Q: Should Phase 10 treat persisted vacation request creator tracking as required, migration-gated work? -> A: Require `CreatedByEmployeeId` tracking, but only via a separately approved migration plan; existing rows may keep null unless a reliable source exists, and new rows must record creator after the approved migration.
- Q: Should `GET /api/vacationrequests` for a Manager include the manager's own vacation requests by default? -> A: No. Manager list returns active direct/indirect team requests only; manager self requests are accessible by detail and creation flows, not the default manager list.
- Q: How should `GET /api/vacationrequests?employeeId={managerId}` behave for a Manager requesting their own employee ID? -> A: Allow self-filter. Manager may list own vacation requests only when `employeeId` equals the manager's own ID; unfiltered manager list remains team-only.
- Q: For vacation operations where the target exists but is outside the requester's allowed scope, should scope authorization run before vacation business-rule validation? -> A: Yes. Return `403 Forbidden` for existing out-of-scope targets before checking pending status, transition validity, overlap, balance, or other domain rules.

## Existing Context Findings

- Phase 8 established reusable self, team, and organization scope decisions.
- Phase 8 chose direct plus indirect reports as manager team scope.
- Phase 9 hardened employee access and established the missing-vs-out-of-scope convention for scoped employee records.
- The current vacation request list, detail, create, and delete service paths are authenticated but do not receive the current requester context for scope decisions.
- The current status update path receives the reviewer employee ID and already blocks self-review, but it still needs role and team-scope authorization.
- The current vacation request entity tracks the owner employee and reviewer employee, but it does not track a separate creator employee.
- The current vacation request create request identifies the target employee. Phase 10 must enforce whether the requester may target that employee.
- The current delete behavior allows deleting only pending vacation requests. Phase 10 must preserve that business rule while adding role and ownership scope.
- Existing vacation validation rules include date order, future date, notice period, balance availability, overlap checks, transition rules, and balance deduction/refund behavior.
- Existing structured error responses use the standard `{ code, message }` shape.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Employee Own Vacation Privacy (Priority: P1)

As a normal employee, I need to see and manage only my own vacation requests so that private leave information for other employees is not exposed.

**Why this priority**: Manual testing showed that normal employees can see all vacation requests. This is the core privacy and authorization gap Phase 10 must close.

**Independent Test**: Can be tested by logging in as a normal employee and attempting to list, view, create, delete, and review vacation requests for self and other employees.

**Acceptance Scenarios**:

1. **Given** a signed-in normal employee with their own vacation requests and other employees' requests, **When** they request `GET /api/vacationrequests`, **Then** only their own vacation requests are returned.
2. **Given** a signed-in normal employee, **When** they request their own vacation request detail, **Then** the request succeeds.
3. **Given** a signed-in normal employee, **When** they request another employee's vacation request detail, **Then** the system returns `403 Forbidden` and no out-of-scope vacation data is returned.
4. **Given** a signed-in normal employee, **When** they create a vacation request for themselves and all existing validation rules pass, **Then** the request is created as their own vacation request.
5. **Given** a signed-in normal employee, **When** they attempt to create a vacation request for another employee, **Then** the system rejects the request before creating vacation data.
6. **Given** a signed-in normal employee, **When** they attempt to review any vacation request, **Then** the system returns `403 Forbidden`.

---

### User Story 2 - Manager Team Vacation Review (Priority: P1)

As a manager, I need to see and review vacation requests for employees in my team while being prevented from accessing peers, unrelated employees, or my own requests for review.

**Why this priority**: Managers need team-operational leave visibility and review authority, but broader access leaks private HR data and self-review creates an approval conflict.

**Independent Test**: Can be tested with a hierarchy containing a manager, direct report, indirect report, peer, unrelated employee, soft-deleted report, terminated report, and the manager's own vacation request.

**Acceptance Scenarios**:

1. **Given** a signed-in manager with active direct and indirect reports, **When** they request `GET /api/vacationrequests`, **Then** only team-owned vacation requests are returned.
2. **Given** a signed-in manager, **When** they request detail for their own vacation request or an active direct/indirect report's vacation request, **Then** the request succeeds.
3. **Given** a signed-in manager, **When** they request detail for a peer, unrelated employee, soft-deleted report, or terminated report vacation request, **Then** the system returns `403 Forbidden`.
4. **Given** a signed-in manager, **When** they create a vacation request, **Then** they may create only for themselves.
5. **Given** a signed-in manager, **When** they attempt to create a vacation request for a team member or unrelated employee, **Then** the system rejects the request before creation.
6. **Given** a signed-in manager, **When** they approve or reject an active team member's pending request and all existing status rules pass, **Then** the review succeeds.
7. **Given** a signed-in manager, **When** they attempt to approve or reject their own vacation request, **Then** the system rejects self-review.

---

### User Story 3 - HR and System On-Behalf Vacation Operations (Priority: P1)

As an HR administrator or system administrator, I need organization-wide vacation visibility and the ability to create vacation requests on behalf of employees so that HR operations can continue under explicit audit-friendly ownership rules.

**Why this priority**: The hardening must stop overbroad employee and manager access without breaking legitimate HR administration workflows.

**Independent Test**: Can be tested by logging in as HR administrator and system administrator users and verifying list, detail, creation, review, and delete behavior across employees.

**Acceptance Scenarios**:

1. **Given** a signed-in HR administrator or system administrator, **When** they request `GET /api/vacationrequests`, **Then** organization-wide vacation requests are returned according to existing status, employee, page, and pageSize filters.
2. **Given** a signed-in HR administrator or system administrator, **When** they request any existing vacation request detail, **Then** the request succeeds unless the request does not exist.
3. **Given** a signed-in HR administrator or system administrator, **When** they create a vacation request for any active employee and all existing validation rules pass, **Then** the request is created for the target employee and records the authenticated creator after approved creator-tracking storage is available.
4. **Given** a signed-in HR administrator or system administrator, **When** they attempt to review their own vacation request, **Then** the system rejects self-review.
5. **Given** a signed-in HR administrator or system administrator, **When** they review any other employee's pending request and all existing status rules pass, **Then** the review succeeds.

---

### User Story 4 - Scoped Pending Vacation Deletion (Priority: P2)

As a vacation request owner or administrator, I need deletion to remain limited to pending requests and to respect role scope so that users cannot delete or probe vacation records outside their authority.

**Why this priority**: Deletion is less frequent than listing, creation, and review, but it mutates business records and must not bypass ownership or administrator scope.

**Independent Test**: Can be tested by attempting pending and non-pending vacation deletion as Employee, Manager, HRAdministrator, and SystemAdministrator users.

**Acceptance Scenarios**:

1. **Given** a signed-in normal employee, **When** they delete their own pending vacation request, **Then** the request is deleted according to the existing delete behavior.
2. **Given** a signed-in normal employee, **When** they delete another employee's vacation request, **Then** the system returns `403 Forbidden`.
3. **Given** a signed-in manager, **When** they delete their own pending vacation request, **Then** the request is deleted according to the existing delete behavior.
4. **Given** a signed-in manager, **When** they attempt to delete a team member's vacation request, **Then** the system rejects the operation unless a future approved business rule explicitly grants manager team deletion.
5. **Given** a signed-in HR administrator or system administrator, **When** they delete any pending vacation request, **Then** the request is deleted according to the existing delete behavior.
6. **Given** any signed-in user with delete scope, **When** they delete a non-pending vacation request, **Then** the system rejects the request using the existing business-rule behavior.

### Edge Cases

- Requesting user has no valid employee ID claim or no current employee context.
- Requesting employee is missing, soft-deleted, or terminated after login.
- Requesting employee is suspended; Phase 10 must preserve the Phase 8 decision unless an existing vacation-specific rule already forbids the action.
- Target vacation request does not exist.
- Target vacation request exists but is outside the requester's permitted scope.
- Existing out-of-scope vacation targets must return `403 Forbidden` before revealing pending status, transition validity, overlap, balance, notice, or other vacation business-rule details.
- Manager has no active direct or indirect reports.
- Manager has only soft-deleted or terminated reports.
- Manager attempts to list, view, create, delete, approve, or reject a request for a peer, unrelated employee, soft-deleted report, or terminated report.
- Manager attempts to create a request for a direct or indirect report.
- HR administrator or system administrator creates a request for another employee while the target employee is inactive, soft-deleted, terminated, or otherwise ineligible under existing vacation rules.
- Any role attempts to approve or reject their own vacation request.
- Same-status vacation review is requested; the existing Phase 5 idempotent no-op success behavior must remain consistent.
- Vacation list uses status and employee filters that would otherwise match out-of-scope requests.
- Vacation list receives an `employeeId`, requester, manager, or target-style filter outside the authenticated user's allowed scope; the system must return an empty scoped page/list, preserve the normal pagination shape, and avoid revealing whether the out-of-scope employee exists.
- Pagination is requested after scope filtering.
- Vacation create request fails existing business rules such as date order, past start date, notice period, overlap, insufficient balance, or invalid target employee.
- Vacation delete targets a pending request, approved request, rejected request, or already-deleted request.
- Creator tracking is not present in the current schema, requiring an explicit migration/backfill strategy before any database schema change is approved.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST use the completed Phase 8 self, team, and organization scope definitions for vacation request access decisions.
- **FR-002**: Vacation request list, detail, create, review, and delete operations MUST evaluate the authenticated requester before granting self, team, or organization scope.
- **FR-002A**: For existing targets outside the requester's allowed scope, vacation operations MUST return `403 Forbidden` before evaluating pending status, transition validity, target eligibility, overlap, balance, notice period, or other vacation domain rules.
- **FR-003**: `GET /api/vacationrequests` MUST return only own vacation requests for `Employee` role users.
- **FR-004**: `GET /api/vacationrequests` MUST return only active direct and indirect team-owned vacation requests for `Manager` role users; it MUST NOT include the manager's own vacation requests by default.
- **FR-005**: `GET /api/vacationrequests` MUST return organization-wide vacation requests for `HRAdministrator` and `SystemAdministrator` role users.
- **FR-006**: Existing list filters, including status, employee, page, and pageSize, MUST be applied within the requester's allowed scope and MUST NOT broaden access. As a manager-specific exception, `employeeId` equal to the manager's own employee ID MUST return only the manager's own vacation requests, while the unfiltered manager list remains team-only.
- **FR-006A**: When a scoped vacation list receives an `employeeId`, manager, requester, or target-style filter outside the authenticated user's allowed scope, the system MUST return an empty scoped page/list, preserve the normal pagination shape, MUST NOT reveal whether the out-of-scope employee exists, and MUST NOT return unauthorized employees.
- **FR-007**: Pagination MUST be applied after scope filtering so result pages and counts do not leak out-of-scope vacation data.
- **FR-008**: `GET /api/vacationrequests/{id}` MUST allow employees to view only their own vacation request detail.
- **FR-009**: `GET /api/vacationrequests/{id}` MUST allow managers to view their own vacation request detail and active direct or indirect report vacation request detail.
- **FR-010**: `GET /api/vacationrequests/{id}` MUST allow HR administrators and system administrators to view any existing vacation request detail.
- **FR-011**: Vacation request detail for a missing request ID MUST return the existing not-found behavior.
- **FR-012**: Vacation request detail for an existing but out-of-scope request MUST return `403 Forbidden` with the standard structured error shape.
- **FR-013**: `POST /api/vacationrequests` MUST allow employees to create vacation requests only for themselves.
- **FR-014**: `POST /api/vacationrequests` MUST allow managers to create vacation requests only for themselves; managers MUST NOT create vacation requests for team members in Phase 10.
- **FR-015**: `POST /api/vacationrequests` MUST allow HR administrators and system administrators to create vacation requests for any employee who passes existing vacation eligibility rules.
- **FR-016**: Vacation creation MUST preserve existing validation rules for target employee eligibility, date order, future start date, notice period, overlap, working day count, and balance availability.
- **FR-017**: Vacation creation MUST reject attempts to target a missing, inactive, soft-deleted, terminated, or otherwise ineligible employee according to existing vacation business rules.
- **FR-018**: Vacation creation MUST record the authenticated creator for new vacation requests after approved creator-tracking storage is available.
- **FR-019**: If the current data model cannot store a separate vacation request creator, Phase 10 planning MUST document a migration strategy before any migration is created.
- **FR-020**: Any creator-tracking migration strategy MUST avoid inventing verified historical creator data for existing vacation rows; existing rows may keep null creator metadata unless a reliable source exists.
- **FR-021**: Any creator-tracking migration strategy MUST make new vacation requests record a creator after the approved migration, while handling existing null creator values safely in queries and responses.
- **FR-022**: `PUT /api/vacationrequests/{id}/status` MUST return `403 Forbidden` for normal employee role users.
- **FR-023**: `PUT /api/vacationrequests/{id}/status` MUST allow managers to approve or reject only active direct or indirect team-owned vacation requests.
- **FR-024**: `PUT /api/vacationrequests/{id}/status` MUST allow HR administrators and system administrators to approve or reject any request except their own.
- **FR-025**: Self-review MUST be forbidden for all roles, including Manager, HRAdministrator, and SystemAdministrator.
- **FR-026**: Vacation status updates MUST preserve existing transition, same-status idempotent no-op, timestamp, review metadata, balance deduction, and balance refund behavior.
- **FR-027**: `DELETE /api/vacationrequests/{id}` MUST preserve the existing pending-only delete rule.
- **FR-028**: `DELETE /api/vacationrequests/{id}` MUST allow employees to delete only their own pending vacation requests.
- **FR-029**: `DELETE /api/vacationrequests/{id}` MUST allow managers to delete only their own pending vacation requests; managers MUST NOT delete team member vacation requests unless a future approved rule grants that capability.
- **FR-030**: `DELETE /api/vacationrequests/{id}` MUST allow HR administrators and system administrators to delete any pending vacation request according to existing delete behavior.
- **FR-031**: Vacation delete for a missing request ID MUST return the existing not-found behavior.
- **FR-032**: Vacation delete for an existing but out-of-scope request MUST return `403 Forbidden` before business-rule mutation.
- **FR-033**: Phase 10 MUST preserve existing vacation routes, successful response shapes, pagination envelope shape, authentication cookies, claims, status-code conventions, and structured error response shape except for the new forbidden outcomes and optional additive creator metadata explicitly approved in planning.
- **FR-034**: Phase 10 MUST NOT change vacation type rules, working day rules, balance algorithms, notification behavior, frontend behavior, employee endpoint behavior, trip endpoint behavior, or Swagger documentation except where documentation references vacation access rules.
- **FR-035**: Phase 10 MUST include role-and-scope validation covering Employee, Manager, HRAdministrator, and SystemAdministrator behavior for vacation list, detail, create, review, and delete operations.
- **FR-036**: Phase 10 MUST NOT create a database migration unless the plan proves current storage cannot represent required creator tracking and the user approves the migration separately.

### Required Access Matrix

| Operation | Employee | Manager | HRAdministrator | SystemAdministrator |
|-----------|----------|---------|-----------------|---------------------|
| `GET /api/vacationrequests` | Own requests only | Active direct and indirect team requests only by default; own requests only when `employeeId` equals self | Organization-wide | Organization-wide |
| `GET /api/vacationrequests/{id}` | Own request only | Self plus active direct and indirect team requests | Any existing request | Any existing request |
| `POST /api/vacationrequests` | Create for self only | Create for self only | Create for any eligible employee | Create for any eligible employee |
| `PUT /api/vacationrequests/{id}/status` | `403 Forbidden` | Review active direct and indirect team requests only; self-review forbidden | Review any non-self request | Review any non-self request |
| `DELETE /api/vacationrequests/{id}` | Own pending request only | Own pending request only; no team delete | Any pending request | Any pending request |

### Out of Scope

- Employee access hardening. This belongs to Phase 9 and must remain unchanged.
- Trip visibility, requester/traveler distinction, or trip creation authorization. This belongs to Phase 11.
- Lifecycle documentation and full manual retest beyond vacation access wording updates. This belongs to Phase 12.
- Swagger/OpenAPI response documentation pass. This belongs to Phase 13.
- Authentication redesign, JWT, token refresh, SSO, or public setup endpoints.
- New vacation types, accrual rules, carryover rules, payroll rules, notifications, or frontend UI.
- Changing working day calculations or vacation balance algorithms unless a direct Phase 10 scope test exposes a real defect that must be reported before patching.
- New manager team deletion permissions.

### Key Entities *(include if feature involves data)*

- **Requesting Employee**: The signed-in employee whose role and scope determine access.
- **Vacation Request Owner**: The employee whose vacation balance and absence period are represented by the vacation request.
- **Vacation Request Creator**: The authenticated employee who created the request. This may equal the owner for self-created requests or differ for HR/System on-behalf creation. Persisted creator tracking is required but migration-gated; existing rows may keep null creator metadata unless a reliable source exists.
- **Vacation Reviewer**: The authenticated employee who approves or rejects the request, subject to role, scope, and self-review restrictions.
- **Vacation Request List Result**: A paged list constrained by role scope before filters and pagination are applied.
- **Manager Team Scope**: Active direct and indirect reports under a manager according to Phase 8.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of normal-employee vacation list checks return only vacation requests owned by the requester.
- **SC-002**: 100% of normal-employee vacation detail checks allow own request detail and deny other employee request detail.
- **SC-003**: 100% of unfiltered manager vacation list checks contain zero manager-owned, peer, unrelated employee, soft-deleted report, or terminated report vacation requests.
- **SC-003A**: 100% of manager vacation list checks filtered by the manager's own employee ID return only manager-owned vacation requests.
- **SC-004**: 100% of manager vacation detail checks allow own, direct-report, and indirect-report vacation requests while denying outside-scope vacation requests.
- **SC-005**: 100% of Employee and Manager vacation creation checks prevent creating requests for another employee.
- **SC-006**: 100% of HR administrator and system administrator on-behalf creation checks preserve valid vacation creation for eligible employees.
- **SC-007**: 100% of self-review attempts by Manager, HRAdministrator, and SystemAdministrator users are rejected.
- **SC-008**: 100% of Employee review attempts are rejected with `403 Forbidden`.
- **SC-009**: 100% of Manager review checks allow active team requests and deny own or outside-team requests.
- **SC-010**: 100% of scoped delete checks preserve pending-only deletion and deny out-of-scope deletion.
- **SC-011**: Existing vacation validation, same-status idempotent behavior, structured error response shape, successful response shape, cookies, and claims remain compatible outside the new access denials.
- **SC-012**: Phase 10 completes with zero employee, trip, compensation, document, attendance, dashboard, audit, bootstrap, or Swagger behavior changes.
- **SC-013**: No database migration is created unless separately approved after the Phase 10 plan documents the creator-tracking need and migration/backfill strategy.
- **SC-014**: 100% of out-of-scope target operations return `403 Forbidden` before exposing target state or vacation business-rule validation details.
- **SC-015**: 100% of scoped vacation list requests with out-of-scope employee/target filters return an empty paged result with no unauthorized employee data or target-existence disclosure.

## Assumptions

- Phase 8 authorization scope foundation is complete and available before Phase 10 implementation.
- Phase 9 employee access hardening is complete and must not be changed by Phase 10.
- Manager team scope means direct plus indirect active reports, matching the Phase 8 decision.
- Manager vacation list access is team-only by default; manager self vacation requests remain accessible through detail, self-creation, and an explicit `employeeId` self filter.
- Authenticated out-of-scope vacation access returns `403 Forbidden`; missing vacation request IDs continue to return the existing not-found response.
- Vacation list scope filtering happens before pagination so pages do not leak counts or items from outside the requester's scope.
- Current pending-only delete behavior is the approved baseline and should remain unless a later clarification explicitly changes it.
- Persisted creator tracking is required for new requests after approved storage exists, but the current schema does not appear to store it. The technical plan must document the migration/backfill strategy and must not create a migration without explicit approval.
