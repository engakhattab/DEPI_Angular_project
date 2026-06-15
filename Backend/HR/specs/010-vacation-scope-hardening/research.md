# Research: Phase 10 - Vacation Request Scope Hardening

## Decision: Use Requester-Aware Vacation Service Methods

**Decision**: Add requester employee ID parameters to vacation service methods for list, detail, create, status update, and delete operations. Return `Result<T>` for read methods that can now fail due to authorization.

**Rationale**: Current `VacationRequestsController` passes requester context only for status update. List, detail, create, and delete cannot enforce self/team/organization scope without requester context. Returning `Result<T>` lets the service produce `401`, `403`, and `404` without controller business logic.

**Alternatives considered**:

- Controller-only authorization: rejected because the constitution requires business scope in service classes.
- ASP.NET authorization policies per endpoint: rejected because vacation owner/team decisions need data access and existing service business-rule ordering.
- Keep nullable read responses and infer forbidden in controller: rejected because it mixes HTTP adapter logic with business authorization.

## Decision: Reuse Phase 8 `IEmployeeAccessService`

**Decision**: Use the completed Phase 8 access service for requester context, role checks, self scope, team scope, organization scope, and visible employee IDs.

**Rationale**: Phase 8 already centralizes direct-plus-indirect manager team scope and invalid requester handling. Reuse avoids duplicate hierarchy logic in vacation services and keeps Phase 10 aligned with Phase 9 hardening.

**Alternatives considered**:

- Reimplement manager hierarchy traversal in `VacationRequestService`: rejected as duplicate scope logic.
- Add vacation-specific access service immediately: rejected as unnecessary unless implementation reveals real complexity.
- Use controller role attributes only: rejected because `Manager` scope depends on the target vacation owner.

## Decision: Scope Vacation Lists Before Pagination

**Decision**: Build an allowed owner ID set in the service and pass it to repository paging before pagination and total-count calculation. Apply `status` and `employeeId` filters inside that allowed set.

**Rationale**: The spec requires pagination and counts not to leak out-of-scope vacation records. Repository-side filtering keeps paging correct and avoids loading broad result sets into memory.

**Alternatives considered**:

- Fetch all rows then filter in memory: rejected because total counts/pages would be incorrect and inefficient.
- Keep existing `employeeId` filter only: rejected because it does not represent role/team scope.
- Return `403` for every out-of-scope list filter: rejected for list filters because an empty scoped result avoids existence probing and matches "filter within allowed scope" semantics.

## Decision: Keep Missing-vs-Out-of-Scope Detail/Delete/Review Ordering

**Decision**: For ID-based target operations, preserve `404` when the vacation request does not exist and return `403` for existing out-of-scope targets before domain-rule validation.

**Rationale**: The Phase 10 spec and Phase 9 convention distinguish missing IDs from existing but unauthorized IDs. For existing out-of-scope targets, authorization-first ordering prevents leaking pending status, transition validity, balance, notice, overlap, or other business-rule details.

**Alternatives considered**:

- Always return `404` for out-of-scope records: rejected because it conflicts with the current Phase 10 spec.
- Validate domain rules before authorization: rejected because it leaks target state.
- Authorize before target lookup for all users: rejected because the spec preserves missing ID `404` behavior for ID-based operations.

## Decision: Preserve Pending Hard Delete Behavior

**Decision**: Keep the current pending-only delete behavior and add role/scope restrictions. Do not introduce cancellation, soft-delete, or audit semantics for vacation requests in Phase 10.

**Rationale**: Current implementation hard-removes pending requests. The spec explicitly says delete remains pending-only and Phase 10 must not expand workflow semantics.

**Alternatives considered**:

- Convert delete to cancel status: rejected as a new workflow outside Phase 10.
- Let managers delete team requests: rejected by the Phase 10 clarification and access matrix.
- Add vacation audit trail here: rejected as unrelated to the scoped access fix unless existing audit requirements are already wired by earlier phases.

## Decision: Treat `CreatedByEmployeeId` as Required but Migration-Gated

**Decision**: Document a nullable `CreatedByEmployeeId` FK as the required persisted creator-tracking model, but do not create the migration without separate approval.

**Rationale**: The current `VacationRequest` entity stores owner and reviewer, not creator. HR/System on-behalf creation needs creator tracking to distinguish the owner from the acting employee. The project migration policy requires stopping before schema changes.

**Alternatives considered**:

- Code-only Phase 10 with no persisted creator tracking: rejected because the clarified spec requires creator tracking.
- Backfill existing rows to owner-as-creator: rejected because that invents historical creator data.
- Add a non-null creator column immediately: rejected because existing rows cannot be reliably backfilled.

## Decision: Keep API DTO Compatibility

**Decision**: Keep existing routes, request DTOs, success response compatibility, pagination envelope, cookie behavior, claims, and structured errors. If creator metadata is later exposed in responses, it must be additive and tied to the approved migration work.

**Rationale**: The constitution and Phase 10 spec both prioritize backward compatibility. Scope hardening changes authorization outcomes and filtering, not route or DTO contracts.

**Alternatives considered**:

- Add separate "on behalf" endpoint: rejected as unnecessary because existing `POST /api/vacationrequests` already carries target `EmployeeId`.
- Rename `EmployeeId` to owner ID in requests: rejected as a breaking change.
- Normalize existing error codes: rejected because compatibility phases must handle that separately.
