# Feature Specification: Phase 1 — Global Exception Handling & Pagination Infrastructure

**Feature Branch**: `002-exception-handling-pagination`

**Created**: 2026-05-21

**Status**: Draft

**Input**: User description: "Read PLAN.md and create specification for phase 1."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consistent Error Responses (Priority: P1)

As an API consumer, I want every error returned by the system to follow a single, predictable format so that my client application can parse and display errors reliably without special-casing individual endpoints.

**Why this priority**: Centralized error handling is the foundation for all subsequent phases. Every service extracted in Phase 3 and every business rule enforced in Phase 5 will rely on this middleware to translate exceptions into safe, structured responses.

**Independent Test**: Can be fully tested by triggering each category of error (not-found, conflict, business-rule violation, unhandled server error) and verifying the response body matches the standard format and the correct HTTP status code is returned. No raw exception stack traces should ever appear in the response.

**Acceptance Scenarios**:

1. **Given** a request targets a resource that does not exist, **When** the service throws a not-found error, **Then** the API returns HTTP 404 with body `{ "code": "NOT_FOUND", "message": "<description>" }`.
2. **Given** a request creates a conflict with existing data, **When** the service throws a conflict error, **Then** the API returns HTTP 409 with body `{ "code": "CONFLICT", "message": "<description>" }`.
3. **Given** a request violates a business rule, **When** the service throws a business-rule error, **Then** the API returns HTTP 422 with body `{ "code": "BUSINESS_RULE", "message": "<description>" }`.
4. **Given** an unexpected server error occurs, **When** the exception is unhandled, **Then** the API returns HTTP 500 with body `{ "code": "SERVER_ERROR", "message": "An unexpected error occurred." }` and the actual exception details are logged server-side only.
5. **Given** any error occurs, **When** the response is sent, **Then** the `Content-Type` header is `application/json` and the body never contains raw exception text, stack traces, or internal server details.

---

### User Story 2 - Pagination Infrastructure (Priority: P2)

As a developer building list endpoints in future phases, I want a reusable pagination utility available in the shared layer so that I can apply consistent, efficient pagination to any queryable data source without reimplementing the logic per endpoint.

**Why this priority**: While not user-facing in this phase, the pagination utility is a prerequisite for Phase 3 (Service Layer Extraction), where every list endpoint must be paginated.

**Independent Test**: Can be tested by creating an in-memory queryable data source, applying the pagination utility with various page/pageSize combinations, and verifying the returned metadata (total count, page number, total pages, has-next, has-previous) and item subset are correct.

**Acceptance Scenarios**:

1. **Given** a queryable data source with 50 items, **When** I request page 2 with page size 10, **Then** the result contains items 11–20, total count is 50, total pages is 5, has-next is true, and has-previous is true.
2. **Given** a queryable data source with 3 items, **When** I request page 1 with page size 10, **Then** the result contains all 3 items, total count is 3, total pages is 1, has-next is false, and has-previous is false.
3. **Given** a queryable data source, **When** I request page 1 with page size 25, **Then** the default behavior returns up to 25 items per page.

### Edge Cases

- What happens when page number is 0 or negative? The utility should treat it as page 1.
- What happens when page size is 0 or negative? The utility should use the default page size (25).
- What happens when the requested page exceeds total pages? The utility returns an empty item list with correct metadata.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST intercept all unhandled exceptions at the API boundary and convert them into a structured JSON error response.
- **FR-002**: The error response format MUST be `{ "code": "<ERROR_CODE>", "message": "<human-readable message>" }` for all error types.
- **FR-003**: Domain-specific not-found errors MUST map to HTTP 404.
- **FR-004**: Domain-specific conflict errors MUST map to HTTP 409.
- **FR-005**: Domain-specific business rule errors MUST map to HTTP 422.
- **FR-006**: All other unhandled exceptions MUST map to HTTP 500 with a generic safe message — actual details MUST only be logged server-side.
- **FR-007**: The error interception MUST be the first middleware in the request pipeline so it catches errors from all downstream middleware and handlers.
- **FR-008**: The system MUST provide a reusable pagination utility that accepts a queryable data source, page number, and page size, and returns a paged result with metadata (items, total count, current page, page size, total pages, has-next, has-previous).
- **FR-009**: All existing API endpoints MUST continue to function identically after this phase.

### Key Entities

- **Error Response**: A standardized error object with a machine-readable code and a human-readable message.
- **Paged Result**: A generic wrapper containing a subset of items, plus pagination metadata (total count, current page, page size, total pages, navigation flags).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of API error responses conform to the standard `{ "code", "message" }` JSON shape — no endpoint returns raw exception text.
- **SC-002**: Server-side logs capture full exception details (type, message, stack trace) for every 500-level error.
- **SC-003**: The pagination utility correctly pages a dataset of any size, returning accurate metadata for page counts, navigation flags, and item subsets.
- **SC-004**: 100% of existing API endpoints remain functional with no change to their request/response contracts.

## Assumptions

- Phase 0 (Foundation & Project Restructure) is complete — the layered project structure and domain exception types (`NotFoundException`, `ConflictException`, `BusinessRuleException`) are already available.
- The pagination utility will not be wired into any controller in this phase — it is infrastructure preparation for Phase 3.
- The error interception middleware does not modify successful (2xx/3xx) responses in any way.
- Weekend days for business logic purposes are Friday and Saturday (MENA region), but this is not relevant to this phase.
