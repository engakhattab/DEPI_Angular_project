# Feature Specification: Phase 11 - Trips Ownership and Scope Hardening

**Feature Branch**: `[011-trip-ownership-scope-hardening]`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "create specifications for phase 11."

## Boundary Definitions and Scope

- **Trip ownership and scope hardening**: The business-rule change that prevents authenticated users from seeing, creating, or deleting trips outside their approved role and scope.
- **Trip traveler**: The employee the trip belongs to and who is expected to take the trip.
- **Trip requester**: The authenticated employee who creates the trip request.
- **Self scope**: A signed-in employee accessing or creating a trip for their own employee record.
- **Team scope**: A manager accessing or creating trips for active direct and indirect reports, matching the completed Phase 8 team-scope decision.
- **Organization scope**: HR administrators and system administrators accessing or creating trips across the organization.
- **On-behalf trip creation**: A manager, HR administrator, or system administrator creating a trip whose traveler is another employee permitted by that actor's scope.
- **Eligible trip traveler**: A target employee who exists and passes existing trip creation eligibility rules, including active, non-deleted, non-terminated status and any existing trip-specific validation.

Phase 11 is limited to trip list visibility, trip detail visibility, trip creation ownership, delete authorization, requester/traveler distinction, and required requester storage planning. Separate requester storage is required for Phase 11, but any schema migration remains gated behind explicit user approval. It must not change employee endpoint behavior, vacation behavior, Swagger documentation, authentication design, compensation, documents, attendance, dashboard, audit, or frontend behavior.

## Clarifications

### Session 2026-06-15

- Q: How should Phase 11 handle the employee identifier currently sent in trip create requests? -> A: Keep `requestedByEmployeeId` in create requests as the target traveler for compatibility; store the authenticated employee separately as the real requester after approved storage exists.
- Q: How should existing trip rows be interpreted when requester/traveler storage is separated? -> A: Existing `RequestedByEmployeeId` values are treated as the traveler; a new requester reference remains nullable for old rows because historical requester identity is not reliably known.
- Q: Is separate requester storage required in Phase 11 or only optional? -> A: Separate requester storage is required for Phase 11, but implementation must stop for explicit migration approval before creating or applying the schema change.
- Q: How should trip list filters behave when the requested employee/traveler is outside the authenticated user's scope? -> A: Return an empty paged list, preserve the normal pagination shape, and do not reveal whether the out-of-scope employee or trip exists.
- Q: Should Phase 11 change trip deletion semantics? -> A: Preserve current hard-delete behavior for in-scope trips, but require scope authorization before deletion.

## Existing Context Findings

- Phase 8 established reusable self, direct-plus-indirect team, and organization scope decisions.
- Phase 9 hardened employee endpoint access and established a missing-vs-out-of-scope convention for protected records.
- Phase 10 hardened vacation request access and added an approved creator-tracking pattern for on-behalf requests.
- The current trip endpoints are authenticated but do not receive the current requester context for list, detail, create, or delete scope decisions.
- The current trip create request exposes `requestedByEmployeeId`, and Phase 11 keeps that request field as the target traveler for compatibility. The authenticated employee must be recorded separately as the real requester after approved storage exists.
- The current trip entity has `RequestedByEmployeeId` and a `RequestedBy` employee reference. Phase 11 treats the existing value as the traveler for current and historical compatibility, while the authenticated requester must be stored separately after approved storage exists.
- The current trip delete behavior hard-removes a trip when it exists. Phase 11 preserves that hard-delete behavior for in-scope trips while adding role and scope authorization before deletion.
- Existing trip validation rules include target traveler existence, active/non-deleted employee eligibility, future trip date, and working-day trip date.
- Existing structured error responses use the standard `{ code, message }` shape.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Employee Own Trip Privacy (Priority: P1)

As a normal employee, I need to see and create only my own trips so that I cannot view operational travel information for other employees or impersonate them in trip requests.

