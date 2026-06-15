# HR Backend Scope Hardening Multi-Phase Roadmap

## Purpose

This roadmap splits the remaining business-logic fixes into separate phases instead of treating them as one large change.

The goal is to help Codex understand **why each phase exists**, what problem it solves, what business rules must be enforced, and how to move through Spec Kit safely from specification to implementation.

This file is a planning reference before starting any code changes.

The project is already functionally complete, but manual testing found important HR business-logic gaps around:

- employee visibility
- vacation request visibility and ownership
- trip requester/traveler ownership
- Swagger/OpenAPI undocumented responses

These issues should be fixed carefully in multiple phases because they affect security, privacy, authorization, and real HR behavior.

---

# Global Rule

Do not implement all phases at once.

Each phase must follow the Spec Kit workflow independently:

1. `/speckit.specify`
2. `/speckit.clarify`
3. `/speckit.checklist`
4. `/speckit.plan`
5. `/speckit.tasks`
6. `/speckit.analyze`
7. `/speckit.implement`
8. post-implementation review
9. validation
10. handoff/update notes

After each phase, stop and wait for approval before starting the next phase.

---

# Global Business Model

## Roles

The system uses these roles:

| Role | Meaning |
|---|---|
| `Employee` | Normal employee with self-service access only |
| `Manager` | Employee manager with team-scoped access |
| `HRAdministrator` | HR admin with organization-wide HR operational access |
| `SystemAdministrator` | Highest system/admin role with role assignment and full organization access |

---

## Scope Definitions

### Self Scope

A user can access data that belongs to their own employee profile.

Examples:

- own profile
- own attendance
- own vacation requests
- own trips

---

### Team Scope

A manager can access employees and requests that belong to their team.

Preferred definition:

- direct reports
- indirect reports if the system can support hierarchy traversal safely

If the current code supports only direct reports, Codex must document this and either:

1. implement direct-only team scope for now, or
2. implement direct + indirect team scope if it can be done safely and simply

The chosen behavior must be explicit in the spec.

---

### Organization Scope

HRAdministrator and SystemAdministrator can access organization-wide operational HR data.

Examples:

- all employees
- all vacation requests
- all trips
- audit logs, if already allowed
- compensation/documents, if already implemented and role-protected

---

## Important Principle

`[Authorize]` is not enough.

`[Authorize]` only proves the user is logged in.

The application must also enforce:

- role checks
- ownership checks
- team-scope checks
- on-behalf action rules

Scope filtering should be enforced in the Application/Service layer, not only in controller attributes.

---

# Migration Policy For All Phases

Migrations are allowed only when a phase proves that the current database schema cannot represent the required business rule.

Codex must not create a migration automatically.

Before creating any migration, Codex must stop and report:

1. Which business rule requires a schema change
2. Why a code-only fix is not enough
3. Which table/columns/relationships/indexes must change
4. Whether existing data needs backfill
5. Migration name proposed
6. Tests that prove the need

Only after approval should Codex create the migration.

---

# Validation Required After Every Phase

At the end of each phase, run:

```powershell
dotnet build .\HR.slnx -c Release
dotnet test .\HR.slnx -c Release
dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

If a migration was approved and created, also run:

```powershell
dotnet ef database update --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

---

# Phase 8 — Authorization Scope Foundation

## Phase Goal

Create or confirm the shared authorization-scope foundation that later phases will use.

This phase should not fix every endpoint yet.

Its goal is to make the later phases safer and consistent.

---

## Why This Phase Exists

Manual testing showed that some endpoints are protected only by authentication, not by business scope.

Before fixing employees, vacations, and trips, the system needs a consistent way to answer:

- Who is the current employee?
- What role does the current employee have?
- Is this target employee the current user?
- Is this target employee inside the manager's team?
- Is this user HR/System Admin?

---

## Problems This Phase Solves

- Avoid duplicating scope logic in every controller
- Avoid controller-only authorization
- Avoid inconsistent Manager team logic across modules
- Provide reusable services/helpers for later phases

---

## Business Logic Required

Codex must identify or create a clean way to evaluate:

### Current User Context

Required information:

