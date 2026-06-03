# Phase 0 Research: Phase 5 - HR Business Logic Improvements

## Decision: Build on the Phase 4 repository and unit-of-work boundaries

**Rationale**: Phase 4 already introduced tailored repositories, `IUnitOfWork`, and EF entity configurations. Phase 5 business rules need persistence queries but should not reintroduce `ApplicationDbContext` into services.

**Alternatives considered**:

- Direct service `ApplicationDbContext` access: rejected because it violates the active data-access abstraction.
- Generic repository: rejected because the constitution explicitly prefers tailored repositories.

## Decision: Add one Phase 5 EF migration for required persistent state

**Rationale**: Vacation balance, soft deletion, termination time, reviewer audit, stored working-day count, and trip requester ownership must survive process restarts and be queryable. Existing migrations must remain unchanged.

**Alternatives considered**:

- In-memory calculation only: rejected because balance restoration and audit data must be durable.
- Editing the initial migration: rejected by migration discipline.

## Decision: Store `WorkingDayCount` on each vacation request

**Rationale**: Balance restoration must return the exact amount deducted when an approved request later becomes rejected. Storing the calculated count prevents later rule or calendar changes from altering historical balances.

**Alternatives considered**:

- Recalculate from `StartDate` and `EndDate` during cancellation: rejected because it can drift if calendar rules change later.
- Store only deducted amount on the employee: rejected because the request audit would not explain the balance change.

## Decision: Use a small working-day helper and .NET `TimeProvider`

**Rationale**: Sunday through Thursday working-day rules are shared by vacation duration, vacation notice, and trip scheduling. `TimeProvider` is built into .NET 8 and makes date-dependent tests deterministic without a custom clock abstraction.

**Alternatives considered**:

- Duplicate date loops in each service: rejected because notice and duration rules could drift.
- Hard-code `DateTimeOffset.UtcNow` in services: rejected because tests would be time-dependent.

## Decision: Filter soft-deleted employees explicitly in normal employee queries

**Rationale**: The spec requires normal employee results to exclude soft-deleted profiles while historical reporting can still retain and reference them. Explicit repository filters avoid broad global query-filter side effects on vacation/trip historical navigation data.

**Alternatives considered**:

- Global `HasQueryFilter(e => !e.IsDeleted)`: rejected because it can hide removed employees from historical vacation and trip records unless every historical query remembers `IgnoreQueryFilters`.
- Hard delete employees: rejected because Phase 5 requires retained profiles and retained login identity association.

## Decision: Treat termination and soft deletion as separate lifecycle outcomes

**Rationale**: The clarified spec states that termination records `TerminatedAt`, rejects pending vacations, revokes access, and keeps the profile visible. Soft deletion is a stronger action that also hides the retained profile from normal employee results.

**Alternatives considered**:

- Make termination automatically hide the employee: rejected because terminated-but-not-soft-deleted employees must remain visible.
- Keep hard delete behavior for removals: rejected because employee history and login identity association must be retained.

## Decision: Reject sign-in and existing sessions through employee status checks

**Rationale**: New sign-ins can be denied in `AuthService`. Existing cookies must be rejected immediately, so cookie `OnValidatePrincipal` should call a narrow session validator that checks the `employee_id` claim against active, non-deleted employee state.

**Alternatives considered**:

- Delete or disable the Identity user: rejected because soft-deleted employees must retain their login identity association for reporting and audit.
- Wait for cookie expiry or logout: rejected by the clarification requiring immediate rejection.

## Decision: Pass reviewer identity from authenticated claims, not request body

**Rationale**: The reviewer is security context. The API can read the existing `employee_id` claim and pass it to the service. The request body remains only the requested status, preventing clients from spoofing reviewer identity.

**Alternatives considered**:

- Add `ReviewedByEmployeeId` to the request DTO and trust it: rejected because clients could approve as someone else.
- Require manager/RBAC authorization: rejected because Phase 5 explicitly allows any authenticated employee except the requester.

## Decision: Enforce duplicate active email in the service layer with repository support

**Rationale**: The constitution requires invalid duplicates to be rejected before reaching the database. The repository should expose an active-email existence check that excludes the current employee and soft-deleted profiles.

**Alternatives considered**:

- Rely only on a database unique constraint: rejected because the rule applies only to active, non-deleted employees and must produce structured service errors.
- Treat soft-deleted profiles as duplicates: rejected by the spec.

## Decision: Keep Phase 5 API route surface unchanged with additive DTO fields

**Rationale**: Existing controllers already expose create, update, delete, list, and detail routes. The spec requires new behavior and a few extra fields, not new endpoint families.

**Alternatives considered**:

- Add separate approval/cancellation endpoints: rejected because the existing status update endpoint can enforce the state machine.
- Add historical reporting endpoints: rejected as outside Phase 5.

## Decision: Count department employees as current non-deleted profiles

**Rationale**: Department counts must exclude soft-deleted employees. The spec does not exclude terminated-but-visible employees, so those remain counted until a soft-delete/removal action hides them.

**Alternatives considered**:

- Count only active employees: rejected because the requirement says exclude soft-deleted profiles, not terminated profiles.
- Persist `EmployeeCount` on `Department`: rejected because it is derived data and can be projected from employee records.