**Why this priority**: Manual testing showed that employees can see all trips and can create trips for other employees. This is the main privacy and impersonation gap Phase 11 must close.

**Independent Test**: Can be tested by logging in as a normal employee and attempting to list, view, create, and delete trips for self and for other employees.

**Acceptance Scenarios**:

1. **Given** a signed-in normal employee with their own trips and other employees' trips, **When** they request the trip list, **Then** only their own trips are returned.
2. **Given** a signed-in normal employee, **When** they request their own trip detail, **Then** the request succeeds.
3. **Given** a signed-in normal employee, **When** they request another employee's trip detail, **Then** the system returns `403 Forbidden` and no out-of-scope trip data is returned.
4. **Given** a signed-in normal employee, **When** they create a trip for themselves and all existing trip validation rules pass, **Then** the trip is created with the employee as traveler, and the authenticated employee is used as requester for authorization. Persisted requester metadata is recorded only after approved requester storage exists.
5. **Given** a signed-in normal employee, **When** they attempt to create a trip for another employee, **Then** the system rejects the request before creating trip data.
6. **Given** a signed-in normal employee, **When** they attempt to delete another employee's trip, **Then** the system returns `403 Forbidden`.

---

### User Story 2 - Manager Own and Team Trip Operations (Priority: P1)

As a manager, I need to see and create trips for myself and my team while being prevented from accessing peers, unrelated employees, soft-deleted employees, or terminated employees.

**Why this priority**: Managers need operational visibility and trip creation for their teams, but broad access would expose travel records outside their management scope.

**Independent Test**: Can be tested with a hierarchy containing a manager, direct report, indirect report, peer, unrelated employee, soft-deleted report, terminated report, and manager-owned trips.

**Acceptance Scenarios**:

1. **Given** a signed-in manager with active direct and indirect reports, **When** they request the trip list, **Then** only their own and active team trips are returned.
2. **Given** a signed-in manager, **When** they request detail for their own trip or an active direct/indirect report's trip, **Then** the request succeeds.
3. **Given** a signed-in manager, **When** they request detail for a peer, unrelated employee, soft-deleted report, or terminated report trip, **Then** the system returns `403 Forbidden`.
4. **Given** a signed-in manager, **When** they create a trip for themselves or an active direct/indirect report and all existing validations pass, **Then** the trip is created.
5. **Given** a signed-in manager, **When** they attempt to create a trip for a peer, unrelated employee, soft-deleted report, or terminated report, **Then** the system rejects the request before creating trip data.
6. **Given** a signed-in manager, **When** they delete a trip within own/team scope, **Then** the current hard-delete behavior succeeds after authorization.
7. **Given** a signed-in manager, **When** they delete an out-of-scope trip, **Then** the system returns `403 Forbidden`.

---

### User Story 3 - HR and System Organization Trip Operations (Priority: P1)

As an HR administrator or system administrator, I need organization-wide trip visibility and the ability to create trips for eligible employees so that business travel can be managed centrally.

**Why this priority**: The hardening must stop employee and manager overreach without breaking legitimate organization-wide HR and system administration workflows.

**Independent Test**: Can be tested by logging in as HR administrator and system administrator users and verifying list, detail, creation, and delete behavior across employees.

**Acceptance Scenarios**:

1. **Given** a signed-in HR administrator or system administrator, **When** they request the trip list, **Then** organization-wide trips are returned according to existing pagination behavior.
2. **Given** a signed-in HR administrator or system administrator, **When** they request any existing trip detail, **Then** the request succeeds unless the trip does not exist.
3. **Given** a signed-in HR administrator or system administrator, **When** they create a trip for any active employee and all existing trip validation rules pass, **Then** the trip is created for the target traveler and records the authenticated requester after approved requester/traveler storage is available.
4. **Given** a signed-in HR administrator or system administrator, **When** they attempt to create a trip for a missing, inactive, soft-deleted, or terminated employee, **Then** the system rejects the request using existing trip validation conventions.
5. **Given** a signed-in HR administrator or system administrator, **When** they delete any existing trip, **Then** the current hard-delete behavior succeeds after authorization.

