# HR Backend Authorization Scope Hardening Plan

## Purpose

This document is the reference plan for fixing the current business-logic authorization gaps in the HR backend.

The project is already implemented and tested, but manual API testing revealed that some authenticated users can access data outside their real HR scope.

This is not only a technical authorization issue. It is a business logic issue.

The goal is to make the system behave like a real HR system by enforcing:

- role-based access
- ownership checks
- team-scoped access
- HR/Admin organization-wide access
- safe on-behalf actions
- accurate Swagger/OpenAPI response documentation

This document is intentionally written as business logic and implementation guidance, not low-level syntax.

---

## Critical Problems Found

### 1. Employees Visibility Problem

Current observed problem:

- A normal employee can call `GET /api/employees`
- The employee can see all employees
- This exposes HR data that should not be visible to normal employees

This is not acceptable in a production HR system.

---

### 2. Vacation Requests Visibility Problem

Current observed problem:

- A normal employee can call `GET /api/vacationrequests`
- The employee can see all vacation requests for all employees

Correct behavior:

- Employee sees only their own vacation requests
- Manager sees only their team's vacation requests
- HR Administrator and System Administrator see all vacation requests

---

### 3. Trips Ownership Problem

Current observed problem:

- Any employee can call `POST /api/trips`
- The employee can set `requestedByEmployeeId` to another employee
- Any employee can call `GET /api/trips` and see all trips

This allows impersonation and data leakage.

Correct behavior:

- Employee creates trips only for self
- Manager creates trips for self and team members only
- HR Administrator and System Administrator create trips for any employee
- Trip list/get endpoints return only data inside the current user's scope

---

### 4. Swagger Undocumented Responses Problem

Current observed problem:

- Some endpoints return correct status codes such as `201 Created`
- Swagger shows those responses as `Undocumented`

Correct behavior:

- Controllers should document accurate success and error response status codes
- Swagger should clearly show expected responses instead of `Undocumented`

---

## Core Business Rule

Every request must answer these questions:

1. Who is the current authenticated user?
2. What is their role?
3. What is their employee ID?
4. Is the target data owned by them?
5. Is the target data inside their team scope?
6. Are they HR/System Admin with organization-wide permission?

`[Authorize]` alone is not enough.

A logged-in employee is authenticated, but that does not mean they can access all HR data.

---

## Role Scope Model

### Employee

A normal employee has self-service access only.

Allowed:

- view own authenticated profile
- view own vacation requests
- create vacation request for self
- view own trips
- create trip for self
- use own attendance endpoints

Not allowed:

- list all employees
- view other employees' HR profiles
- view all vacation requests
- create vacation request for another employee
- view all trips
- create trip for another employee
- view audit logs
- view compensation
- assign roles
- create employees

---

### Manager

A manager has access to self and team-scoped data.

Team scope should preferably mean:

- direct reports
- indirect reports, if the project supports hierarchy traversal safely

If indirect reports are not currently implemented, Codex must document whether the first implementation uses direct reports only or implements direct + indirect team scope.

Allowed:

- view own profile
- view team employees
- view own and team vacation requests
- review team vacation requests
- view own and team trips
- create trips for self and team members
- view team dashboard
- view team attendance if already supported

Not allowed:

- list all employees outside their team
- view other departments outside their scope
- create employees
- assign roles
- view audit logs
- view compensation unless explicitly required by future business rules
- review their own vacation request
- create vacation request for another employee in the current approved design

---

### HRAdministrator

HR Administrator has organization-wide HR operational access.

Allowed:

- list all employees
- view any employee profile
- create employees
- update employee HR data if already supported
- view all vacation requests
- create vacation request on behalf of any employee
- review vacation requests except self-review
- view all trips
- create trips for any employee
- manage compensation if already implemented
- manage documents if already implemented
- view audit logs if already implemented
- view organization dashboard

Not allowed:

- assign SystemAdministrator role unless current business decision allows it
- review their own vacation request

Current approved decision:

- role assignment remains SystemAdministrator only

---

### SystemAdministrator

System Administrator has full organization-wide and system-level access.