- current authenticated employee ID
- current role
- current email/username if needed
- whether current user is active and not deleted/terminated if already part of auth validation

### Scope Checks

Required decisions:

- `IsSelf(targetEmployeeId)`
- `IsManagerOf(targetEmployeeId)`
- `CanAccessEmployee(targetEmployeeId)`
- `CanAccessTeamData(targetEmployeeId)`
- `IsHRAdministrator`
- `IsSystemAdministrator`
- `HasOrganizationScope`

### Manager Team Scope

Preferred:

- direct + indirect reports

Acceptable if project constraints require:

- direct reports only, documented clearly

---

## In Scope

- inspect current auth/current-user service
- inspect claims usage
- inspect role enum/string handling
- inspect manager relationship model
- add reusable application-level scope helper if needed
- add tests for helper behavior

---

## Out of Scope

- do not change endpoint behavior yet except if required to support the helper
- do not redesign authentication
- do not change cookie auth to JWT
- do not change role names
- do not implement employee/vacation/trip fixes yet

---

## Tests Required

Add focused tests for:

- self scope
- HR/System organization scope
- manager direct reports
- manager outside-team employee
- indirect reports if implemented
- employee has no team scope
- inactive/deleted/terminated employee behavior if relevant to current auth rules

---

## Spec Kit Workflow For Phase 8

### Specify Prompt

```text
/speckit.specify Create a specification for Phase 8: Authorization Scope Foundation.

This is an existing completed HR backend project.

Do not implement anything.
Do not change source code.
Do not create migrations.

Read the current project context, Phase 7 artifacts, current auth/current-user implementation, employee role model, manager relationship model, and this roadmap file.

The goal of Phase 8 is to define and prepare reusable authorization scope logic for later phases.

Focus on:
- current user context
- role scope
- self scope
- manager team scope
- organization scope
- reusable service/helper expectations
- tests required

Do not include endpoint-specific fixes yet except as examples.
```

### Clarify Prompt

```text
/speckit.clarify Review the Phase 8 specification.

Clarify:
- whether team scope is direct-only or direct + indirect
- what current-user service already exists
- how roles are represented
- where scope checks should live
- which edge cases need tests

Do not implement anything.
```

### Checklist Prompt

```text
/speckit.checklist Create a requirements quality checklist for Phase 8: Authorization Scope Foundation.

Validate clarity, testability, scope boundaries, and consistency with the existing architecture.
```

### Plan Prompt

```text
/speckit.plan Create the technical implementation plan for Phase 8.

Plan only the shared scope foundation.
Do not plan endpoint-specific fixes yet.
```

### Tasks Prompt

```text
/speckit.tasks Generate tasks for Phase 8.

Tasks must be small, ordered, and focused only on current-user/scope foundation and tests.
```

### Analyze Prompt

```text
/speckit.analyze Analyze Phase 8 spec, plan, tasks, constitution, and current code.

Do not implement if there are unresolved critical or medium issues.
```

### Implement Prompt

```text
/speckit.implement Implement Phase 8 only.

Do not implement employee/vacation/trip endpoint changes yet.
Run build/tests/EF checks after implementation.
```

---

## Phase 8 Acceptance Criteria

Phase 8 is complete when:

- current user scope logic is clear and reusable
- team scope behavior is explicit
- scope helper/service tests pass
- no unrelated endpoint behavior changes were made
- build/test/EF checks pass

---

# Phase 9 — Employee Access Scope Hardening

## Phase Goal

Fix employee endpoint visibility and management permissions.

---

## Why This Phase Exists

Manual testing showed that a normal employee can call:

```http
GET /api/employees
```

and see all employees.

This is not valid HR behavior because employee profiles contain internal HR data such as:

- email
- phone number
- birth date
- department
- manager
- status
- notes
- termination/deletion state

A normal employee should not see all employee profiles.

---

## Business Rules Required

## `GET /api/employees`

| Role | Required Behavior |
|---|---|
| `Employee` | `403 Forbidden` |
| `Manager` | team employees only |
| `HRAdministrator` | all employees |
| `SystemAdministrator` | all employees |

Important decision:

