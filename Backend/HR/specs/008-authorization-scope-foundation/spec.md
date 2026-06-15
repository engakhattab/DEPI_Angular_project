# Feature Specification: Phase 8 - Authorization Scope Foundation

**Feature Branch**: `[008-authorization-scope-foundation]`

**Created**: 2026-06-13

**Status**: Draft

**Input**: User description: "Start with Phase 8 only: Authorization Scope Foundation. Read AUTHORIZATION_SCOPE_MULTI_PHASE_ROADMAP.md first. Do not implement anything yet. Create a Spec Kit specification for Phase 8 only."

## Boundary Definitions and Scope

- **Authorization scope foundation**: The shared business rules and reusable decision surface used to determine what an authenticated employee may access by role, ownership, manager team relationship, and organization-wide privilege.
- **Current employee**: The employee represented by the authenticated session. The current employee must be resolvable to a valid employee record before service-level authorization decisions are trusted.
- **Self scope**: Access to records that belong to the current employee.
- **Team scope**: Access for a manager to active, non-deleted direct and indirect reports through the existing manager relationship. This phase chooses direct plus indirect reports because the current project already supports and tests hierarchy traversal.
- **Organization scope**: Access for `HRAdministrator` and `SystemAdministrator` roles to organization-wide HR operational records.
- **Scope decision**: A reusable answer such as "is self", "is manager of target", "can access employee", "has organization scope", or "has required role".

Phase 8 is a foundation phase. It must not harden employee, vacation, or trip endpoints yet. It defines the shared scope behavior that Phases 9, 10, and 11 will use to fix endpoint-specific visibility and ownership rules.

## Clarifications

### Session 2026-06-13

- Q: Should suspended users keep role/team/organization scope eligibility in the Phase 8 foundation? -> A: Suspended users remain scope-eligible unless deleted or terminated; endpoint-specific phases decide which suspended actions are forbidden.

## Existing Context Findings

- The system already uses cookie-based sessions and authenticated employee claims.
- The current session includes an `employee_id` claim, employee number claim, full-name claim, email claim, framework role claim, and `employee_role` claim.
- Controllers currently extract the current employee ID from the authenticated principal when service methods require requester context.
- Existing session validation rejects missing, soft-deleted, or terminated employee sessions during cookie validation.
- Current roles are single-value employee roles: `Employee`, `Manager`, `HRAdministrator`, and `SystemAdministrator`.
- A shared application-level access contract already exists for current employee context, role checks, employee access checks, and visible employee IDs.
- Existing manager scope tests already cover direct reports, indirect reports, peers, unrelated employees, soft-deleted reports, and suspended reports.
- Some services already use requester-aware scope checks, including attendance, compensation, documents, dashboard, and audit logs.
- Employee, vacation request, and trip service contracts still expose list/detail/create operations that are not consistently requester-aware. Their endpoint behavior must remain unchanged in Phase 8 and be hardened in later phases.

## Team Scope Decision

Phase 8 chooses **direct plus indirect reports** as the team scope definition.

Team scope includes:

- the manager's direct active reports
- active indirect reports through the manager chain
- the manager themself only where the specific scope decision is "visible employees" or "can access employee self"

Team scope excludes:

- peers
- unrelated employees
- the manager's own manager
- soft-deleted employees
- terminated employees
- suspended employees from team data sets unless a later endpoint-specific phase explicitly allows suspended targets for a particular operation

This decision matches the roadmap preference and the current manager-chain behavior already present in the project.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Resolve Current Employee Scope (Priority: P1)

As a protected HR workflow, I need a consistent current-employee context so that later authorization checks are based on the authenticated employee record rather than duplicated controller logic.

**Why this priority**: Every later scope decision depends on knowing who the current employee is, what role they hold, and whether their account is still eligible to act.

**Independent Test**: Can be tested by resolving current employee context for active, suspended, terminated, soft-deleted, and missing employees and verifying that eligibility flags and failures are consistent with the existing authentication rules.