Allowed:

- all HR Administrator capabilities
- assign roles
- system setup/bootstrap responsibility
- organization-wide dashboard and audit access

Still not allowed:

- self-review of own vacation request

---

## Approved Access Matrix

## Employees

| Endpoint | Employee | Manager | HRAdministrator | SystemAdministrator |
|---|---|---|---|---|
| `GET /api/employees` | `403 Forbidden` | team only | all employees | all employees |
| `GET /api/employees/{id}` | self only | self + team only | any employee | any employee |
| `POST /api/employees` | `403 Forbidden` | `403 Forbidden` | allowed | allowed |
| employee update/delete management endpoints | `403 Forbidden`, unless explicit self-service exists | `403 Forbidden`, unless explicit team action exists | allowed | allowed |
| `PUT /api/employees/{id}/role` | `403 Forbidden` | `403 Forbidden` | `403 Forbidden` | allowed |

---

## Vacation Requests

| Endpoint | Employee | Manager | HRAdministrator | SystemAdministrator |
|---|---|---|---|---|
| `GET /api/vacationrequests` | own requests only | team requests only | all requests | all requests |
| `GET /api/vacationrequests/{id}` | own request only | self + team requests only | any request | any request |
| `POST /api/vacationrequests` | own request only | own request only | can create for any employee | can create for any employee |
| `PUT /api/vacationrequests/{id}/status` | `403 Forbidden` | team requests only, no self-review | any request, no self-review | any request, no self-review |
| `DELETE /api/vacationrequests/{id}` | own pending request only | no team delete unless explicitly approved | pending requests if allowed by existing rules | pending requests if allowed by existing rules |

### Vacation On-Behalf Rule

HR Administrator and System Administrator can create a vacation request for another employee.

This is needed for real HR workflows such as:

- sick leave submitted manually
- HR entering a request on behalf of an employee
- administrative correction

The system should record who created the request.

Preferred model:

- `EmployeeId` = employee who owns the vacation
- `CreatedByEmployeeId` = employee who created the request

Examples:

Employee creates own request:

- `EmployeeId = EMP004`
- `CreatedByEmployeeId = EMP004`

HR creates request for employee:

- `EmployeeId = EMP004`
- `CreatedByEmployeeId = EMP002`

If this field does not exist, Codex must evaluate whether a migration is required.

---

## Trips

| Endpoint | Employee | Manager | HRAdministrator | SystemAdministrator |
|---|---|---|---|---|
| `GET /api/trips` | own trips only | own + team trips only | all trips | all trips |
| `GET /api/trips/{id}` | own trip only | own + team trip only | any trip | any trip |
| `POST /api/trips` | own trip only | own + team trip only | can create for any employee | can create for any employee |
| `DELETE /api/trips/{id}` | own trip only if delete is allowed | own + team trip only if delete is allowed | any trip if delete is allowed | any trip if delete is allowed |

### Trip Ownership Rule

The current trip model is not clear enough if it only has:

- `requestedByEmployeeId`

A real HR system must distinguish between:

1. The employee the trip belongs to
2. The employee who created/requested the trip

Preferred model:

- `TravelerEmployeeId` = employee who will take the trip
- `RequestedByEmployeeId` = employee who created the trip request

Examples:

Employee creates own trip:

- `TravelerEmployeeId = EMP004`
- `RequestedByEmployeeId = EMP004`

Manager creates trip for team member:

- `TravelerEmployeeId = EMP004`
- `RequestedByEmployeeId = EMP003`

HR creates trip for employee:

- `TravelerEmployeeId = EMP004`
- `RequestedByEmployeeId = EMP002`

If the current entity does not support this distinction, Codex must analyze whether a schema migration is required.

---

## Migration Policy

Migrations are allowed only if there is a proven schema/model gap.

Codex must not create a migration automatically without first reporting:

1. The exact business rule that cannot be represented by the current schema
2. Why a code-only fix is not correct
3. The exact table/columns/relationships/indexes that must change
4. Whether existing data needs backfill
5. Whether the migration is required by this plan
6. The migration name it intends to create