- This endpoint is an admin-style list endpoint.
- Normal employees should not receive a self-only list here.
- Normal employees should use `/api/auth/me` or a future self endpoint.

---

## `GET /api/employees/{id}`

| Role | Required Behavior |
|---|---|
| `Employee` | self only |
| `Manager` | self + team only |
| `HRAdministrator` | any employee |
| `SystemAdministrator` | any employee |

---

## `POST /api/employees`

| Role | Required Behavior |
|---|---|
| `Employee` | `403 Forbidden` |
| `Manager` | `403 Forbidden` |
| `HRAdministrator` | allowed |
| `SystemAdministrator` | allowed |

---

## Role Assignment

Role assignment remains:

```text
SystemAdministrator only
```

HRAdministrator must not assign roles unless a future business decision changes this.

---

## In Scope

- employee list filtering/forbidden behavior
- employee detail scope checks
- employee creation role restriction
- role assignment protection confirmation
- tests for each role/scope combination
- update lifecycle guide if it currently says authenticated users can list/create employees

---

## Out of Scope

- public employee directory
- frontend UI
- employee self-service profile editing unless already implemented
- changing DTO response shape unless necessary for security
- compensation/document scope redesign

---

## Tests Required

### Employee Role Tests

- Employee cannot call `GET /api/employees`
- Employee can access self through approved self endpoint such as `/api/auth/me`
- Employee can view own detail only if that is intended
- Employee cannot view another employee detail
- Employee cannot create employee

### Manager Role Tests

- Manager can list team employees only
- Manager cannot see employees outside team
- Manager can view self
- Manager can view direct/indirect report depending on Phase 8 decision
- Manager cannot create employee

### HRAdministrator Tests

- HRAdministrator can list all employees
- HRAdministrator can view any employee
- HRAdministrator can create employee
- HRAdministrator cannot assign roles if SystemAdministrator-only

### SystemAdministrator Tests

- SystemAdministrator can list all employees
- SystemAdministrator can view any employee
- SystemAdministrator can create employee
- SystemAdministrator can assign roles

---

## Spec Kit Workflow For Phase 9

### Specify Prompt

```text
/speckit.specify Create a specification for Phase 9: Employee Access Scope Hardening.

Read AUTHORIZATION_SCOPE_MULTI_PHASE_ROADMAP.md and Phase 8 artifacts first.

Do not implement anything.

The goal is to fix employee endpoint authorization and visibility:
- Employee cannot list all employees
- Manager sees team only
- HR/System see all
- POST /api/employees is HR/System only
- role assignment remains SystemAdministrator only

Focus on functional requirements, user stories, acceptance criteria, and edge cases.
```

### Clarify Prompt

```text
/speckit.clarify Review Phase 9 Employee Access Scope Hardening.

Focus on:
- Manager team scope
- whether Employee can GET own detail or only /api/auth/me
- expected 403 vs 404 behavior
- employee creation permissions
- role assignment permissions
```

### Checklist Prompt

```text
/speckit.checklist Create a requirements quality checklist for Phase 9.
```

### Plan Prompt

```text
/speckit.plan Create the technical plan for Phase 9.

Continue from Phase 8 scope infrastructure.
Do not modify vacations or trips in this phase.
```

### Tasks Prompt

```text
/speckit.tasks Generate actionable tasks for Phase 9 only.

Include tests first, implementation, docs update, validation.
```

### Analyze Prompt

```text
/speckit.analyze Analyze Phase 9 before implementation.
```

### Implement Prompt

```text
/speckit.implement Implement Phase 9 only.

Do not change vacation or trip logic in this phase.
Run build/tests/EF checks.
```

---

## Phase 9 Acceptance Criteria

Phase 9 is complete when:

- Employee cannot list all employees
- Employee cannot see other employee HR profiles
- Manager sees only self/team
- HR/System see all
- POST employee is HR/System only
- role assignment is still SystemAdministrator only
- tests pass
- lifecycle docs reflect new behavior

---

# Phase 10 — Vacation Request Scope Hardening

## Phase Goal

Fix vacation request listing, detail access, creation ownership, HR on-behalf creation, and review permissions.

---

## Why This Phase Exists

