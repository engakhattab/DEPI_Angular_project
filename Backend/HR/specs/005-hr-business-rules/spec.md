# Feature Specification: Phase 5 - HR Business Logic Improvements

**Feature Branch**: `005-hr-business-rules`

**Created**: 2026-06-02

**Status**: Draft

**Input**: User description: "create specifications for phase 5"

## Clarifications

### Session 2026-06-03

- Q: Who may approve or reject a vacation request during Phase 5? -> A: Any authenticated employee except the requester. Manager-only, reporting-chain, HR-role, and RBAC authorization remain deferred.
- Q: What happens to authentication after an employee is soft-deleted? -> A: New sign-ins and existing authenticated sessions are rejected immediately. Revocation is not delayed until logout or expiry.
- Q: How does termination differ from soft deletion? -> A: Termination immediately records the termination time, rejects pending vacation requests, revokes access, and keeps the retained profile visible. Soft deletion is a separate stronger lifecycle action that additionally hides the retained profile from normal employee results.
- Q: When may a vacation request be deleted? -> A: Only pending requests may be hard-deleted. Approved and rejected requests remain retained for audit. Approved requests may be rejected through the reviewer-driven cancellation flow, which restores balance correctly.
- Q: How is the three-working-day vacation notice window counted? -> A: Exclude both the submission day and vacation start day. Require three full working days between submission and start.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Submit Valid Vacation Requests (Priority: P1)

As an active employee, I want vacation requests to be checked against my existing requests, available balance, and required notice period so that invalid leave is rejected before it affects staffing plans.

**Why this priority**: Vacation-request correctness is the largest group of missing HR rules and prevents conflicting or unfulfillable leave requests from entering the review workflow.

**Independent Test**: Can be fully tested by submitting vacation requests across valid and invalid date ranges for active, suspended, and terminated employees with different balances and existing requests.

**Acceptance Scenarios**:

1. **Given** an active employee with sufficient vacation balance and no conflicting request, **When** the employee submits a future vacation request with the required notice, **Then** the request is accepted as pending.
2. **Given** an employee with a pending or approved vacation request, **When** the employee submits another request whose date range overlaps the existing request, **Then** the new request is rejected.
3. **Given** an employee whose available vacation balance is less than the requested working-day count, **When** the employee submits the request, **Then** the request is rejected.
4. **Given** a suspended or terminated employee, **When** that employee submits a vacation request, **Then** the request is rejected.
5. **Given** a request that begins in the past or does not meet the minimum notice period, **When** the employee submits the request, **Then** the request is rejected.

---

### User Story 2 - Review Vacation Requests Predictably (Priority: P1)

As an authenticated vacation-request reviewer, I want request statuses, reviewer details, and balance adjustments to follow explicit rules so that approvals and cancellations are traceable and do not corrupt employee balances.

**Why this priority**: Review decisions affect staffing records and employee balances. Invalid transitions or self-approval would undermine the reliability of the workflow.

**Independent Test**: Can be fully tested by reviewing pending, approved, and rejected requests with different reviewers and verifying status, audit, and balance outcomes.

**Acceptance Scenarios**:

1. **Given** a pending vacation request and a reviewer who is not the requester, **When** the reviewer approves the request, **Then** the request becomes approved, the review is recorded, and the employee's working-day balance is deducted once.
2. **Given** an approved vacation request, **When** a reviewer rejects it as a cancellation, **Then** the request becomes rejected, the review is recorded, and the previously deducted balance is restored once.
3. **Given** a rejected vacation request, **When** a reviewer attempts any further status change, **Then** the change is rejected.
4. **Given** any vacation request, **When** its requester attempts to approve or reject it, **Then** the change is rejected.
5. **Given** an authenticated employee who is not the requester, **When** that employee approves or rejects a vacation request, **Then** the review is allowed without requiring a manager-only, reporting-chain, HR-role, or RBAC check.
6. **Given** a pending vacation request, **When** an authenticated user with existing delete access deletes it, **Then** the request is removed.
7. **Given** an approved or rejected vacation request, **When** an authenticated user with existing delete access attempts to delete it, **Then** deletion is rejected so review history remains retained.

---

### User Story 3 - Protect Employee Lifecycle Integrity (Priority: P1)

As an HR administrator, I want employee updates and removals to preserve historical records and enforce valid organizational relationships so that workforce data remains trustworthy.

**Why this priority**: Employee lifecycle mistakes can invalidate reporting, create circular reporting chains, or allow conflicting active profiles.

