# Feature Specification: Phase 6 - DI Registration Cleanup

**Feature Branch**: `[006-di-registration-cleanup]`

**Created**: 2026-06-04

**Status**: Draft

**Input**: User description: "Now let's start phase 6"

## Clarifications

### Session 2026-06-04

- Q: Where should service implementation registrations live when service contracts are application-owned but current implementations depend on infrastructure concerns? -> A: Application exposes its registration entry point, while infrastructure registers infrastructure-backed service implementations.

## Boundary Definitions and Scope

- **Registration entry point**: A project-owned `DependencyInjection` extension method that groups service registrations owned by that project.
- **Startup surface / startup composition**: `HR.API/Program.cs`, limited to host-owned request handling, authentication cookie wiring, authorization, controller setup, JSON options, CORS, Swagger, middleware order, and endpoint mapping.
- **Owning layer**: The project responsible for a dependency category. `HR.Application` owns application contracts and application-only registrations. `HR.Infrastructure` owns EF Core, Identity stores, repositories, infrastructure-backed service implementations, time/rule helpers, and unit-of-work registrations.
- **Runtime behavior**: Existing routes, request JSON, response JSON, pagination envelopes, cookies, claims, status codes, error codes, session behavior, and Phase 5 business-rule behavior.

Phase 6 is DI registration cleanup only. It must not change runtime behavior, create migrations, add schema objects, add Phase 7 features, add authorization behavior, or move infrastructure-backed implementations into `HR.Application`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Keep Application Startup Readable (Priority: P1)

As a backend maintainer, I want the application's startup composition to delegate dependency registration to the owning layers so that future changes are easier to review and less likely to accidentally mix responsibilities.

**Why this priority**: Startup composition is the highest-impact maintenance surface for this phase. If it remains cluttered with registrations owned by other layers, Phase 6 has not delivered its main value.

**Independent Test**: Can be fully tested by reviewing startup composition after the refactor and confirming it delegates application and infrastructure registration through project-owned entry points while preserving the same runtime behavior.

**Acceptance Scenarios**:

1. **Given** the completed Phase 5 backend, **When** a maintainer reviews startup composition, **Then** application and infrastructure dependencies are grouped behind owning-layer registration entry points instead of being individually registered at the startup surface.
2. **Given** the completed Phase 5 backend, **When** the application starts, **Then** authentication, authorization, serialization, routing, cross-origin access, documentation, and error handling remain available with the same behavior as before.
3. **Given** a maintainer adds a future application or infrastructure dependency, **When** they look for the correct registration location, **Then** the intended owning layer is clear from the registration structure.

---

### User Story 2 - Preserve Existing Runtime Behavior (Priority: P1)

As an HR system user, I want all existing HR endpoints and authentication behavior to keep working after the wiring cleanup so that the refactor does not interrupt normal work.

**Why this priority**: Phase 6 is a wiring-only refactor. It must not change business behavior, database schema, authentication semantics, routes, response shapes, or error payload compatibility.

**Independent Test**: Can be fully tested by running the existing automated suite and a focused manual smoke test for login and representative HR workflows before and after the cleanup.

**Acceptance Scenarios**:

1. **Given** an active employee with valid credentials, **When** the employee signs in after Phase 6, **Then** the same session behavior and login response remain available.
2. **Given** a protected HR action, **When** an authenticated request reaches it after Phase 6, **Then** the system returns the same response shape and status behavior as before the cleanup.
3. **Given** an expected business-rule or compatibility failure, **When** the failure occurs after Phase 6, **Then** the structured error response still includes the same status, code field, and message field behavior.

---

### User Story 3 - Make Registration Ownership Auditable (Priority: P2)

As a reviewer, I want a clear boundary between application-owned registrations, infrastructure-owned registrations, and host-owned request handling concerns so that architecture compliance can be audited quickly.

**Why this priority**: The constitution requires each project to own its dependency registration once Phase 6 is active. The cleanup should make future reviews faster and reduce regression risk.