---

### User Story 4 - Requester and Traveler Clarity (Priority: P2)

As an HR operator or manager, I need trip records to clearly identify both the employee taking the trip and the employee who requested it so that on-behalf trip creation cannot be confused with impersonation.

**Why this priority**: Scope hardening can prevent immediate impersonation, but clear ownership metadata is needed for accountability and future auditing.

**Independent Test**: Can be tested by creating trips as an employee, manager, HR administrator, and system administrator and verifying the returned trip ownership metadata.

**Acceptance Scenarios**:

1. **Given** an employee creates a trip for themselves, **When** the trip is returned, **Then** traveler and requester identify the same employee if both concepts are available.
2. **Given** a manager creates a trip for a team member, **When** the trip is returned, **Then** traveler identifies the team member and requester identifies the manager after approved requester/traveler storage is available.
3. **Given** HR or System creates a trip for an employee, **When** the trip is returned, **Then** traveler identifies the employee and requester identifies the authenticated administrator after approved requester/traveler storage is available.
4. **Given** existing trip rows were created before requester/traveler separation, **When** they are queried, **Then** the existing employee reference is treated as the traveler and historical requester metadata remains unknown/null unless a reliable source is later approved.

### Edge Cases

- Requesting user has no valid employee ID claim or no current employee context.
- Requesting employee is missing, soft-deleted, or terminated after login.
- Requesting employee is suspended; Phase 11 must preserve the Phase 8 decision unless an existing trip-specific rule already forbids the action.
- Target traveler employee is missing, inactive, soft-deleted, terminated, or otherwise ineligible under existing trip rules.
- Target trip does not exist.
- Target trip exists but is outside the requester's permitted scope.
- Existing out-of-scope trip targets must return `403 Forbidden` before revealing trip state or other trip validation details.
- In-scope trip deletion must preserve the current hard-delete behavior after authorization succeeds.
- Manager has no active direct or indirect reports.
- Manager has only soft-deleted or terminated reports.
- Manager attempts to list, view, create, or delete a trip for a peer, unrelated employee, soft-deleted report, or terminated report.
- Trip list pagination is requested after scope filtering.
- Trip list receives an employee/traveler filter outside the authenticated user's allowed scope; the system must return an empty paged list, preserve the normal pagination shape, and avoid revealing whether the out-of-scope employee or trip exists.
- Trip create request fails existing validation rules such as past trip date or non-working trip date.
- Trip request body attempts to set an employee ID that does not match the authenticated user's allowed self/team/organization scope.
- Existing trips use `RequestedByEmployeeId` as traveler metadata and have no reliable separate requester history.
- Delete is requested for a missing trip, in-scope trip, or out-of-scope trip.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST use the completed Phase 8 self, team, and organization scope definitions for trip access decisions.
- **FR-002**: Trip list, detail, create, and delete operations MUST evaluate the authenticated requester before granting self, team, or organization scope.
- **FR-003**: Requesting employees who are missing, soft-deleted, or terminated MUST NOT receive self, team, or organization trip scope.
- **FR-004**: Employee role users MUST see only trips whose traveler is their own employee record.
- **FR-005**: Manager role users MUST see only trips owned by themselves or active direct and indirect reports.
- **FR-006**: HRAdministrator and SystemAdministrator role users MUST see organization-wide trips.
- **FR-007**: Pagination MUST be applied after trip scope filtering so result pages and counts do not leak out-of-scope trip data.
- **FR-007A**: When a scoped trip list receives an employee or traveler filter outside the authenticated user's allowed scope, the system MUST return an empty paged list, preserve the normal pagination shape, MUST NOT reveal whether the out-of-scope employee or trip exists, and MUST NOT return unauthorized trips.
- **FR-008**: Trip detail for a missing trip ID MUST return the existing not-found behavior.
- **FR-009**: Trip detail for an existing but out-of-scope trip MUST return `403 Forbidden` with the standard structured error shape.
- **FR-010**: Employee role users MUST be able to create trips only for themselves.
- **FR-011**: Manager role users MUST be able to create trips for themselves and active direct or indirect reports only.
- **FR-012**: HRAdministrator and SystemAdministrator role users MUST be able to create trips for any employee who passes existing trip eligibility rules.
- **FR-013**: Trip creation MUST reject attempts to target a missing employee or an ineligible trip traveler according to existing trip business rules. Ineligible trip travelers include inactive, soft-deleted, terminated, and any target employee otherwise rejected by existing trip-specific validation.
- **FR-014**: Trip creation MUST preserve existing validation rules for future trip date, working-day trip date, trip reference, project, route, trip type, generated trip code, and generated request code.
- **FR-015**: The existing `requestedByEmployeeId` trip create request field MUST remain accepted as the target traveler identifier for compatibility and MUST NOT be trusted as the authenticated requester.
- **FR-016**: The authenticated employee MUST be treated as the requester for new trips after approved requester/traveler storage is available.
- **FR-017**: The system MUST distinguish the trip traveler from the trip requester in the business model.
- **FR-018**: Separate requester storage is required for Phase 11, and planning MUST document the migration strategy before any migration is created.
- **FR-019**: Any requester/traveler migration strategy MUST treat existing `RequestedByEmployeeId` values as traveler references and MUST avoid inventing verified historical requester data for existing trip rows.
- **FR-020**: Existing trip rows MAY keep null requester metadata unless a reliable historical requester source is approved; queries and responses MUST remain safe when requester metadata is null.
- **FR-021**: Any requester/traveler migration strategy MUST make new trips record both traveler and authenticated requester after the approved migration.
- **FR-022**: Existing trip routes and request compatibility MUST remain stable unless a later approved clarification explicitly allows a breaking contract change.
- **FR-023**: Existing successful trip response fields MUST remain stable; any traveler/requester metadata added to responses MUST be additive.
- **FR-024**: Trip delete for a missing trip ID MUST return the existing not-found behavior.
- **FR-025**: Trip delete for an existing but out-of-scope trip MUST return `403 Forbidden` before mutation.
- **FR-026**: Employee role users MUST be able to hard-delete only own trips after authorization succeeds.
- **FR-027**: Manager role users MUST be able to hard-delete own or active team trips after authorization succeeds.
- **FR-028**: HRAdministrator and SystemAdministrator role users MUST be able to hard-delete any trip after authorization succeeds.
- **FR-029**: Phase 11 MUST preserve existing authentication cookies, claims, status-code conventions, structured error response shape, and trip validation error codes except for new forbidden outcomes and approved additive traveler/requester metadata.
- **FR-030**: Phase 11 MUST NOT change employee, vacation, compensation, document, attendance, dashboard, audit, bootstrap, or Swagger behavior.
- **FR-031**: Phase 11 MUST include role-and-scope validation covering Employee, Manager, HRAdministrator, and SystemAdministrator behavior for trip list, detail, create, and delete operations.
- **FR-032**: Phase 11 MUST NOT create a database migration until the requester-storage migration strategy is documented and the user explicitly approves migration creation.