**Independent Test**: Can be fully tested by updating employee statuses, assigning managers, changing email addresses, and removing employees while checking retained history and related vacation requests.

**Acceptance Scenarios**:

1. **Given** an active employee, **When** HR suspends or terminates the employee, **Then** the status change is accepted.
2. **Given** a suspended employee, **When** HR reactivates or terminates the employee, **Then** the status change is accepted.
3. **Given** a terminated employee, **When** HR attempts any further status change, **Then** the change is rejected.
4. **Given** a proposed manager assignment that would create a direct or indirect reporting cycle, **When** HR submits the update, **Then** the update is rejected.
5. **Given** an active employee profile that already uses an email address, **When** HR creates or updates another active employee with the same email address, **Then** the operation is rejected.
6. **Given** an employee with pending vacation requests, **When** HR terminates or removes that employee, **Then** the pending requests are automatically rejected and the employee record remains available for historical reporting.
7. **Given** a soft-deleted employee with a retained login identity, **When** that former employee attempts to sign in, **Then** authentication is denied.
8. **Given** a soft-deleted employee with an existing authenticated session, **When** that former employee attempts to use the session, **Then** access is rejected immediately without waiting for logout or expiry.
9. **Given** an employee who becomes terminated without being soft-deleted, **When** HR views normal employee results, **Then** the retained profile remains visible while new sign-ins and existing authenticated sessions are rejected immediately.

---

### User Story 4 - Record Traceable Trips (Priority: P2)

As an HR user, I want each trip to identify the requesting employee and use a valid trip date so that transportation records can be traced to an active employee.

**Why this priority**: Trips currently lack employee ownership, which limits accountability and permits invalid scheduling.

**Independent Test**: Can be fully tested by submitting trips for active, suspended, terminated, missing, and removed employees across past, working, and non-working dates.

**Acceptance Scenarios**:

1. **Given** an active employee and an allowed future working date, **When** a trip is submitted for that employee, **Then** the trip is accepted and associated with the employee.
2. **Given** a missing, suspended, terminated, or removed employee, **When** a trip is submitted for that employee, **Then** the trip is rejected.
3. **Given** a date in the past, **When** a trip is submitted, **Then** the trip is rejected.
4. **Given** a non-working date, **When** a trip is submitted, **Then** the trip is rejected.

---

### User Story 5 - See Department Staffing Counts (Priority: P3)

As an HR user, I want department results to show the current employee count so that I can understand staffing levels without a separate lookup.

**Why this priority**: This improves the usefulness of existing department views after the employee soft-deletion rule is active.

**Independent Test**: Can be fully tested by viewing departments before and after adding, moving, terminating, and removing employees.

**Acceptance Scenarios**:

1. **Given** a department with active and retained removed employees, **When** HR views the department, **Then** the displayed employee count includes current employees and excludes soft-deleted employees.
2. **Given** an employee moved between departments, **When** HR views both departments, **Then** each displayed employee count reflects the current assignment.

### Edge Cases