Manual testing showed that a normal employee can see all vacation requests.

This is invalid HR behavior.

Vacation requests are private employee records.

---

## Business Rules Required

## `GET /api/vacationrequests`

| Role | Required Behavior |
|---|---|
| `Employee` | own requests only |
| `Manager` | team requests only |
| `HRAdministrator` | all requests |
| `SystemAdministrator` | all requests |

---

## `GET /api/vacationrequests/{id}`

| Role | Required Behavior |
|---|---|
| `Employee` | own request only |
| `Manager` | self + team request only |
| `HRAdministrator` | any request |
| `SystemAdministrator` | any request |

---

## `POST /api/vacationrequests`

| Role | Required Behavior |
|---|---|
| `Employee` | create for self only |
| `Manager` | create for self only |
| `HRAdministrator` | create for any employee |
| `SystemAdministrator` | create for any employee |

Approved business decision:

- HRAdministrator can create a vacation request on behalf of an employee.
- SystemAdministrator can also create on behalf of an employee.
- Manager does not create vacation requests for team members in the current design.
- Employee never creates vacation requests for another employee.

---

## `PUT /api/vacationrequests/{id}/status`

| Role | Required Behavior |
|---|---|
| `Employee` | `403 Forbidden` |
| `Manager` | review team requests only |
| `HRAdministrator` | review any request |
| `SystemAdministrator` | review any request |

Self-review is always forbidden.

This means no user can approve or reject their own vacation request, even if they are Manager, HRAdministrator, or SystemAdministrator.

---

## `DELETE /api/vacationrequests/{id}`

Recommended behavior:

| Role | Required Behavior |
|---|---|
| `Employee` | delete own pending request only |
| `Manager` | no team delete unless already approved by existing rules |
| `HRAdministrator` | delete/cancel pending requests if existing business rules allow |
| `SystemAdministrator` | delete/cancel pending requests if existing business rules allow |

Codex must inspect current delete rules and preserve them unless they conflict with ownership/scope.

---

## Created By Tracking

To support HR on-behalf creation cleanly, the system should track:

- owner of the vacation request
- creator of the vacation request

Preferred model:

```text
EmployeeId = employee who owns the vacation
CreatedByEmployeeId = employee who created the request
```

Examples:

Employee creates own vacation:

```text
EmployeeId = EMP004
CreatedByEmployeeId = EMP004
```

HR creates vacation for employee:

```text
EmployeeId = EMP004
CreatedByEmployeeId = EMP002
```

If the current entity does not support this, Codex must evaluate whether a migration is required.

---

## In Scope

- vacation list scope filtering
- vacation detail scope checks
- vacation creation ownership checks
- HR/System on-behalf vacation creation
- self-review prevention
- manager team review restriction
- possible `CreatedByEmployeeId` migration if approved
- tests
- docs update

---

## Out of Scope

- new vacation types
- notification system
- frontend UI
- changing working day rules unless already broken
- changing vacation balance algorithm unless scope fix reveals a real defect

---

## Tests Required

### Employee Tests

- Employee sees own vacation requests only
- Employee cannot see another employee request
- Employee can create own vacation request
- Employee cannot create vacation for another employee
- Employee cannot review any request

### Manager Tests

- Manager sees team vacation requests only
- Manager cannot see outside-team requests
- Manager can create own vacation request
- Manager cannot create request for team member
- Manager can approve/reject team request
- Manager cannot approve/reject outside-team request
- Manager cannot review own request

### HRAdministrator Tests

- HRAdministrator sees all vacation requests
- HRAdministrator can create request for any employee
- HRAdministrator cannot review own request

### SystemAdministrator Tests

- SystemAdministrator sees all vacation requests
- SystemAdministrator can create request for any employee
- SystemAdministrator cannot review own request

---

## Spec Kit Workflow For Phase 10

### Specify Prompt

```text
/speckit.specify Create a specification for Phase 10: Vacation Request Scope Hardening.

Read AUTHORIZATION_SCOPE_MULTI_PHASE_ROADMAP.md and Phase 8/9 artifacts first.

Do not implement anything.

The goal is to fix vacation request business logic:
- Employee own-only visibility
- Manager team-only visibility/review
- HR/System all visibility
- HR/System on-behalf vacation creation
- Employee/Manager self-only vacation creation
- self-review blocked for all roles
- possible CreatedByEmployeeId tracking
```