**Acceptance Scenarios**:

1. **Given** an authenticated active employee, **When** the current employee context is resolved, **Then** the result identifies the employee ID, role, active state, and account state.
2. **Given** an authenticated suspended employee, **When** the current employee context is resolved, **Then** the result identifies the employee and reports that they are not active without treating them as terminated.
3. **Given** a terminated or soft-deleted employee, **When** the session is validated or current employee context is used for role-based authorization, **Then** privileged access is denied consistently.
4. **Given** a missing or invalid employee identifier, **When** a scope decision is requested, **Then** the decision fails as unauthenticated or not allowed without exposing unrelated data.

---

### User Story 2 - Evaluate Role and Organization Scope (Priority: P1)

As an HR administrator or system administrator, I need organization-wide scope decisions to be consistent so that later employee, vacation, and trip hardening can safely reuse one definition of broad access.

**Why this priority**: Organization-scope users are allowed to see more records than employees or managers. Inconsistent checks can either leak data or block legitimate HR operations.

**Independent Test**: Can be tested by assigning each supported role and checking that role and organization-scope decisions match the Phase 8 access definitions.

**Acceptance Scenarios**:

1. **Given** an employee role user, **When** organization scope is evaluated, **Then** organization scope is denied.
2. **Given** a manager role user, **When** organization scope is evaluated, **Then** organization scope is denied.
3. **Given** an HR administrator, **When** organization scope is evaluated, **Then** organization scope is granted.
4. **Given** a system administrator, **When** organization scope is evaluated, **Then** organization scope is granted.
5. **Given** a suspended administrator, **When** organization-scope eligibility is evaluated in the Phase 8 foundation, **Then** organization scope remains eligible unless a later endpoint-specific phase forbids the specific action.
6. **Given** a terminated or soft-deleted administrator, **When** any organization-scope decision is evaluated, **Then** organization scope is denied.

---

### User Story 3 - Evaluate Self and Manager Team Scope (Priority: P1)

As a manager, I need team-scope decisions to include my direct and indirect reports but exclude peers and unrelated employees so that later endpoint hardening can filter records safely.

**Why this priority**: The remaining roadmap phases depend on a reliable team definition for employee, vacation, and trip records.

**Independent Test**: Can be tested with a prepared reporting chain containing a manager, direct report, indirect report, peer, unrelated employee, soft-deleted report, and terminated report.

**Acceptance Scenarios**:

1. **Given** a normal employee, **When** self scope is evaluated against their own employee ID, **Then** access is allowed.
2. **Given** a normal employee, **When** self scope is evaluated against another employee ID, **Then** access is denied.
3. **Given** a manager with a direct report, **When** manager-team scope is evaluated for the direct report, **Then** access is allowed.
4. **Given** a manager with an indirect report, **When** manager-team scope is evaluated for the indirect report, **Then** access is allowed.
5. **Given** a manager and a peer or unrelated employee, **When** manager-team scope is evaluated for that target, **Then** access is denied.
6. **Given** a manager and a soft-deleted or terminated report, **When** visible team data is requested, **Then** the target is excluded unless a later endpoint-specific phase explicitly documents a historical-record exception.

---

### User Story 4 - Prepare Later Endpoint Hardening Without Changing Behavior (Priority: P2)

As a maintainer, I need Phase 8 to prepare reusable scope decisions without changing employee, vacation, or trip endpoint behavior yet so that each later phase remains small and reviewable.

**Why this priority**: The roadmap requires employee, vacation, and trip fixes to happen in separate phases after the foundation is clear.

**Independent Test**: Can be tested by confirming Phase 8 introduces or verifies only shared scope behavior and focused tests, while employee, vacation, and trip endpoint-specific behavior remains unchanged.

**Acceptance Scenarios**:

1. **Given** Phase 8 work is complete, **When** endpoint behavior is reviewed, **Then** no employee, vacation, or trip scope hardening has been implemented ahead of Phases 9, 10, or 11.
2. **Given** later phases need self, team, or organization decisions, **When** they are planned, **Then** they can reference the shared Phase 8 definitions instead of creating conflicting local rules.
3. **Given** no schema change is needed for current-user and manager-chain scope decisions, **When** Phase 8 is completed, **Then** no migration is created.

### Edge Cases

- Current employee identifier is missing from the authenticated session.
- Current employee identifier points to no employee record.
- Current employee is soft-deleted or terminated after login.
- Current employee is suspended: the foundation must expose non-active state and preserve existing session behavior; endpoint-specific phases decide which suspended actions are forbidden.
- Target employee ID is missing, soft-deleted, terminated, suspended, or outside the requester's allowed scope.
- Manager chain contains direct and indirect reports.
- Manager chain contains soft-deleted or terminated employees.
- Manager and target are in different departments; department mismatch alone does not grant or deny team scope.
- A user has a valid role claim but the employee record no longer allows that role to be trusted.
- Existing employee, vacation, and trip endpoints remain over-broad until their dedicated phases.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST have one canonical definition of current employee context for service-level authorization decisions.
- **FR-002**: Current employee context MUST identify the current employee ID, current role, active status, soft-delete status, and terminated status.
- **FR-003**: Current employee context MUST expose the minimum fields required for authorization scope decisions. Email and username are not required for Phase 8 unless already available without expanding the context contract.
- **FR-004**: The system MUST treat a missing current employee record as an unauthenticated or invalid-session condition for scope decisions.
- **FR-005**: The system MUST deny role-based and organization-scope authorization for soft-deleted or terminated employees.
- **FR-005A**: The system MUST keep suspended employees scope-eligible in the shared Phase 8 foundation unless a later endpoint-specific phase forbids the specific action.
- **FR-006**: The system MUST preserve existing cookie-based authentication and existing role names.
- **FR-007**: The system MUST support exactly these role-scope categories: `Employee`, `Manager`, `HRAdministrator`, and `SystemAdministrator`.
- **FR-008**: The system MUST define `IsSelf(targetEmployeeId)` as true only when the target employee ID equals the current employee ID.
- **FR-009**: The system MUST define manager team scope as direct plus indirect active, non-deleted reports through the existing manager relationship.
- **FR-010**: The system MUST exclude peers, unrelated employees, the manager's own manager, soft-deleted employees, and terminated employees from manager team scope.
- **FR-011**: The system MUST allow a manager to access themself through self scope, not by treating themself as their own report.
- **FR-012**: The system MUST define organization scope as true for non-deleted, non-terminated `HRAdministrator` and `SystemAdministrator` employees, including suspended administrators unless a later endpoint-specific phase forbids the specific action.
- **FR-013**: The system MUST define `CanAccessEmployee(targetEmployeeId)` as true when the target is self, when the current manager has team scope to the target, or when the current employee has organization scope.
- **FR-014**: The system MUST define visible employee IDs consistently: employees see self only, managers see self plus active direct and indirect reports, and organization-scope users see active organization employees.
- **FR-015**: The system MUST keep service-layer scope checks reusable by later employee, vacation, and trip phases; controller attributes alone are not sufficient for business scope.
- **FR-016**: Phase 8 MUST NOT implement employee list/detail/create hardening, vacation request scope hardening, or trip ownership/scope hardening.
- **FR-017**: Phase 8 MUST NOT create database migrations unless a later approved plan proves the existing employee role and manager relationship cannot represent required scope decisions.
- **FR-018**: Phase 8 MUST NOT change routes, response JSON shapes, cookies, claims, status codes, or error codes.
- **FR-019**: Phase 8 MUST preserve existing successful behavior for attendance, compensation, documents, dashboard, audit logs, authentication, and bootstrap while clarifying shared scope decisions.
- **FR-020**: Phase 8 MUST include focused validation for self scope, organization scope, manager direct reports, manager indirect reports, outside-team targets, employee no-team-scope behavior, and inactive/deleted/terminated employee behavior.