Likely schema changes that may be needed:

### Vacation Requests

Possible addition:

- `CreatedByEmployeeId`

Purpose:

- distinguish request owner from request creator

### Trips

Possible addition:

- `TravelerEmployeeId`

Purpose:

- distinguish trip owner/traveler from trip requester/creator

If existing fields already support these concepts clearly, Codex should reuse them and avoid unnecessary migrations.

---

## Implementation Principle

Do not fix this by controller attributes only.

Controller-level roles help, but they do not solve data scope.

The scope checks must exist in the application/service/query layer so that:

- list queries are filtered correctly
- detail queries reject out-of-scope records
- create/update/delete operations verify target ownership/team access
- tests can validate behavior without relying only on HTTP attributes

Recommended service logic inputs:

- current employee ID
- current role
- target employee ID
- manager/team relationship
- requested action
- target entity owner/traveler/request owner

---

# Spec Kit Execution Plan

This work must be handled as a structured Spec Kit remediation phase.

Suggested phase name:

`Authorization Scope Hardening`

Suggested feature folder name:

`008-authorization-scope-hardening`

Do not jump directly into implementation.

Follow this order:

1. specify
2. clarify
3. checklist
4. plan
5. tasks
6. analyze
7. implement by stage
8. review
9. documentation update
10. Swagger response documentation pass

---

# Stage 0 — Context Review

## Goal

Codex must first inspect the current project and confirm the actual implemented behavior.

## Codex Should Read

- `CODEX_HANDOFF.md` if it exists
- `README.md` if it exists
- `AGENTS.md` if it exists
- `.specify/memory/constitution.md`
- existing specs folder
- Phase 7 spec/plan/tasks/implementation summary
- `API_LIFECYCLE_TESTING_GUIDE.md`
- `CLIENT_INSTALLATION_GUIDE.md`
- controllers in `HR.API`
- services/contracts in `HR.Application`
- repositories/services in `HR.Infrastructure`
- domain entities in `HR.Domain`
- tests in `HR.Tests`

## Codex Should Confirm

- Which endpoints currently leak all employees/vacations/trips
- How current user identity is resolved
- How roles are represented
- Whether manager team traversal already exists
- Whether vacation requests have a creator field
- Whether trips distinguish traveler from requester
- Whether a migration is required

## Output

Codex should summarize:

1. Current employee endpoint behavior
2. Current vacation request behavior
3. Current trip behavior
4. Current role/scope infrastructure
5. Current entity gaps
6. Whether migration appears necessary
7. Recommended next Spec Kit command

No source code changes in this stage.

---

# Stage 1 — Specify

## Goal

Create a clear functional specification for Authorization Scope Hardening.

## Required Spec Content

The spec must include:

1. Problem statement
2. Business reason
3. User stories for each role
4. Employees access rules
5. Vacation request access rules
6. Trip access rules
7. On-behalf vacation creation rules
8. Trip traveler/requester ownership rules
9. Manager team scope definition
10. Self-review prevention
11. Out-of-scope items
12. Acceptance criteria
13. Edge cases
14. Security expectations
15. Error/status code expectations

## Required User Stories

### Employee

- As an Employee, I can see only my own vacation requests.
- As an Employee, I can create a vacation request only for myself.
- As an Employee, I can see only my own trips.
- As an Employee, I can create a trip only for myself.
- As an Employee, I cannot list all employees.
- As an Employee, I cannot view another employee's HR profile.

### Manager

- As a Manager, I can view employees in my team.
- As a Manager, I can view vacation requests for my team.
- As a Manager, I can review vacation requests for my team.
- As a Manager, I cannot review my own vacation request.
- As a Manager, I can create trips for myself and my team.
- As a Manager, I cannot create trips for employees outside my team.

### HRAdministrator

- As an HRAdministrator, I can list and view all employees.
- As an HRAdministrator, I can create employees.
- As an HRAdministrator, I can create vacation requests on behalf of employees.
- As an HRAdministrator, I can view all vacation requests.
- As an HRAdministrator, I can create trips for any employee.
- As an HRAdministrator, I cannot review my own vacation request.
- As an HRAdministrator, I cannot assign roles if role assignment is SystemAdministrator-only.

