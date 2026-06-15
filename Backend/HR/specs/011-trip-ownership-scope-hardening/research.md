# Research: Phase 11 - Trip Ownership and Scope Hardening

## Decision: Use Requester-Aware Trip Service Methods

**Decision**: Add requester employee ID parameters to trip service methods for list, detail, create, and delete operations. Return `Result<T>` for read methods that can now fail due to authorization.

**Rationale**: Current trip endpoints call service methods without requester context, so the service cannot enforce self/team/organization scope. Returning `Result<T>` lets the service produce `401`, `403`, and `404` without controller business logic.

**Alternatives considered**:

- Controller-only authorization: rejected because the constitution requires business scope in service classes.
- ASP.NET authorization policies per endpoint: rejected because trip owner/team decisions need data access and existing service business-rule ordering.
- Keep nullable read responses and infer forbidden in controller: rejected because it mixes HTTP adapter logic with business authorization.

## Decision: Reuse Phase 8 `IEmployeeAccessService`

**Decision**: Use the completed Phase 8 access service for requester context, role checks, self scope, team scope, organization scope, and visible employee IDs.

**Rationale**: Phase 8 already centralizes direct-plus-indirect manager team scope and invalid requester handling. Reuse keeps Phase 11 aligned with Phase 9 and Phase 10 access hardening.

**Alternatives considered**:

- Reimplement manager hierarchy traversal in `TripService`: rejected as duplicate scope logic.
- Add a trip-specific access service immediately: rejected as unnecessary unless implementation reveals real complexity.
- Use controller role attributes only: rejected because `Manager` scope depends on the target traveler.

## Decision: Scope Trip Lists Before Pagination

**Decision**: Build an allowed traveler ID set in the service and pass it to repository paging before pagination and total-count calculation. Apply the optional `travelerEmployeeId` filter inside that allowed set.

**Rationale**: The spec requires pagination and counts not to leak out-of-scope trip records. Repository-side filtering keeps paging correct and avoids loading broad result sets into memory.

**Alternatives considered**:

- Fetch all rows then filter in memory: rejected because total counts/pages would be incorrect and inefficient.
- Return `403` for out-of-scope list filters: rejected because an empty scoped result avoids existence probing and matches the Phase 11 decision.
- Leave list endpoint organization-wide for all roles: rejected because it is the main privacy gap Phase 11 closes.

## Decision: Keep Missing-vs-Out-of-Scope ID Ordering

**Decision**: For ID-based target operations, preserve `404` when the trip does not exist and return `403` for existing out-of-scope trips before mutation or other validation.

**Rationale**: The Phase 11 spec follows the Phase 9 and Phase 10 convention for protected existing records. For existing out-of-scope targets, authorization-first ordering prevents leaking trip ownership or validation details.

**Alternatives considered**:

- Always return `404` for out-of-scope records: rejected because it conflicts with the current Phase 11 spec.
- Validate domain rules before authorization: rejected because it can leak target state.
- Authorize before target lookup for all ID operations: rejected because the spec preserves missing ID `404` behavior.

## Decision: Preserve Current Hard Delete Behavior

**Decision**: Keep current trip hard-delete behavior for in-scope trips and add role/scope restrictions. Do not introduce cancellation, soft-delete, audit semantics, or approval workflow for trips in Phase 11.

**Rationale**: The user explicitly chose to preserve current deletion semantics. Phase 11 is scope hardening, not a lifecycle redesign.

**Alternatives considered**:

- Convert delete to a cancel or soft-delete state: rejected as a new workflow outside Phase 11.
- Restrict all trip deletes to HR/System: rejected because the spec permits employee own delete and manager own/team delete after authorization.
- Add trip audit trail here: rejected as unrelated unless a separate approved phase requires it.

## Decision: Treat `RequestedByEmployeeId` as Compatibility Traveler Data

**Decision**: Keep the existing `requestedByEmployeeId` create request field and existing `Trip.RequestedByEmployeeId` / `TripResponse.RequestedByEmployeeId` fields as compatibility traveler data. Add separate requester storage only after explicit migration approval.

**Rationale**: Existing rows contain only one employee reference, and the clarified spec says those values are traveler data. Renaming or repurposing public fields would break clients and risk inventing false historical requester data.

**Alternatives considered**:

- Treat existing `RequestedByEmployeeId` as historical requester: rejected by the clarified Phase 11 decision.
- Rename the request field to `travelerEmployeeId`: rejected as a breaking request contract change.
- Backfill requester as the traveler for old rows: rejected because that invents unverified requester history.

## Decision: Use Nullable `RequesterEmployeeId` as the Approval-Gated Storage Model

**Decision**: Document a nullable `RequesterEmployeeId` FK on `Trips` as the required persisted requester-tracking model, but do not create the migration without separate approval.

**Rationale**: New trips need to record who created the request separately from who takes the trip. Existing rows cannot be reliably backfilled, so nullable storage preserves data integrity.

**Alternatives considered**:

- Code-only Phase 11 with no persisted requester tracking: rejected because the clarified spec requires separate requester storage.
- Add a non-null requester column immediately: rejected because existing rows cannot be reliably backfilled.
- Reuse vacation `CreatedByEmployeeId` naming: rejected to avoid implying a generic audit feature; `RequesterEmployeeId` matches trip business language.

## Decision: Keep API DTO Compatibility

**Decision**: Keep existing routes, request DTO field names, success response compatibility, pagination envelope, cookie behavior, claims, and structured errors. Optional traveler/requester response metadata may be added only additively after approved storage exists.

**Rationale**: The constitution and Phase 11 spec prioritize backward compatibility. Scope hardening changes authorization outcomes and filtering, not route or DTO contracts.

**Alternatives considered**:

- Add a separate on-behalf trip endpoint: rejected as unnecessary because existing `POST /api/trips` already carries a target employee field.
- Normalize existing error codes: rejected because compatibility phases must handle that separately.
- Change existing successful response fields: rejected as a breaking change.