- An overlap check includes requests that start or end on the same date as an existing pending or approved request.
- A rejected vacation request does not block a new request for the same dates.
- Vacation balance deduction happens only when a request first becomes approved; rejection of a previously approved request restores the same number of working days once.
- Vacation requests with an end date before the start date continue to be rejected.
- A vacation request is inside the notice window when there are fewer than three full working days between the submission day and vacation start day.
- Any authenticated employee except the requester may approve or reject a vacation request during Phase 5.
- Only pending vacation requests may be hard-deleted; approved and rejected requests remain retained for audit.
- Same-status vacation updates and same-status employee updates are idempotent no-op successes: they do not duplicate side effects, do not update review or termination timestamps again, and are not rejected merely because the requested status is already current.
- Vacation notice checks near Friday/Saturday weekends and Sunday start dates count only full Sunday-through-Thursday working days between the submission day and vacation start day; weekend days never satisfy the notice requirement.
- Manager updates reject missing managers and reject direct, indirect, or bounded long-chain reporting cycles when the proposed assignment eventually points back to the employee being updated.
- An email conflict check applies to active employee profiles and does not treat a retained soft-deleted profile as an active duplicate.
- Removing an employee preserves the employee record for historical reporting while excluding it from normal current-employee results.
- Termination records the termination time, rejects pending vacation requests, revokes access immediately, and retains profile visibility until the separate soft-deletion action occurs.
- A soft-deleted employee retains the associated login identity for reporting and audit, but new sign-ins and existing authenticated sessions are rejected immediately.
- A cross-department manager assignment is allowed and recorded as a warning rather than rejected.
- Sunday through Thursday are working days; Friday and Saturday are excluded from vacation duration, notice, and trip scheduling.
- Department staffing counts exclude soft-deleted employee profiles.
- Trip requests for removed employees are rejected even though historical employee data is retained.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST reject a vacation request when the employee already has a pending or approved request whose date range overlaps the requested date range.
- **FR-002**: The system MUST allow a rejected vacation request to coexist with a later request for the same dates.
- **FR-003**: The system MUST allow only active employees to submit vacation requests.
- **FR-004**: The system MUST reject vacation requests whose start date is in the past.
- **FR-005**: The system MUST require at least three full working days of advance notice for a vacation request, excluding both the submission day and the vacation start day from the count.
- **FR-006**: The system MUST track each employee's available vacation balance, starting at 21 working days unless an approved future policy changes the default.
- **FR-007**: The system MUST reject vacation requests that exceed the employee's available working-day vacation balance.
- **FR-008**: The system MUST deduct the requested working-day count once when a vacation request first becomes approved and MUST restore that count once if an approved request later becomes rejected.
- **FR-009**: The system MUST calculate vacation duration, minimum notice, and trip working-day eligibility using Sunday through Thursday as working days and Friday through Saturday as weekend days.
- **FR-010**: The system MUST prevent an employee from approving or rejecting their own vacation request.
- **FR-010A**: During Phase 5, the system MUST allow any authenticated employee except the requester to approve or reject a vacation request without introducing manager-only, reporting-chain, HR-role, or RBAC authorization.
- **FR-011**: The system MUST record the reviewing employee and review time whenever a vacation-request review decision is accepted.
- **FR-012**: Vacation-request status changes MUST follow this state machine: `Pending` may become `Approved` or `Rejected`; `Approved` may become `Rejected`; `Rejected` is terminal.
- **FR-013**: The system MUST reject vacation-request status changes that are not allowed by the vacation-request state machine.
- **FR-013B**: Vacation-request same-status updates MUST be idempotent no-op successes and MUST NOT duplicate balance changes, reviewer audit changes, or timestamp updates.
- **FR-013A**: The system MUST allow hard deletion only for pending vacation requests and MUST retain approved and rejected vacation requests for audit.
- **FR-014**: Employee status changes MUST follow this state machine: `Active` may become `Suspended` or `Terminated`; `Suspended` may become `Active` or `Terminated`; `Terminated` is terminal.
- **FR-015**: The system MUST reject employee status changes that are not allowed by the employee-status state machine.
- **FR-015A**: Employee same-status updates MUST be idempotent no-op successes and MUST NOT duplicate termination side effects, pending-vacation rejection, access-revocation work, or timestamp updates.
- **FR-016**: The system MUST automatically reject an employee's pending vacation requests when that employee becomes terminated or is removed.
- **FR-016A**: When an employee becomes terminated, the system MUST immediately record the termination time, deny new sign-ins, and reject existing authenticated sessions without waiting for logout or expiry.
- **FR-017**: The system MUST keep an employee number immutable after employee creation.
- **FR-018**: The system MUST reject a manager assignment that would create a direct or indirect circular reporting chain.
- **FR-019**: When an employee is assigned a manager from a different department, the system MUST allow the assignment and record a warning so valid organizational exceptions are not blocked.
- **FR-020**: The system MUST reject creation or update of an active employee when another active employee already uses the same email address.
- **FR-021**: Removing an employee MUST preserve the employee profile for historical reporting, mark it as removed, record the termination time, and place it in the terminated status.
- **FR-022**: Normal employee results MUST retain terminated-but-not-soft-deleted employee profiles and MUST exclude soft-deleted employee profiles while historical reporting remains able to retain them.
- **FR-023**: When an employee is soft-deleted, the system MUST retain the associated login identity and employee-to-identity association for reporting and audit while explicitly denying new sign-ins and immediately rejecting existing authenticated sessions for that employee.
- **FR-024**: A trip submission MUST identify the requesting employee.
- **FR-025**: The system MUST accept a trip only when the requesting employee exists and is active.
- **FR-026**: The system MUST reject trips scheduled in the past or on a non-working day.
- **FR-027**: Department results MUST include an employee count that excludes soft-deleted employee profiles.
- **FR-028**: New Phase 5 rule failures MUST continue to use the existing structured error response format without renaming existing compatibility error codes as part of this phase.
- **FR-029**: Phase 5 MUST preserve existing authentication routes, session-cookie behavior, claims, and unrelated public behavior except for the explicitly approved HR business-rule outcomes in this specification.
- **FR-030**: Phase 5 MUST NOT perform Phase 6 dependency-registration restructuring or introduce Phase 7 advanced HR features.