### Required Access Matrix

| Operation | Employee | Manager | HRAdministrator | SystemAdministrator |
|-----------|----------|---------|-----------------|---------------------|
| Trip list | Own trips only | Own plus active direct and indirect team trips | Organization-wide | Organization-wide |
| Trip detail | Own trip only | Own plus active direct and indirect team trips | Any existing trip | Any existing trip |
| Trip create | Create for self only | Create for self or active direct/indirect team member | Create for any eligible employee | Create for any eligible employee |
| Trip delete | Own trip only; preserve current hard-delete behavior | Own/team trip only; preserve current hard-delete behavior | Any trip; preserve current hard-delete behavior | Any trip; preserve current hard-delete behavior |

### Out of Scope

- Employee endpoint access hardening. This belongs to Phase 9 and must remain unchanged.
- Vacation request access, creator tracking, or review rules. This belongs to Phase 10 and must remain unchanged.
- Swagger/OpenAPI response documentation pass. This belongs to Phase 13.
- Lifecycle documentation full retest beyond narrow trip access wording. This belongs to Phase 12.
- New trip approval, rejection, or status workflow.
- New trip billing, notification, transportation scheduling, or frontend UI behavior.
- Authentication redesign, JWT, token refresh, SSO, or public setup endpoints.
- Changing working-day rules or trip-code generation unless a direct Phase 11 scope test exposes a real defect that must be reported before patching.