### Required Scope Decisions

| Decision | Required Meaning |
|----------|------------------|
| Current employee context | Resolve the authenticated employee record and role state used by service authorization |
| Is self | Current employee ID equals target employee ID |
| Is manager of target | Target is an active, non-deleted direct or indirect report |
| Can access employee | Self, manager team target, or organization scope |
| Can access team data | Manager access to self plus active direct and indirect reports, or organization scope where applicable |
| Is HR administrator | Current role is `HRAdministrator` and employee is not deleted or terminated |
| Is system administrator | Current role is `SystemAdministrator` and employee is not deleted or terminated |
| Has organization scope | Current role is `HRAdministrator` or `SystemAdministrator` and employee is not deleted or terminated |

### Out of Scope

- Employee endpoint hardening. This belongs to Phase 9.
- Vacation request scope, ownership, on-behalf creation, or review hardening. This belongs to Phase 10.
- Trip visibility, ownership, traveler/requester distinction, or create/delete hardening. This belongs to Phase 11.
- Lifecycle guide rewrite after hardening. This belongs to Phase 12.
- Swagger/OpenAPI response documentation. This belongs to Phase 13.
- Authentication redesign, JWT, token refresh, SSO, or public setup endpoints.
- New role names, multi-role employees, temporary elevated grants, or permission union behavior.
- Database schema changes unless separately approved after analysis.

### Key Entities *(include if feature involves data)*

- **Authenticated Employee Context**: The resolved identity and account state for the signed-in employee, including employee ID, role, and eligibility flags.
- **Employee Role**: The single current role assigned to an employee: `Employee`, `Manager`, `HRAdministrator`, or `SystemAdministrator`.
- **Manager Relationship**: The existing employee-to-manager relationship used to calculate direct and indirect reports.
- **Scope Decision**: A reusable authorization answer used by service workflows to allow, deny, or filter access.
- **Visible Employee Set**: The employee IDs visible to a requester based on self, team, or organization scope.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of self-scope checks allow a user to access their own employee ID and deny unrelated employee IDs for normal employees.
- **SC-002**: 100% of tested organization-scope checks allow non-deleted, non-terminated HR administrators and system administrators, including suspended administrators, and deny employees, managers, soft-deleted users, and terminated users.
- **SC-003**: 100% of manager-scope checks allow direct and indirect active reports while denying peers, unrelated employees, soft-deleted reports, and terminated reports.
- **SC-004**: 100% of visible-employee-set checks return self only for employees, self plus active direct and indirect reports for managers, and active organization employees for organization-scope roles.
- **SC-005**: 100% of focused Phase 8 tests can run without requiring employee, vacation, or trip endpoint behavior changes.
- **SC-006**: Phase 8 completion produces zero new migrations and zero route, response-shape, cookie, claim, status-code, or error-code changes.
- **SC-007**: Later Phase 9, 10, and 11 plans can reference Phase 8 scope definitions without redefining self, team, or organization scope.

## Assumptions

- Phase 7 implementation is complete before Phase 8 planning begins.
- Existing cookie-based authentication remains the only authentication mechanism.
- The existing employee role enum remains the role source of truth.
- The existing employee manager relationship remains the team hierarchy source of truth.
- Team scope is direct plus indirect reports because existing repository behavior and tests already support that decision.
- Suspended employees are not active, but they are not the same as terminated employees. They remain scope-eligible in the Phase 8 foundation unless a later endpoint-specific phase forbids a specific action.
- Phase 8 is allowed to add or refine tests and shared scope artifacts during implementation, but this specification does not authorize endpoint-specific behavior changes.
- Employee, vacation, and trip over-broad access issues remain known gaps until Phases 9, 10, and 11.
