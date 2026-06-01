# Feature Specification: Phase 3 - Service Layer Extraction

**Feature Branch**: `003-service-layer-extraction`

**Created**: 2026-06-01

**Status**: Draft

**Input**: User description: "Read PLAN.md and create specification for phase 3."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manage Departments Reliably (Priority: P1)

As an authenticated HR user, I want to continue listing, viewing, creating, updating, and deleting departments so that organizational data remains manageable while the internal architecture is improved.

**Why this priority**: Department operations are the smallest business area and provide the first independently testable extraction checkpoint before larger workflows are changed.

**Independent Test**: Can be fully tested by exercising every existing department operation, confirming unchanged successful outcomes, expected validation failures, and paginated department browsing.

**Acceptance Scenarios**:

1. **Given** authenticated access and existing departments, **When** a user browses departments with a valid page request, **Then** the system returns the requested subset with accurate pagination metadata in alphabetical order.
2. **Given** a valid new department name, **When** a user creates the department, **Then** the department is created and returned using the existing department data shape.
3. **Given** a duplicate department name or a department that still has assigned employees, **When** a user attempts the conflicting operation, **Then** the system rejects it with a structured error response.

---

### User Story 2 - Process Vacation Requests Reliably (Priority: P2)

As an authenticated HR user, I want to continue browsing, viewing, submitting, updating, and deleting vacation requests so that leave administration remains functional during the refactor.

**Why this priority**: Vacation requests have filtering and validation behavior that must be preserved after the initial department extraction proves the approach.

**Independent Test**: Can be fully tested by submitting valid and invalid requests, filtering the paginated request list, changing request status, deleting a request, and confirming that existing behavior remains unchanged.

**Acceptance Scenarios**:

1. **Given** vacation requests with different statuses and employees, **When** a user applies the existing filters and requests a page, **Then** only matching requests are returned with accurate pagination metadata and the current newest-first ordering.
2. **Given** a valid employee and valid date range, **When** a user submits a vacation request, **Then** the request is created in the pending state and returned using the existing response data shape.
3. **Given** an invalid date range or an unknown employee, **When** a user submits a vacation request, **Then** the system rejects the request with a structured validation or not-found response.

---

### User Story 3 - Manage Trips Reliably (Priority: P3)

As an authenticated HR user, I want to continue browsing, viewing, creating, and deleting trips so that transportation records remain available while internal responsibilities are separated.

**Why this priority**: Trip operations are less complex than employee management and provide a focused checkpoint before the most complex extraction.

**Independent Test**: Can be fully tested by browsing paginated trips, creating a trip, viewing it, and deleting it while confirming that generated trip identifiers and existing response data remain consistent.

**Acceptance Scenarios**:

1. **Given** existing trips, **When** a user requests a page of trips, **Then** the system returns the requested subset with accurate pagination metadata in the current newest-first ordering.
2. **Given** valid trip details, **When** a user creates a trip, **Then** the trip is created with generated trip and request identifiers and is returned using the existing trip data shape.
3. **Given** an unknown trip, **When** a user attempts to view or delete it, **Then** the system returns a structured not-found response.

---

### User Story 4 - Manage Employees Reliably (Priority: P4)

As an authenticated HR user, I want to continue browsing, viewing, creating, updating, and deleting employees so that employee administration remains fully functional after the most complex business logic is moved out of request handling.

**Why this priority**: Employee operations include credential creation, department and manager validation, identity updates, and related-record cleanup. They are extracted last to reduce behavioral risk.

**Independent Test**: Can be fully tested by exercising the complete employee lifecycle, including generated credentials, existing status filters, related department and manager validations, identity updates, deletion cleanup, and paginated browsing.

**Acceptance Scenarios**:

1. **Given** employees with different statuses, **When** a user browses employees with an optional status filter and a valid page request, **Then** matching employees are returned with accurate pagination metadata in employee-number order.
2. **Given** valid employee details, **When** a user creates an employee, **Then** the employee and associated login identity are created together and the existing creation response is returned.
3. **Given** an existing employee, **When** a user changes valid profile details, **Then** the employee and associated login identity remain synchronized.
4. **Given** an existing employee with related vacation requests or direct reports, **When** a user deletes the employee, **Then** the existing cleanup behavior is preserved and the operation completes consistently.

