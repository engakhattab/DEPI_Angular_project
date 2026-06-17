# Feature Specification: Phase 12 - Lifecycle Documentation and Manual Retest

**Feature Branch**: `012-lifecycle-manual-retest`  
**Created**: 2026-06-17  
**Status**: Draft  
**Input**: User description: "Read the existing project context and create a specification for phase 12."

## Boundary Definitions and Scope

Phase 12 is a documentation and manual validation phase for the completed HR backend scope-hardening work. It updates lifecycle testing and client setup documentation so a developer can manually retest the full business flow against a fresh local SQL Server database after Phases 8-11.

Phase 12 includes both documentation updates and execution of the documented local fresh-database manual retest. The manual retest MUST use a disposable local SQL Server database named `HrSystemDb_Phase12LifecycleTest` so validation does not mutate the current project database. This local database may be created, reset, seeded with local-only validation data, and updated using existing approved migrations for Phase 12 validation. Phase 12 MUST NOT implement new backend behavior, change source code, change routes, change response JSON, change cookies, change claims, change status codes, change error codes, create new database migrations, or perform the Phase 13 Swagger/OpenAPI documentation pass. If manual retesting finds a runtime defect, the defect MUST be recorded and stopped for explicit follow-up approval before source code changes.

## Clarifications

### Session 2026-06-17

- Q: Should Phase 12 update docs only, or update docs and execute the local manual retest? -> A: Update docs/checklists and execute the local manual retest, recording pass/fail evidence in the Phase 12 summary.
- Q: Which local SQL Server database should Phase 12 use for the fresh manual retest? -> A: Create/use `HrSystemDb_Phase12LifecycleTest` as the disposable Phase 12 manual retest database.

## User Scenarios and Testing *(mandatory)*

### User Story 1 - Retest Current Role Scope Behavior (Priority: P1)

As a backend developer or tester, I need the lifecycle testing guide to reflect the completed Phase 8-11 authorization rules so I can manually validate employee, vacation, and trip behavior with the correct role expectations.

**Why this priority**: The existing lifecycle guide predates later scope hardening and can mislead testers into expecting access that should now return 403, empty scoped lists, or business-rule failures.

**Independent Test**: Follow the updated lifecycle guide from a fresh local database using the documented four-role dataset and verify that each role-specific employee, vacation, and trip scenario has an expected status/result matching current Phase 8-11 behavior.

**Acceptance Scenarios**:

1. **Given** an authenticated Employee actor, **When** the tester follows employee, vacation, trip, compensation, and audit checks, **Then** the guide expects self-scoped access only where allowed and forbidden outcomes for organization-wide or sensitive data.
2. **Given** an authenticated Manager actor with a team, **When** the tester follows manager checks, **Then** the guide expects team-scoped employee/vacation/trip behavior, prevents outside-scope access, and blocks self-review.
3. **Given** authenticated HR Administrator and System Administrator actors, **When** the tester follows elevated checks, **Then** the guide distinguishes HR-wide access from System Administrator-only role assignment.

---

### User Story 2 - Recreate a Fresh Manual Test Dataset (Priority: P1)

As a developer preparing local validation, I need deterministic setup instructions for a fresh SQL Server database so I can recreate the same manual test actors and relationships each time.

**Why this priority**: Manual scope validation depends on stable employee roles, manager/team relationships, departments, credentials, and migrated schema state.

**Independent Test**: Starting with a clean local database, apply the documented setup flow and create or verify EMP001 through EMP004 with the required roles and relationships without editing source code or committing secrets.

**Acceptance Scenarios**:

1. **Given** a clean database, **When** the tester follows the setup instructions, **Then** the database has a System Administrator, HR Administrator, Manager, and Employee test actor with clear local-only credentials.
2. **Given** the local dataset is configured, **When** manager-scoped tests run, **Then** the Manager actor has at least one direct team member and at least one outside-scope employee exists for negative tests.
3. **Given** local-only passwords are documented, **When** docs are committed, **Then** they are labeled as non-production placeholders and no customer-specific credentials or secrets are introduced.

---

### User Story 3 - Align Client Setup and Handoff Documentation (Priority: P2)

