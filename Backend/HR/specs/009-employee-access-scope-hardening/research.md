# Research: Phase 9 - Employee Access Scope Hardening

## Decision: Use Requester-Aware Employee Service Methods

**Decision**: Employee service operations behind employee endpoints should accept the requester employee ID and return `Result<T>` where authorization can fail.

**Rationale**: The constitution requires business logic in services, not controllers. Existing controller methods currently call unauthenticated service signatures for list/detail/create/update/delete. Passing the requester into the service lets the service enforce role, self, team, organization, and write-authorization rules consistently.

**Alternatives considered**:

- Controller-only `[Authorize(Policy = ...)]`: rejected because manager/self/team scope cannot be represented safely by static controller policies alone.
- Separate endpoint-specific authorization filters: rejected as more complex than necessary and easier to drift from Phase 8 scope definitions.
- Adding a new employee access facade: rejected for Phase 9 because the existing `IEmployeeService` already owns employee list/detail/write behavior.

## Decision: Preserve Routes and DTO Shapes

**Decision**: Keep all employee routes and public success DTO shapes stable. Change only authorization outcomes, list filtering, and service-level validation.

**Rationale**: Phase 9 is a hardening phase, not a client contract redesign. Existing clients should not lose fields or routes when they remain authorized.

**Alternatives considered**:

- Add new manager or self endpoints: rejected because the spec explicitly forbids a public directory or new self-service workflow.
- Return narrow DTOs for employees/managers: rejected because the spec allows current DTOs for authorized access and only requires preventing unauthorized exposure.

## Decision: Filter Before Pagination

**Decision**: Apply employee scope filters before pagination and before calculating paged totals.

**Rationale**: Filtering after pagination can leak out-of-scope counts, empty/non-empty page patterns, or item ordering. The spec requires managers to see team results only and normal employees to receive no list payload.

**Alternatives considered**:

- Filter mapped DTOs after repository pagination: rejected because it leaks total count and produces unstable page sizes.
- Fetch all employees and filter in memory: rejected because it is less efficient and bypasses repository query ownership.

## Decision: Add Narrow Repository Query Support Without Schema Changes

**Decision**: Add repository methods for scoped employee pages, organization-wide pages including soft-deleted records, detail including soft-deleted records, and active system-administrator count/guard support.

**Rationale**: Existing `GetPageWithDetailsAsync` and `GetByIdWithDetailsAsync` filter out soft-deleted rows. Phase 9 needs HR/System historical visibility but manager lists must remain active-team-only. Repository-level queries keep EF details in infrastructure.

**Alternatives considered**:

- Reuse existing `GetPageWithDetailsAsync` for all roles: rejected because it excludes soft-deleted records required for HR/System visibility.
- Add database columns or views: rejected because existing role, status, manager, and deletion fields already represent the needed rules.

## Decision: Use Existing Structured Forbidden Errors

**Decision**: Authenticated out-of-scope access should return existing structured `403 Forbidden` payloads using current `ServiceError.Forbidden()` mapping.

**Rationale**: The spec requires compatibility with existing `{ code, message }` responses and forbids broad error-code normalization.

**Alternatives considered**:

- Hide existing out-of-scope records behind `404`: rejected because clarification selected `403` for authenticated out-of-scope access.
- Introduce new error codes for every access denial: rejected because it adds client churn without a Phase 9 requirement.

## Decision: Guard Last Active System Administrator With Existing Data

**Decision**: Determine active system administrator availability from existing `Employee` rows where `Role == SystemAdministrator`, `Status == Active`, and `IsDeleted == false`.

**Rationale**: This directly matches the clarification and does not require schema changes. The guard should run before any update/delete/status change, termination, soft deletion, or role assignment/demotion that would remove the last active system administrator.

**Alternatives considered**:

- Rely on bootstrap to recover from lockout: rejected because bootstrap no-ops when an active system administrator exists and is not a public recovery workflow.
- Defer lockout prevention to a future phase: rejected by Phase 9 clarification.

## Decision: Guard Role Demotion Without Broadening Role Assignment Authority

**Decision**: Phase 9 confirms role assignment remains `SystemAdministrator` only and adds last-active-SystemAdministrator protection for role demotion. If the target employee is an active `SystemAdministrator`, assigning a non-`SystemAdministrator` role must be rejected before mutation when that demotion would leave zero active `SystemAdministrator` employees.

**Rationale**: Role demotion can remove administrator eligibility the same way termination or status change can. Protecting the last active system administrator here closes the lockout path while preserving the existing rule that only system administrators can assign roles.

**Alternatives considered**:

- Allow last-admin demotion because the endpoint is already SystemAdministrator-only: rejected because it can still lock out administration.
- Remove role assignment from Phase 9: rejected because role assignment protection confirmation is part of the roadmap and spec.