### Clarify Prompt

```text
/speckit.clarify Review Phase 10.

Clarify:
- whether CreatedByEmployeeId is needed
- migration/backfill strategy if needed
- delete behavior
- 403 vs 404 for out-of-scope requests
- manager team scope from Phase 8
```

### Checklist Prompt

```text
/speckit.checklist Create a requirements quality checklist for Phase 10.
```

### Plan Prompt

```text
/speckit.plan Create the technical plan for Phase 10.

Do not modify employee or trip behavior except where shared scope helpers are reused.
```

### Tasks Prompt

```text
/speckit.tasks Generate actionable tasks for Phase 10 only.
```

### Analyze Prompt

```text
/speckit.analyze Analyze Phase 10 before implementation.
```

### Implement Prompt

```text
/speckit.implement Implement Phase 10 only.

Do not implement trip hardening in this phase.
Run build/tests/EF checks.
```

---

## Phase 10 Acceptance Criteria

Phase 10 is complete when:

- Employee sees only own vacation requests
- Manager sees only team vacation requests
- HR/System see all vacation requests
- Employee cannot create request for another employee
- Manager cannot create request for another employee
- HR/System can create on behalf
- self-review is blocked
- tests pass
- docs updated

---

# Phase 11 — Trips Ownership and Scope Hardening

## Phase Goal

Fix trip visibility, trip creation ownership, manager team trip creation, and requester/traveler distinction.

---

## Why This Phase Exists

Manual testing showed that any employee can create a trip and set `requestedByEmployeeId` to another employee.

This allows impersonation.

Also, employees can see all trips, which leaks operational HR/business data.

---

## Business Rules Required

## `GET /api/trips`

| Role | Required Behavior |
|---|---|
| `Employee` | own trips only |
| `Manager` | own + team trips only |
| `HRAdministrator` | all trips |
| `SystemAdministrator` | all trips |

---

## `GET /api/trips/{id}`

| Role | Required Behavior |
|---|---|
| `Employee` | own trip only |
| `Manager` | own + team trip only |
| `HRAdministrator` | any trip |
| `SystemAdministrator` | any trip |

---

## `POST /api/trips`

| Role | Required Behavior |
|---|---|
| `Employee` | create trip for self only |
| `Manager` | create trip for self or team member |
| `HRAdministrator` | create trip for any employee |
| `SystemAdministrator` | create trip for any employee |

Approved business decision:

- Manager can create trips for self and team members.
- HR/System can create trips for any active employee.
- Employee can only create trips for self.

---

## `DELETE /api/trips/{id}`

Recommended behavior:

| Role | Required Behavior |
|---|---|
| `Employee` | delete own trip only if delete is allowed |
| `Manager` | delete own/team trip only if delete is allowed |
| `HRAdministrator` | delete any trip if delete is allowed |
| `SystemAdministrator` | delete any trip if delete is allowed |

Codex must inspect current delete behavior and preserve it unless it conflicts with ownership/scope.

---

## Traveler vs Requester

The system must distinguish:

1. The employee the trip belongs to
2. The employee who created/requested the trip

Preferred model:

```text
TravelerEmployeeId = employee who will take the trip
RequestedByEmployeeId = employee who created the request
```

Examples:

Employee creates own trip:

```text
TravelerEmployeeId = EMP004
RequestedByEmployeeId = EMP004
```

Manager creates trip for team member:

```text
TravelerEmployeeId = EMP004
RequestedByEmployeeId = EMP003
```

HR creates trip for employee:

```text
TravelerEmployeeId = EMP004
RequestedByEmployeeId = EMP002
```

If the current entity has only `RequestedByEmployeeId`, Codex must analyze whether to add `TravelerEmployeeId`.

---

## Request Body Rule

The API must not allow an employee to impersonate another employee.

If the request body contains a target employee ID:

- Employee can only target self
- Manager can target self/team only
- HR/System can target any active employee