### SystemAdministrator

- As a SystemAdministrator, I can perform organization-wide employee, vacation, and trip operations.
- As a SystemAdministrator, I can assign roles.
- As a SystemAdministrator, I cannot review my own vacation request.

## Out of Scope

Do not implement these unless already present:

- new frontend UI
- employee public directory
- trip approval workflow if it does not currently exist
- compensation visibility redesign
- document self-service redesign
- new notification system
- major auth rewrite
- changing cookie authentication to JWT

---

# Stage 2 — Clarify

## Goal

Find and resolve ambiguity before planning.

## Questions Codex Should Answer or Ask

1. Does team scope mean direct reports only or direct + indirect reports?
2. Does the current system already support recursive manager hierarchy?
3. Should employee list for Manager include the manager's own profile?
4. Should `GET /api/employees` return 403 for Employee or return only self?
   - Approved decision: return 403 for the admin-style list endpoint.
5. Should `POST /api/vacationrequests` keep `employeeId` in the request body?
   - Approved behavior: Employees and Managers can only use their own employee ID. HR/System can use any valid employee ID.
6. Does VacationRequest need `CreatedByEmployeeId`?
7. Does Trip need `TravelerEmployeeId`?
8. What should happen to old trips if `TravelerEmployeeId` is added?
9. Should Manager create vacation requests for team members?
   - Approved current decision: no. Manager creates vacation requests for self only.
10. Should Manager delete trips for team members?
    - Use the same visibility scope if delete already exists, unless existing rules are stricter.
11. What status code should out-of-scope access return?
    - Prefer `403 Forbidden` for authenticated users without permission.
    - Use `404 Not Found` only if the project intentionally hides existence consistently.

## Output

Codex should produce clarification answers and update the spec if needed.

No implementation in this stage.

---

# Stage 3 — Checklist

## Goal

Create a requirements quality checklist.

## Checklist Must Validate

- Employee access rules are explicit
- Manager team scope is explicit
- HR/Admin scope is explicit
- Vacation on-behalf creation is explicit
- Trip traveler/requester distinction is explicit
- Status codes are explicit
- Migration requirements are identified
- Tests are required for every role/scope combination
- No hidden assumptions remain
- Swagger documentation pass is included as final stage
- Out-of-scope items are clear

## Output

Codex should show:

1. Passed checklist items
2. Failed/weak items
3. Required fixes before planning
4. Whether planning is safe

No implementation in this stage.

---

# Stage 4 — Plan

## Goal

Create the technical implementation plan.

## Plan Must Cover

1. Current auth/current-user mechanism
2. Scope-check strategy
3. Team-scope query strategy
4. Employees endpoint changes
5. Vacation endpoint changes
6. Trip endpoint changes
7. Entity/model changes if needed
8. Migration/backfill strategy if needed
9. DTO changes if needed
10. Error handling/status code strategy
11. Audit impact
12. Test strategy
13. Swagger documentation pass strategy
14. Documentation update strategy

## Important Plan Rules

- Do not change routes unless absolutely required.
- Do not weaken existing validation.
- Do not expose more fields to normal employees.
- Keep HR/System organization-wide behavior.
- Keep role assignment SystemAdministrator-only.
- Keep response shape stable unless a security issue requires change.
- Prefer service-layer scope enforcement.

## Output

Codex should summarize:

1. Files likely to change
2. Whether migration is required
3. Risk areas
4. Testing plan
5. Recommended task breakdown

No implementation in this stage.

---

# Stage 5 — Tasks

## Goal

Generate actionable tasks grouped by implementation stage.

Tasks must be small and ordered.

## Required Task Groups

### Group A — Tests First / Baseline Reproduction

Create failing or focused tests proving current gaps:

- Employee can currently list all employees
- Employee can currently see all vacation requests
- Employee can currently see all trips
- Employee can currently create trip for another employee
- Employee can currently create vacation request for another employee, if applicable
- Manager scope tests
- HR/System allowed tests

