# Feature Specification: Phase 13 - Swagger/OpenAPI Response Documentation Pass

**Feature Branch**: `013-swagger-response-docs`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "Create specifications for phase 13"

## Boundary Definitions and Scope

Phase 13 documents the API response outcomes that already exist in the HR backend so Swagger/OpenAPI consumers no longer see expected responses as `Undocumented`. This phase is a response-documentation pass only. It must make the published API reference accurately describe current success responses, error responses, response payloads, empty responses, file responses, and authentication/authorization outcomes for all existing endpoint groups.

Phase 13 MUST NOT change runtime business behavior, route names, request shapes, response JSON shapes, status-code behavior, validation rules, authorization scope rules, authentication/cookie behavior, database schema, database migrations, seeded data, or client-facing lifecycle instructions except for recording Phase 13 verification evidence. If the documentation review exposes a runtime defect or inconsistent behavior, Phase 13 MUST record it as a follow-up and stop before changing behavior.

## User Scenarios and Testing *(mandatory)*

### User Story 1 - Document Common API Responses (Priority: P1)

As a developer or client integrator using Swagger, I need each endpoint to show its expected success and error responses so I can understand the API contract without guessing from manual calls.

**Why this priority**: The known Phase 13 problem is that valid endpoint responses, including successful `201 Created` results, appear as `Undocumented`, which reduces API usability even when runtime behavior is correct.

**Independent Test**: Open Swagger for the current API, review each endpoint group, and verify that expected success and error responses are listed with the correct status codes and payload expectations.

**Acceptance Scenarios**:

1. **Given** an endpoint that successfully returns data, **When** a developer opens that operation in Swagger, **Then** the expected success status is documented with the appropriate response payload shape.
2. **Given** an endpoint that creates a resource, **When** a developer opens that operation in Swagger, **Then** `201 Created` is documented and no longer appears as `Undocumented`.
3. **Given** an endpoint that returns no body on success, **When** a developer opens that operation in Swagger, **Then** `204 No Content` is documented without implying a response body.

---

### User Story 2 - Document Authorization and Validation Outcomes (Priority: P1)

As a tester validating role-scoped behavior, I need Swagger to list expected unauthenticated, forbidden, not found, conflict, and validation responses so security and business-rule outcomes are visible in the API reference.

**Why this priority**: Phases 8-11 hardened authorization scope behavior, and client testers need documented expected failures just as much as documented success responses.

**Independent Test**: Review protected and role-scoped endpoints in Swagger and confirm expected 401, 403, 404, 409, and 422 outcomes are documented where current behavior supports them.

**Acceptance Scenarios**:

1. **Given** a protected endpoint, **When** a developer reviews it in Swagger, **Then** an unauthenticated outcome is documented without suggesting bearer-token authentication.
2. **Given** an endpoint with role or ownership restrictions, **When** a developer reviews it in Swagger, **Then** forbidden and/or not-found outcomes are documented according to current behavior.
3. **Given** an endpoint with business-rule or validation failures, **When** a developer reviews it in Swagger, **Then** the documented status codes and error payload shape match the existing behavior.

---

### User Story 3 - Preserve Existing API Behavior While Improving Documentation (Priority: P2)

As a maintainer, I need the response documentation pass to be reviewable and behavior-neutral so client integrations are not affected by documentation cleanup.

**Why this priority**: Phase 13 follows completed scope-hardening work and must not re-open business logic, route, DTO, authentication, or migration changes.

**Independent Test**: Compare the API before and after the documentation pass by building, running tests, opening Swagger, and confirming that routes remain present and runtime responses are unchanged.

**Acceptance Scenarios**:

1. **Given** all existing endpoint groups, **When** Swagger is opened after Phase 13, **Then** no existing route is removed or renamed.
2. **Given** a documented endpoint, **When** it is called using the same inputs as before Phase 13, **Then** the returned status code and response body behavior are unchanged.
3. **Given** the implementation review finds an undocumented behavior mismatch, **When** the mismatch would require runtime changes, **Then** it is recorded as follow-up work instead of being fixed in Phase 13.

### Edge Cases