### Key Entities *(include if feature involves data)*

- **Requesting Employee**: The signed-in employee whose role and scope determine trip access.
- **Trip Traveler**: The employee the trip belongs to and who is expected to take the trip.
- **Trip Requester**: The authenticated employee who created the trip request. This may equal the traveler for self-created trips or differ for manager/HR/System on-behalf creation.
- **Trip List Result**: A paged list constrained by role scope before pagination is applied.
- **Manager Team Scope**: Active direct and indirect reports under a manager according to Phase 8.
- **Historical Trip Row**: A trip created before requester/traveler separation, where the existing employee reference is treated as traveler metadata and requester metadata may be null.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of normal-employee trip list checks return only trips whose traveler is the authenticated employee.
- **SC-002**: 100% of normal-employee trip detail checks allow own trip detail and deny other employee trip detail.
- **SC-003**: 100% of normal-employee trip creation checks prevent creating trips for another employee.
- **SC-004**: 100% of manager trip list checks contain only manager-owned trips and active direct/indirect team-owned trips.
- **SC-005**: 100% of manager trip detail checks allow own, direct-report, and indirect-report trips while denying outside-scope trips.
- **SC-006**: 100% of manager trip creation checks allow self/team travelers and reject outside-scope travelers.
- **SC-007**: 100% of HR administrator and system administrator trip list/detail checks preserve organization-wide visibility.
- **SC-008**: 100% of HR administrator and system administrator trip creation checks preserve valid creation for eligible employees.
- **SC-009**: 100% of scoped trip delete checks preserve existing delete behavior for in-scope trips and deny out-of-scope deletion.
- **SC-010**: 100% of out-of-scope target operations return `403 Forbidden` before exposing out-of-scope trip details or validation outcomes.
- **SC-010A**: 100% of scoped trip list requests with out-of-scope employee/traveler filters return an empty paged result with no unauthorized trip data or target-existence disclosure.
- **SC-011**: Existing trip validation, successful response compatibility, structured error response shape, cookies, and claims remain compatible outside the new scope denials and approved additive ownership metadata.
- **SC-012**: Phase 11 completes with zero employee, vacation, compensation, document, attendance, dashboard, audit, bootstrap, or Swagger behavior changes.
- **SC-013**: No database migration is created unless separately approved after the Phase 11 plan documents the required requester-storage migration/backfill strategy.

## Assumptions

- Phase 8 authorization scope foundation is complete and available before Phase 11 implementation.
- Phase 9 employee access hardening and Phase 10 vacation scope hardening are complete and must not be changed by Phase 11.
- Manager team scope means direct plus indirect active reports, matching the Phase 8 decision.
- Authenticated out-of-scope trip access returns `403 Forbidden`; missing trip IDs continue to return the existing not-found response.
- Trip list scope filtering happens before pagination so pages do not leak counts or items from outside the requester's scope.
- Current hard-delete trip behavior is the approved baseline and remains part of Phase 11 after scope authorization passes.
- Persisted requester/traveler separation is required for Phase 11, but the current schema appears to have only one employee reference that should be interpreted as the traveler. The technical plan must document the migration/backfill strategy and must not create a migration without explicit approval.