If the request body currently uses `requestedByEmployeeId` as the target employee, Codex must decide whether to:

1. keep request body stable and reinterpret it safely, or
2. introduce a clearer DTO field such as `travelerEmployeeId`

Do not break routes unless necessary.

Avoid breaking response shapes unless required for clarity/security.

---

## In Scope

- trip list filtering
- trip detail scope
- trip create ownership checks
- manager team trip creation
- HR/System any employee trip creation
- requester/traveler distinction
- possible migration if approved
- tests
- docs update

---

## Out of Scope

- trip approval workflow if it does not currently exist
- new trip status workflow
- frontend UI
- notification system
- transportation billing
- changing unrelated trip validation rules

---

## Tests Required

### Employee Tests

- Employee sees own trips only
- Employee cannot see another employee trip
- Employee can create trip for self
- Employee cannot create trip for another employee

### Manager Tests

- Manager sees own + team trips only
- Manager cannot see outside-team trips
- Manager can create trip for self
- Manager can create trip for team member
- Manager cannot create trip for outside employee

### HRAdministrator Tests

- HRAdministrator sees all trips
- HRAdministrator can create trip for any active employee

### SystemAdministrator Tests

- SystemAdministrator sees all trips
- SystemAdministrator can create trip for any active employee

### Ownership Tests

- trip response correctly identifies traveler/requester if supported
- requester is current authenticated actor
- traveler is target employee
- employee cannot spoof requester

---

## Spec Kit Workflow For Phase 11

### Specify Prompt

```text
/speckit.specify Create a specification for Phase 11: Trips Ownership and Scope Hardening.

Read AUTHORIZATION_SCOPE_MULTI_PHASE_ROADMAP.md and previous phase artifacts first.

Do not implement anything.

The goal is to fix trip business logic:
- Employee own-only trip visibility/create
- Manager own/team trip visibility/create
- HR/System all trip visibility/create
- prevent requestedByEmployeeId impersonation
- distinguish traveler from requester if schema requires it
```

### Clarify Prompt

```text
/speckit.clarify Review Phase 11.

Clarify:
- whether current Trip entity can represent traveler vs requester
- whether TravelerEmployeeId migration is needed
- how to backfill existing trips if migration is needed
- whether request DTO should keep requestedByEmployeeId or introduce travelerEmployeeId
- 403 vs 404 for out-of-scope trip access
```

### Checklist Prompt

```text
/speckit.checklist Create a requirements quality checklist for Phase 11.
```

### Plan Prompt

```text
/speckit.plan Create the technical plan for Phase 11.

Do not modify vacation logic in this phase.
```

### Tasks Prompt

```text
/speckit.tasks Generate actionable tasks for Phase 11 only.
```

### Analyze Prompt

```text
/speckit.analyze Analyze Phase 11 before implementation.
```

### Implement Prompt

```text
/speckit.implement Implement Phase 11 only.

Do not implement Swagger documentation pass in this phase.
Run build/tests/EF checks.
```

---

## Phase 11 Acceptance Criteria

Phase 11 is complete when:

- Employee sees only own trips
- Manager sees own + team trips only
- HR/System see all trips
- Employee cannot create trip for another employee
- Manager can create trip for team member
- Manager cannot create trip for outside employee
- HR/System can create trip for any active employee
- requester/traveler ownership is clear
- tests pass
- docs updated

---

# Phase 12 — End-to-End Lifecycle Documentation and Manual Retest

## Phase Goal

Update the API lifecycle testing guide and retest the full business flow after the scope fixes.

---

## Why This Phase Exists

The current lifecycle guide was created before the full scope hardening decisions.

After Phases 8–11, the testing order and expected results must be updated.

The client/developer guide must clearly explain:

- which role should call which endpoint
- when 403 is expected
- how employee, vacation, and trip scopes work
- how to test the fixed lifecycle on a fresh database

---

## In Scope

Update:

- `API_LIFECYCLE_TESTING_GUIDE.md`
- `CLIENT_INSTALLATION_GUIDE.md` if it mentions endpoint permissions
- implementation summary or handoff notes
- manual test checklist

---

## Required Documentation Updates

The guide must reflect:

### Employees