- Endpoints that return files or file downloads must document file responses without misrepresenting them as normal JSON payloads.
- Upload endpoints must document payload-too-large behavior only where that outcome exists in current behavior.
- Endpoints that can return an empty list or empty page must keep the documented success response distinct from not-found or forbidden outcomes.
- Authenticated forbidden outcomes must remain distinct from unauthenticated outcomes: 403 versus 401.
- Scope-hardened employee, vacation, and trip endpoints must document current Phase 8-11 access outcomes without broadening access expectations.
- Existing structured error payload expectations must be documented without inventing new error codes or normalizing inconsistent ones.
- Endpoints with no response body must not imply a JSON response body.
- Documentation must not add expected error statuses to endpoints where current behavior does not support them.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Swagger/OpenAPI documentation MUST list all expected success responses for existing endpoint groups, including `200 OK`, `201 Created`, and `204 No Content` where each status is currently returned.
- **FR-002**: Swagger/OpenAPI documentation MUST list expected error responses for existing endpoint groups, including `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict`, `413 Payload Too Large`, and `422 Unprocessable Entity` where each status is currently returned.
- **FR-003**: Response documentation MUST use the current response payload shapes for success responses, paged/list responses, structured error responses, empty responses, and file responses.
- **FR-004**: The response documentation pass MUST cover Auth, Employees, Departments, Attendance, Vacation Requests, Trips, Compensation, Employee Documents, Dashboard, and Audit Logs endpoint groups.
- **FR-005**: Protected endpoints MUST document unauthenticated outcomes in a way that is consistent with the existing cookie-based session behavior.
- **FR-006**: Role-scoped endpoints MUST document forbidden and not-found outcomes according to the current Phase 8-11 authorization and ownership behavior.
- **FR-007**: Business-rule and validation outcomes MUST document the currently returned status codes and structured error response shape without changing validation behavior.
- **FR-008**: File upload and download endpoints MUST document their current response behavior, including file response and payload-too-large outcomes where applicable.
- **FR-009**: Swagger verification MUST confirm that no expected successful response appears as `Undocumented` for reviewed endpoints.
- **FR-010**: Swagger verification MUST confirm that no existing route disappears, is renamed, or is moved to a different endpoint group as part of Phase 13.
- **FR-011**: Phase 13 MUST NOT change route names, request fields, response fields, response JSON shape, status-code behavior, business logic, validation logic, authorization logic, authentication/cookie behavior, database schema, database migrations, or seeded data.
- **FR-012**: If review discovers that the API returns a status or payload inconsistent with project requirements, Phase 13 MUST document the finding as follow-up work and must not change runtime behavior without separate approval.
- **FR-013**: Completion evidence MUST include build/test results and manual Swagger verification notes for the documented endpoint groups.

### Key Entities

- **Endpoint Group**: A user-visible group of related API operations, such as Auth, Employees, Departments, Attendance, Vacation Requests, Trips, Compensation, Employee Documents, Dashboard, or Audit Logs.
- **Response Documentation Entry**: The status code, response body expectation, and user-facing description shown in Swagger/OpenAPI for one possible outcome of an endpoint.
- **Success Response**: A documented successful outcome such as returned data, created resource, deleted/updated result, empty response, list/page result, or file response.
- **Error Response**: A documented failure outcome such as invalid request, unauthenticated request, forbidden access, missing resource, conflict, payload too large, or validation/business-rule failure.
- **Swagger Verification Result**: The recorded evidence that reviewed endpoint responses are documented and that existing routes remain visible.
- **Follow-Up Finding**: A behavior or documentation mismatch found during review that is outside the behavior-neutral Phase 13 scope.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of existing endpoint groups listed in FR-004 are reviewed for success and error response documentation.
- **SC-002**: 100% of reviewed endpoints that currently return `200 OK`, `201 Created`, or `204 No Content` show those expected success statuses as documented responses in Swagger.
- **SC-003**: 100% of reviewed protected endpoints show documented unauthenticated outcomes consistent with current cookie-based session behavior.
- **SC-004**: 100% of reviewed role-scoped employee, vacation, and trip endpoints show documented forbidden and/or not-found outcomes where current Phase 8-11 behavior returns them.
- **SC-005**: Manual Swagger verification confirms zero existing endpoint routes disappear or are renamed during Phase 13.
- **SC-006**: Build and automated test validation complete successfully before Phase 13 is marked complete.
- **SC-007**: The final Phase 13 evidence records any unresolved documentation or runtime mismatch as follow-up work, with no runtime behavior changes bundled into this phase.

## Assumptions

- Phase 8 Authorization Scope Foundation, Phase 9 Employee Access Scope Hardening, Phase 10 Vacation Scope Hardening, Phase 11 Trip Ownership Scope Hardening, and Phase 12 Lifecycle Documentation and Manual Retest are complete before Phase 13 implementation starts.
- The current API behavior, status codes, payload shapes, cookie-based authentication model, and structured error response shape are the source of truth for Phase 13 documentation.
- Swagger/OpenAPI response documentation may be improved, but the underlying API contract must not be changed in this phase.
- Manual Swagger verification is available in a local developer environment after a successful build.
- Structured error code normalization across all modules is separate follow-up work unless explicitly approved later.