As a client installer or handoff recipient, I need installation and completion documentation to reflect the latest migrations, bootstrap flow, permissions, and retest evidence so deployment and verification steps are not stale.

**Why this priority**: Client setup documentation is used after development, and stale migration or permission notes can lead to incorrect installations or invalid acceptance testing.

**Independent Test**: Review the client installation guide, implementation summary, and manual retest checklist and confirm they reference the latest Phase 8-11 behavior and record the Phase 12 retest result.

**Acceptance Scenarios**:

1. **Given** the client installation guide mentions migrations or endpoint permissions, **When** Phase 12 documentation is complete, **Then** those sections include current Phase 10 and Phase 11 migration context and no stale Phase 7-only migration list remains.
2. **Given** the manual retest is executed, **When** results are recorded, **Then** each required role scenario has pass/fail evidence and any failure is captured as a follow-up defect instead of silently changing code.

### Edge Cases

- Fresh database has no bootstrap administrator yet.
- Required department, manager, or employee relationship data is missing.
- Cookie authentication is not preserved by the manual testing tool between requests.
- Unauthenticated requests must be distinguished from authenticated forbidden requests: 401 versus 403.
- Out-of-scope list filters must preserve normal pagination/list shape and avoid revealing whether the target exists where current scoped behavior requires non-disclosure.
- Employee, Manager, HR Administrator, and System Administrator self-review attempts for vacation review must remain blocked.
- Existing historical trip rows may have null requester values from approved migration/backfill strategy and must be documented as compatibility behavior where relevant.
- Soft-deleted or terminated employees must not be treated as valid authenticated actors.
- Local sample passwords must be marked as local-only placeholders and not production secrets.
- Phase 13 Swagger/OpenAPI response annotation gaps must not be solved in Phase 12.
- If manual retest discovers a behavior mismatch, Phase 12 records it and stops for separate implementation approval.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system documentation MUST update `API_LIFECYCLE_TESTING_GUIDE.md` to reflect completed Phase 8-11 authorization scope behavior for authentication, employees, vacations, trips, attendance, compensation, documents, dashboard, and audit checks.
- **FR-002**: The lifecycle guide MUST document and execute a fresh local database setup flow using `HrSystemDb_Phase12LifecycleTest`, including application of existing approved migrations through Phase 11, initial administrator bootstrap, department setup, and the required four-role test actors.
- **FR-003**: The manual dataset MUST include EMP001 `admin@test.com` as System Administrator, EMP002 `hr.admin@test.com` as HR Administrator, EMP003 `manager@test.com` as Manager, and EMP004 `employee@test.com` as Employee, with passwords clearly labeled as local-only placeholders.
- **FR-004**: Employee endpoint documentation MUST state that Employee actors cannot list all employees, cannot create employees, cannot view out-of-scope employee details, and can only access allowed self-scoped employee behavior.
- **FR-005**: Manager endpoint documentation MUST state that Manager actors can access team-scoped employee data, cannot access outside-team employees, cannot create employees unless already allowed by current implementation, and cannot use employee endpoints to probe outside-scope target existence.
- **FR-006**: HR Administrator and System Administrator employee documentation MUST state that HR/System can access organization employee data where authorized, only System Administrator can assign roles, HR Administrator cannot assign roles, and last-active System Administrator protection remains expected.
- **FR-007**: Vacation documentation MUST state that Employees see/create self vacation requests only, Managers see/review team vacation requests only, HR/System can see organization vacation requests and create for employees, and self-review is blocked for every role.
- **FR-008**: Vacation documentation MUST include expected outcomes for out-of-scope details and filtered lists, including empty scoped list/page behavior where applicable and 403/404 behavior where current contracts require it.
- **FR-009**: Trip documentation MUST state that Employees can access and create own trips only, Managers can access and create own/team trips only, HR/System can access and create trips for any employee, and requester/traveler ownership behavior is explicit.
- **FR-010**: Trip documentation MUST include expected handling for requester ownership, traveler scope, out-of-scope access, invalid dates, historical null requester rows, and manager team boundaries.
- **FR-011**: Documentation MUST list expected 401, 403, 404, 409, and 422 outcomes where relevant and MUST preserve the existing structured `{ code, message }` error payload expectation without normalizing or inventing new error codes.
- **FR-012**: `CLIENT_INSTALLATION_GUIDE.md` MUST be reviewed and updated if it contains stale migration names, endpoint permissions, bootstrap instructions, or manual validation instructions affected by Phases 8-11.
- **FR-013**: Phase 12 MUST create or update a manual retest checklist covering all required role scenarios for Employee, Manager, HR Administrator, and System Administrator.
- **FR-014**: Phase 12 MUST execute the documented local manual retest and record results in a Phase 12 implementation summary or handoff notes, including `HrSystemDb_Phase12LifecycleTest` environment/database context, commands run, role scenarios tested, failures, and follow-up defects.
- **FR-015**: Phase 12 MUST NOT modify source code, create new database migrations, change committed schema design, runtime behavior, route names, DTO fields, cookie behavior, claims, status codes, or error-code behavior. Local database creation/reset/update with existing approved migrations is allowed only for Phase 12 validation evidence.
- **FR-016**: Phase 12 MUST NOT perform the Phase 13 Swagger/OpenAPI response annotation pass; Swagger documentation gaps may be listed as deferred Phase 13 work only.
- **FR-017**: Documentation MUST remove or correct stale wording that implies any authenticated user can access all employee, vacation, or trip data.
- **FR-018**: If manual validation fails because implementation behavior differs from the completed Phase 8-11 requirements, Phase 12 MUST document the failure and require separate user approval before any implementation fix.