These tests document the defect before fixing it.

---

### Group B — Shared Scope Infrastructure

Tasks may include:

- current user context improvements
- role/scope helper
- team membership helper
- direct/indirect report query helper
- reusable authorization/scope methods in application layer

No endpoint-specific business changes yet.

---

### Group C — Employees Scope Fix

Implement:

- `GET /api/employees`
  - Employee = 403
  - Manager = team only
  - HR/System = all

- `GET /api/employees/{id}`
  - Employee = self only
  - Manager = self + team
  - HR/System = all

- `POST /api/employees`
  - HR/System only

- role assignment remains SystemAdministrator only

---

### Group D — Vacation Scope Fix

Implement:

- list filtering by role
- detail scope by role
- employee self-only create
- manager self-only create
- HR/System on-behalf create
- created-by tracking if schema supports it or migration is approved
- manager review team only
- self-review blocked for all roles
- employee cannot review

---

### Group E — Trips Scope Fix

Implement:

- list filtering by role
- detail scope by role
- employee self-only trip create
- manager self/team trip create
- HR/System any employee trip create
- traveler/requester distinction if schema supports it or migration is approved
- delete scope, following existing delete semantics

---

### Group F — Migration and Backfill, If Needed

Only if approved.

Possible migration tasks:

- add `VacationRequests.CreatedByEmployeeId`
- add `Trips.TravelerEmployeeId`
- backfill existing vacation created-by data safely
- backfill existing trip traveler data safely
- update EF configuration
- update DTO mapping
- update tests

---

### Group G — End-to-End Authorization Tests

Tests must prove:

#### Employees

- Employee cannot list all employees
- Employee can view self only
- Employee cannot view another employee
- Manager can view team employees
- Manager cannot view outside team
- HR/Admin can view all
- Employee/Manager cannot create employee
- HR/System can create employee

#### Vacation

- Employee list returns own only
- Manager list returns team only
- HR/System list returns all
- Employee cannot create vacation for another employee
- HR/System can create vacation for another employee
- Manager cannot create vacation for another employee
- Manager can review team vacation
- Manager cannot review outside team vacation
- Employee cannot review any vacation
- Self-review is blocked

#### Trips

- Employee list returns own only
- Manager list returns own + team only
- HR/System list returns all
- Employee cannot create trip for another employee
- Manager can create trip for self
- Manager can create trip for team member
- Manager cannot create trip for outside employee
- HR/System can create trip for any active employee
- Trip response clearly identifies traveler/requester if supported

---

### Group H — Documentation Update

Update:

- `API_LIFECYCLE_TESTING_GUIDE.md`
- `CLIENT_INSTALLATION_GUIDE.md`, if it mentions endpoint permissions
- Phase spec/plan/tasks
- implementation summary
- any quickstart docs

Documentation must reflect the final access matrix.

---

### Group I — Swagger Response Documentation Pass

This is the final stage after logic is correct.

Goal:

Add accurate response documentation annotations to controllers so Swagger shows documented responses instead of `Undocumented`.

Controllers to review:

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

For each endpoint, document accurate response codes.

Success examples:

- `200 OK`
- `201 Created`
- `204 No Content`

Error examples:

- `400 Bad Request`
- `401 Unauthorized`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`
- `413 Payload Too Large`
- `422 Unprocessable Entity`

Use the actual response DTOs and error response DTOs from the project.

Do not change business logic in this stage.

---

# Stage 6 — Analyze

## Goal

Run Spec Kit analyze before implementation.

Codex must check:

1. Requirements missing from plan
2. Requirements missing from tasks
3. Tasks not backed by spec
4. Migration risk
5. Missing tests
6. Missing edge cases
7. Phase scope leakage
8. Conflicts with existing architecture
9. Over-engineering
10. Swagger documentation pass included but separated from business logic

## Output

Codex should report:

- Critical issues
- Medium issues
- Low-priority suggestions
- Whether implementation is safe to start

If critical or medium issues exist, do not implement yet.

---

# Stage 7 — Implementation Strategy

## Goal

Implement in small stages.

Do not implement the whole remediation at once.

Recommended order:

1. Shared scope infrastructure
2. Employees scope
3. Vacation scope
4. Trips scope
5. End-to-end authorization tests
6. Documentation update
7. Swagger response documentation pass

After each stage, Codex must stop and report results.

## Per-Stage Output Required

After each implementation stage, Codex must show:

1. Completed task IDs
2. Changed files
3. Behavior changed
4. Tests added/updated
5. Test results
6. Whether migration was created
7. Whether any unrelated code changed
8. Recommended next stage

---

# Stage 8 — Validation

After all logic stages are complete, run:

```powershell
dotnet restore .\HR.slnx
dotnet build .\HR.slnx -c Release
dotnet test .\HR.slnx -c Release
dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