### Edge Cases

- A requested page number of zero or less is treated as page 1.
- A requested page size of zero or less uses the default page size of 25.
- A requested page size greater than 100 is limited to 100 records.
- A requested page beyond the available results returns an empty item list with accurate metadata.
- Existing list filters continue to work when they are combined with pagination.
- Existing conflict, validation, and not-found conditions continue to return structured errors.
- A disconnected client does not cause unnecessary work to continue after the request is abandoned.
- A failure during a multi-step employee operation does not leave partially completed employee or identity data.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST preserve all existing department, vacation request, trip, and employee operations during the refactor.
- **FR-002**: Existing operation paths, supported inputs, and successful single-record or write-operation response data MUST remain backward-compatible.
- **FR-003**: List operations MUST return paginated results containing items, total count, current page, page size, total pages, next-page availability, and previous-page availability.
- **FR-004**: List operations MUST default to page 1 and a page size of 25 when valid values are not supplied.
- **FR-005**: List operations MUST limit page size to a maximum of 100 records.
- **FR-006**: Department browsing MUST retain alphabetical ordering.
- **FR-007**: Vacation request browsing MUST retain status and employee filters and MUST retain newest-first ordering.
- **FR-008**: Trip browsing MUST retain newest-first ordering.
- **FR-009**: Employee browsing MUST retain the status filter and employee-number ordering.
- **FR-010**: Expected not-found, conflict, validation, and business-rule failures MUST be returned using the standard structured error format with a machine-readable code and human-readable message.
- **FR-011**: The request-handling layer MUST delegate business decisions and data operations to dedicated application operations rather than performing them directly.
- **FR-012**: The request-handling layer MUST remain limited to accepting inputs, checking request validity, invoking the appropriate application operation, and translating the outcome into a response.
- **FR-013**: Operations that are no longer needed because the client disconnected MUST stop promptly throughout the request path.
- **FR-014**: Existing session authentication and authorization behavior MUST remain unchanged.
- **FR-015**: The extraction MUST be completed and verified one business area at a time in this order: departments, vacation requests, trips, then employees.
- **FR-016**: This phase MUST NOT introduce new HR business rules, new business entities, or database schema changes.

### Key Entities

- **Department**: An organizational unit with a unique name and assigned employees.
- **Vacation Request**: A leave request associated with an employee, date range, reason, status, and creation history.
- **Trip**: A transportation record with project, route, type, date, generated identifiers, and creation history.
- **Employee**: An HR profile associated with a department, optional manager, employment status, and login identity.
- **Paginated Result**: A bounded subset of records accompanied by metadata that allows users to navigate a larger result set predictably.
- **Structured Error**: A rejected-operation outcome containing a stable machine-readable code and a human-readable explanation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of existing department, vacation request, trip, and employee operations remain available after extraction.
- **SC-002**: 100% of list operations return accurate pagination metadata and never return more than 100 records in one response.
- **SC-003**: 100% of existing list filters and ordering rules produce the same matching record order as before pagination is applied.
- **SC-004**: 100% of expected not-found, conflict, validation, and business-rule failures return a structured response containing both a code and a message.
- **SC-005**: Each of the four business areas can be validated independently before extraction begins on the next area.
- **SC-006**: No request-handling component directly performs business decisions or data operations after the phase is complete.
- **SC-007**: Existing login, logout, current-user restoration, and unauthorized-access behavior pass regression verification without changes.
- **SC-008**: Abandoned requests stop processing without completing unintended data changes.

## Assumptions

- Phases 0, 1, and 2 are complete before Phase 3 implementation begins.
- Pagination intentionally changes list responses from unbounded collections to paginated result objects; single-record and write-operation response data remain backward-compatible.
- The existing business behavior is preserved as-is in this phase. Additional HR rules are reserved for Phase 5.
- Data-access abstraction and entity configuration restructuring are reserved for Phase 4.
- Invalid page numbers are normalized to page 1, invalid page sizes use the default of 25, and page sizes above 100 are limited to 100.
- Existing secure cookie-based session authentication remains the required access model.