### Key Entities

- **Lifecycle Testing Guide**: The primary manual testing document that describes setup, login, endpoint calls, expected responses, and role-specific HR workflow validation.
- **Client Installation Guide**: Deployment/setup document that may mention migrations, bootstrap, configuration, endpoint permissions, and validation commands.
- **Manual Retest Dataset**: The local-only set of departments, employees, roles, relationships, and credentials used to exercise every scope boundary.
- **Role Actor**: One of System Administrator, HR Administrator, Manager, or Employee used during manual validation.
- **Scope Outcome**: The expected result of a request under the current authorization rules, such as allowed, forbidden, not found, empty scoped list, conflict, or validation failure.
- **Manual Retest Checklist**: A task-oriented checklist that records each required manual scenario and final pass/fail status.
- **Retest Result Summary**: Phase 12 completion evidence recording environment, commands, manual checks, failures, and deferred follow-up items.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of lifecycle guide sections covering Auth, Employees, Vacations, Trips, Attendance, Compensation, Documents, Dashboard, and Audit include current actor/scope expectations or explicitly state that no Phase 8-11 scope change applies.
- **SC-002**: A developer can recreate the documented fresh local SQL Server manual test dataset in `HrSystemDb_Phase12LifecycleTest` without source code edits, without creating new migrations, and without committing secrets.
- **SC-003**: 100% of required role scenarios from the Phase 12 roadmap have checklist entries with expected result, actor, endpoint/action, and pass/fail recording space.
- **SC-004**: Updated documentation contains no stale instruction that authenticated users can access all employee, vacation, or trip records regardless of role or scope.
- **SC-005**: Manual retest evidence records an actual result for every required EMP001-EMP004 role scenario or records a blocked reason with follow-up action.
- **SC-006**: No source files, migrations, schema changes, route definitions, DTOs, cookies, claims, status-code behavior, or error-code behavior are modified as part of Phase 12 documentation work.

## Assumptions

- Phase 8 Authorization Scope Foundation, Phase 9 Employee Access Scope Hardening, Phase 10 Vacation Scope Hardening, and Phase 11 Trip Ownership Scope Hardening are implemented before Phase 12 documentation retest starts.
- Phase 10 `VacationRequest.CreatedByEmployeeId` and Phase 11 `Trip.RequesterEmployeeId` migrations are approved project history and should be reflected in documentation where migration lists are maintained.
- The manual tester can run the API locally, access SQL Server, use Swagger or an HTTP client that preserves cookies, and override local connection strings without committing environment-specific values.
- Phase 12 validation uses `HrSystemDb_Phase12LifecycleTest` as a disposable local database and does not mutate the current project database unless separately approved.
- Sample credentials in documentation are local-only placeholders and are not customer credentials.
- Swagger/OpenAPI response documentation hardening is deferred to Phase 13.