If a migration was approved and created, also run:

```powershell
dotnet ef database update --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

Validation must confirm:

- no pending model changes
- all tests pass
- employee scope is enforced
- vacation scope is enforced
- trip scope is enforced
- Swagger no longer shows undocumented common responses
- no unrelated route changes
- no unexpected response shape changes

---

# Stage 9 — Manual API Lifecycle Retest

After automated tests pass, manually test on a fresh database.

Use the existing lifecycle test data:

- `EMP001` System Admin
- `EMP002` HR Admin
- `EMP003` Manager
- `EMP004` Employee

Manual retest must verify:

## Employee

- cannot list all employees
- cannot view another employee
- sees only own vacation requests
- creates vacation only for self
- sees only own trips
- creates trip only for self
- cannot access audit logs
- cannot access compensation

## Manager

- sees only team employees
- cannot see outside-team employees
- sees team vacation requests
- approves/rejects team vacation requests
- cannot self-review
- sees own + team trips
- creates trip for team member
- cannot create trip for outside employee
- cannot access audit logs

## HRAdministrator

- sees all employees
- creates employees
- sees all vacation requests
- creates vacation request for employee
- sees all trips
- creates trip for any employee
- sees audit logs
- cannot assign roles if SystemAdministrator-only

## SystemAdministrator

- full access
- can assign roles
- cannot self-review vacation

---

# Final Acceptance Criteria

This remediation is complete only when:

- Employee cannot see organization-wide employee data
- Employee cannot see all vacation requests
- Employee cannot see all trips
- Employee cannot create vacation/trip for another employee
- Manager scope is enforced
- HR/System organization scope is preserved
- HR can create vacation request on behalf of employee
- Manager can create trip for self/team
- Trips distinguish owner/traveler from creator/requester if required
- Self-review is blocked
- Role assignment remains SystemAdministrator-only
- Tests cover all scope rules
- Swagger documents success/error responses accurately
- Full build/test/EF checks pass
- Lifecycle guide is updated

---

# Suggested Codex Starting Prompt

Use this prompt from the project root.

```text
Read AUTHORIZATION_SCOPE_HARDENING_PLAN.md first.

This is a business-logic remediation phase for the completed HR backend.

Do not implement anything yet.

Start with Spec Kit context review and specification only.

Read the current project context, existing specs, Phase 7 artifacts, controllers, services, entities, tests, and the plan file.

Create or update a new Spec Kit specification for:

Authorization Scope Hardening

The goal is to fix employee/vacation/trip authorization scope gaps and later fix undocumented Swagger responses.

Follow the plan exactly.

Do not change source code.
Do not create migrations.
Do not refactor.

After specification, summarize:
1. spec path
2. main user stories
3. access matrix captured
4. open questions
5. whether migration appears likely
6. recommended next Spec Kit command
```

---

# Notes for Codex

- This plan is the source of truth for the remediation.
- If existing code conflicts with this plan, stop and report.
- If migration is needed, request approval before creating it.
- If a business rule is ambiguous, mark it as `Needs clarification`.
- Do not implement unrelated features.
- Do not redesign authentication.
- Do not change from cookie auth to JWT.
- Do not add frontend work.
- Do not implement a public employee directory in this phase.
- Keep changes small, testable, and stage-based.