- Employee cannot list all employees
- Manager sees team only
- HR/System see all
- Employee creation is HR/System only

### Vacations

- Employee sees own only
- Manager sees team only
- HR/System see all
- HR/System can create vacation for employee
- Employee/Manager can create vacation for self only
- self-review blocked

### Trips

- Employee sees own only
- Manager sees own/team only
- HR/System see all
- Employee creates trip for self only
- Manager creates trip for self/team only
- HR/System creates trip for any employee
- requester/traveler behavior explained

---

## Manual Retest Dataset

Use a fresh database and these fixed test users:

| Employee Number | Email | Role |
|---|---|---|
| `EMP001` | `admin@test.com` | `SystemAdministrator` |
| `EMP002` | `hr.admin@test.com` | `HRAdministrator` |
| `EMP003` | `manager@test.com` | `Manager` |
| `EMP004` | `employee@test.com` | `Employee` |

Suggested local-only passwords:

| User | Password |
|---|---|
| System Admin | `Admin1234` |
| HR Admin | `HrAdmin123` |
| Manager | `Manager123` |
| Employee | `Employee123` |

---

## Required Manual Retest Scenarios

### Employee

- cannot call `GET /api/employees`
- cannot view another employee detail
- sees only own vacation requests
- creates vacation only for self
- sees only own trips
- creates trip only for self
- cannot access audit logs
- cannot access compensation

### Manager

- sees only team employees
- cannot see outside-team employees
- sees team vacation requests
- approves/rejects team vacation requests
- cannot self-review
- sees own + team trips
- creates trip for team member
- cannot create trip for outside employee
- cannot access audit logs

### HRAdministrator

- sees all employees
- creates employees
- sees all vacation requests
- creates vacation request for employee
- sees all trips
- creates trip for any employee
- sees audit logs
- cannot assign roles if SystemAdministrator-only

### SystemAdministrator

- full access
- can assign roles
- cannot self-review vacation

---

## Spec Kit Workflow For Phase 12

### Specify Prompt

```text
/speckit.specify Create a specification for Phase 12: Lifecycle Documentation and Manual Retest.

Do not implement business logic.

The goal is to update testing documentation after the authorization scope hardening phases and define the manual retest flow for a fresh database.
```

### Plan/Tasks Prompt

```text
/speckit.plan Create a documentation and manual retest plan for Phase 12.

Then create tasks for updating guides and running manual verification.
```

### Implement Prompt

```text
/speckit.implement Implement Phase 12 documentation updates only.

Do not change source code unless a documentation test reveals a real mismatch and I approve.
```

---

## Phase 12 Acceptance Criteria

Phase 12 is complete when:

- lifecycle guide matches the new scope rules
- fresh database manual retest steps are updated
- expected 401/403/409/422 cases are documented
- no stale “authenticated user can access everything” wording remains

---

# Phase 13 — Swagger/OpenAPI Response Documentation Pass

## Phase Goal

Fix Swagger `Undocumented` responses.

---

## Why This Phase Exists

Manual Swagger testing showed:

- endpoint returned correct status code such as `201 Created`
- Swagger displayed the response as `Undocumented`

This is not a business-logic bug, but it hurts API usability and client integration.

Swagger should clearly document common success and error responses.

---

## Required Behavior

Add accurate response documentation annotations to controllers so Swagger shows documented responses instead of `Undocumented`.

This phase must not change business behavior.

---

## Controllers To Review

- AuthController
- EmployeesController
- DepartmentsController
- AttendanceController
- VacationRequestsController
- TripsController
- CompensationController
- EmployeeDocumentsController
- DashboardController
- AuditLogsController

---

## Response Codes To Document

Success responses:

- `200 OK`
- `201 Created`
- `204 No Content`

Error responses:

- `400 Bad Request`
- `401 Unauthorized`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`
- `413 Payload Too Large`
- `422 Unprocessable Entity`

Use actual DTO types from the project.

Examples:

- success DTOs
- paged list DTOs
- error response DTOs
- file/download response where applicable

---

## In Scope

- `ProducesResponseType` or equivalent Swagger/OpenAPI annotations
- accurate success response documentation
- accurate error response documentation
- file response documentation if applicable
- verification in Swagger UI

---

## Out of Scope

- changing route names
- changing status codes
- changing response JSON shape
- changing business logic
- changing validation logic
- changing authentication

---

## Tests/Validation Required

At minimum:

```powershell
dotnet build .\HR.slnx -c Release
dotnet test .\HR.slnx -c Release
```

Manual Swagger check:

- `POST /api/attendance/clock-in` no longer shows `201` as undocumented
- common endpoints show documented 401/403/409/422 where applicable
- document upload endpoint documents 413 if implemented
- no routes disappeared from Swagger

---

## Spec Kit Workflow For Phase 13

### Specify Prompt

```text
/speckit.specify Create a specification for Phase 13: Swagger Response Documentation Pass.

Do not implement business logic.

The goal is to document accurate controller response codes so Swagger no longer shows common responses as Undocumented.
```

### Plan Prompt

```text
/speckit.plan Create a technical plan for Phase 13.

Inspect all controllers and define which response codes and DTOs should be documented.
No route or behavior changes.
```

### Tasks Prompt

```text
/speckit.tasks Generate tasks for Phase 13.

Tasks should be grouped by controller.
```

### Analyze Prompt

```text
/speckit.analyze Analyze Phase 13 before implementation.

Confirm it does not change business behavior.
```

### Implement Prompt

```text
/speckit.implement Implement Phase 13 only.

Only add accurate Swagger/OpenAPI response documentation annotations.
Do not change logic/routes/status codes/DTO shapes.
Run build/tests.
```

---

## Phase 13 Acceptance Criteria

Phase 13 is complete when:

- common Swagger responses are documented
- no known successful endpoint shows `Undocumented` for expected success status
- expected error responses are documented
- build/tests pass
- no business logic changed

---

# Final Roadmap Summary

| Phase | Name | Main Goal |
|---|---|---|
| Phase 8 | Authorization Scope Foundation | Build/confirm reusable current-user and team-scope logic |
| Phase 9 | Employee Access Scope Hardening | Fix employee list/detail/create/role access |
| Phase 10 | Vacation Request Scope Hardening | Fix vacation visibility, ownership, HR on-behalf creation, review rules |
| Phase 11 | Trips Ownership and Scope Hardening | Fix trip visibility, creation ownership, manager team creation, traveler/requester distinction |
| Phase 12 | Lifecycle Documentation and Manual Retest | Update guides and retest fresh database lifecycle |
| Phase 13 | Swagger/OpenAPI Response Documentation Pass | Fix `Undocumented` Swagger responses |

---

# Recommended First Codex Prompt

Use this from the project root.

```text
Read AUTHORIZATION_SCOPE_MULTI_PHASE_ROADMAP.md first.

This file is the source of truth for the remaining HR backend remediation work.

Do not implement anything yet.

Start with Phase 8 only: Authorization Scope Foundation.

Before doing anything, inspect the existing project context:
- CODEX_HANDOFF.md if it exists
- README.md if it exists
- AGENTS.md if it exists
- .specify/memory/constitution.md
- existing specs folder
- Phase 7 spec/plan/tasks/implementation summary
- API_LIFECYCLE_TESTING_GUIDE.md
- CLIENT_INSTALLATION_GUIDE.md
- controllers
- application services/contracts
- infrastructure services/repositories
- domain entities
- tests

Then create a Spec Kit specification for Phase 8 only.

Do not modify source code.
Do not create migrations.
Do not implement employee/vacation/trip fixes yet.
Do not start Phase 9.

After creating the Phase 8 specification, summarize:
1. Spec file path
2. Main scope-foundation requirements
3. Team scope decision or open question
4. Current-user mechanism found
5. Any unclear points
6. Recommended next Spec Kit command
```

---

# Important Final Notes

- Do not let Codex implement multiple phases at once.
- Do not let Codex create migrations without approval.
- Do not let Codex fix Swagger documentation before business logic is complete.
- Do not let Codex add a public employee directory in these phases.
- Do not let Codex redesign authentication.
- Do not let Codex switch cookie auth to JWT.
- Stop after each phase and review before continuing.