**Independent Test**: Can be fully tested by running static checks that confirm service and data-access registrations are no longer directly listed at the startup surface and that each owning layer exposes a single registration entry point.

**Acceptance Scenarios**:

1. **Given** the registration cleanup is complete, **When** a reviewer scans the startup surface, **Then** only host-owned request handling, authentication, authorization, serialization, and pipeline concerns are directly composed there.
2. **Given** the registration cleanup is complete, **When** a reviewer scans application registration ownership, **Then** application service contracts are registered by the application layer or an approved owning-layer entry point.
3. **Given** the registration cleanup is complete, **When** a reviewer scans infrastructure registration ownership, **Then** persistence, identity, repository, time, rule-helper, and infrastructure service implementations are registered by the infrastructure layer.

---

### Edge Cases

- Phase 6 must preserve the current session authentication settings, including structured responses for unauthenticated and forbidden requests.
- Phase 6 must preserve the current database connection behavior and must not create, remove, or modify database schema objects.
- Phase 6 must not move business rules into startup composition or controllers.
- Phase 6 must not introduce Phase 7 features, role-based access changes, attendance, salary, documents, dashboards, audit logs, or new authorization policy behavior.
- Phase 6 must not reintroduce direct data-access dependencies into service classes.
- Phase 6 must remain compatible with the existing local database after the already-approved Phase 5 migration has been applied.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide one clear registration entry point for application-owned dependencies, even when no current application-only services require registration.
- **FR-002**: The system MUST provide one clear registration entry point for infrastructure-owned dependencies, including infrastructure-backed implementations of application service contracts.
- **FR-003**: The startup composition MUST delegate application and infrastructure registration to those owning entry points.
- **FR-004**: The startup composition MUST continue to own request-handling concerns, including controller setup, serialization, authentication, authorization, cross-origin access, documentation, and middleware ordering.
- **FR-005**: Existing endpoint routes, request bodies, response bodies, pagination envelopes, session behavior, claims, status codes, and structured error payload shapes MUST remain stable.
- **FR-006**: Existing Phase 5 business rules MUST remain active after the cleanup.
- **FR-007**: The cleanup MUST NOT add new tables, columns, relationships, migrations, seed data, or database constraints.
- **FR-008**: The cleanup MUST NOT change the configured local database target or require a database reset.
- **FR-009**: The cleanup MUST preserve cancellation forwarding, scoped lifetime behavior, and transaction behavior for existing workflows.
- **FR-010**: The cleanup MUST preserve existing credential validation and session validation behavior.
- **FR-011**: The cleanup MUST preserve time-provider and business-rule helper availability for services that depend on deterministic time and working-day calculations.
- **FR-012**: The cleanup MUST provide reviewer-friendly static checks proving registration ownership boundaries are respected.
- **FR-013**: The cleanup MUST be verifiable with the existing automated test suite plus a focused local database smoke test.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of existing automated tests pass after the cleanup.
- **SC-002**: 100% of representative manual smoke checks for login, employee, vacation, trip, and department workflows complete with the same externally visible behavior as before the cleanup.
- **SC-003**: Reviewers can identify the registration owner for application dependencies and infrastructure dependencies in under 2 minutes.
- **SC-004**: Static review confirms zero direct application or infrastructure service registrations remain at the startup surface.
- **SC-005**: Static review confirms zero new database migrations or schema changes are introduced by Phase 6.
- **SC-006**: The application starts successfully against the configured local database after the Phase 5 migration is applied.

## Assumptions

- Phase 5 implementation has been completed in code and its automated tests pass.
- The Phase 5 database migration should be applied to `HrSystemDb` before any Phase 6 manual runtime validation.
- Phase 6 is a wiring and ownership cleanup only; it is not a behavior or schema feature.
- Application service contracts remain application-owned, while infrastructure-backed implementations remain registered by infrastructure.
- Existing session authentication remains the authentication model.
- Existing compatibility error codes remain allowed; error-code normalization is outside Phase 6.
- The configured local database may be used for manual validation and test data setup after specification and planning, but the specification step itself does not mutate the database.