### Key Entities

- **Employee**: An HR profile with an immutable employee number, status, email address, available vacation balance, department, optional manager, removal state, termination time, and associated login identity. Termination revokes access while retaining normal profile visibility; soft deletion additionally hides the profile from normal employee results.
- **Vacation Request**: A leave request with an employee, date range, reason, review status, creation and update times, reviewing employee, and review time.
- **Trip**: A transportation request with trip details, generated identifiers, date, and requesting employee.
- **Department**: An organizational unit with current employee-count information.
- **Login Identity**: The existing sign-in record associated with an employee profile. It is retained for reporting and audit after employee soft deletion, but new sign-ins and existing authenticated sessions are rejected immediately.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of vacation submissions that overlap an existing pending or approved request are rejected, while equivalent submissions overlapping only rejected requests remain eligible for normal validation.
- **SC-002**: 100% of vacation submissions from non-active employees, with insufficient balance, in the past, or inside the minimum notice window based on three full working days between submission and start are rejected.
- **SC-003**: 100% of tested vacation-request status changes follow the approved transition table, prevent self-approval, record reviewer audit details, adjust balances exactly once, and leave same-status updates as no-op successes.
- **SC-003A**: 100% of tested pending vacation-request deletions succeed, while deletion attempts for approved or rejected requests are rejected and retain their audit history.
- **SC-004**: 100% of tested employee-status changes follow the approved transition table, same-status updates are no-op successes, and termination rejects all pending vacation requests for the affected employee.
- **SC-005**: 100% of tested direct and indirect circular manager assignments are rejected before the employee relationship changes.
- **SC-006**: 100% of tested active-employee duplicate email attempts are rejected, while retained soft-deleted profiles do not block an otherwise valid active email.
- **SC-007**: 100% of tested employee removals preserve historical employee data, record termination information, exclude the profile from normal current-employee results, retain the associated login identity, deny subsequent sign-ins, and reject existing authenticated sessions immediately.
- **SC-007A**: 100% of tested terminations record the termination time, reject pending vacation requests, revoke new and existing authenticated access immediately, and retain the employee profile in normal results until a separate soft-deletion action occurs.
- **SC-008**: 100% of tested trip submissions are traceable to an active employee and reject past or non-working dates.
- **SC-009**: Department results report the correct current employee count after employee creation, movement, termination, and soft deletion.
- **SC-010**: Existing regression checks pass without Phase 6 dependency-registration restructuring, Phase 7 feature additions, or unrelated authentication compatibility changes.

## Assumptions

- Phases 0 through 4 are complete before Phase 5 implementation begins.
- Existing routes remain in place. Phase 5 changes business outcomes and adds only the data needed for the approved HR workflows.
- Vacation requests begin in the `Pending` status.
- The initial employee vacation balance is 21 working days.
- The minimum vacation notice period is three full working days, excluding both the submission day and the vacation start day.
- The working week is Sunday through Thursday; Friday and Saturday are weekend days.
- Approved vacation requests may later be rejected as reviewer-driven cancellations.
- Soft-deleted employees are retained for historical reporting but excluded from normal current-employee results.
- Normal employee results mean the existing current employee list/detail API responses and current department employee count projections. Historical reporting means retained data remains available to existing internal relationships and future reporting work; Phase 5 does not add new historical-reporting endpoints.
- Soft-deleted employees retain their login identity association for reporting and audit, but new sign-ins and existing authenticated sessions are rejected immediately without waiting for logout or expiry.
- Terminated-but-not-soft-deleted employees remain visible in normal employee results, but access is revoked immediately and pending vacation requests are rejected.
- Cross-department manager assignments are valid organizational exceptions and are recorded as warnings rather than rejected.
- Existing endpoint authorization remains unchanged. References to authenticated users with existing access do not introduce manager-only, HR-role, RBAC, or new delete authorization rules in Phase 5.
- Multi-entity state changes, including vacation approval/cancellation and employee termination/removal, are persisted atomically through the existing unit-of-work boundary so partial status, balance, audit, access, or pending-vacation side effects are not committed.
- Department hierarchy is not introduced in Phase 5. Any manager-department rule applies to the current flat department model.
- Phase 6 dependency-registration cleanup remains deferred.
- Attendance, role-based access control, compensation, documents, dashboard metrics, general audit logging, and other advanced HR features remain deferred to Phase 7.
