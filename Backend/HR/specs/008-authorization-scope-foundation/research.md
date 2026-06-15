# Phase 8 Research: Authorization Scope Foundation

## Decision: Reuse `IEmployeeAccessService` as the Shared Scope Contract

**Rationale**: The project already has an application-level `IEmployeeAccessService` and an infrastructure-backed `EmployeeAccessService`. Existing Phase 7 services already depend on it for attendance, compensation, documents, dashboard, and audit access. Reusing it keeps Phase 8 aligned with the architecture and avoids a second scope abstraction.

**Alternatives considered**:

- Add controller-only authorization policies: rejected because `[Authorize]` cannot answer ownership or manager-chain questions.
- Add a new authorization service beside `IEmployeeAccessService`: rejected because it would duplicate existing scope logic and increase later phase drift.
- Move implementation into `HR.Application`: rejected because manager hierarchy queries require repository/data access internals owned by `HR.Infrastructure`.

## Decision: Make Named Scope Decisions Explicit

**Rationale**: The current service already supports role checks, employee access checks, and visible employee IDs, but the Phase 8 spec names additional decisions that later phases need to reference directly. Explicit method names reduce ambiguity when Phases 9, 10, and 11 harden endpoints.

**Alternatives considered**:

- Leave only `CanAccessEmployeeAsync` and document implied behavior: rejected because later phases would need to infer self, manager, and organization decisions.
- Create extension methods only in tests: rejected because implementation phases need a stable application contract.

## Decision: Direct Plus Indirect Team Scope

**Rationale**: The roadmap prefers direct plus indirect reports when safe. The existing repository already has `GetDirectAndIndirectReportIdsAsync`, and current tests already cover direct and indirect reports.

**Alternatives considered**:

- Direct-only manager scope: rejected because current code already supports hierarchy traversal and the spec clarified direct plus indirect scope.
- Department-based scope: rejected because department mismatch alone does not define manager access in the current business model.

## Decision: Use Existing Employee Role and Status Data

**Rationale**: `Employee.Role`, `Employee.Status`, `Employee.IsDeleted`, and `Employee.ManagerId` already represent the Phase 8 scope model. No new schema is needed.

**Alternatives considered**:

- Add a separate scope table: rejected as unnecessary.
- Add hierarchy cache tables: rejected because current scale and existing tests do not justify cached scope state.

## Decision: Suspended Requesters Remain Scope-Eligible in Phase 8

**Rationale**: The clarification decision says suspended users remain scope-eligible unless deleted or terminated. Endpoint-specific phases decide which suspended actions are forbidden.

**Alternatives considered**:

- Treat suspended requesters as denied for all scope checks: rejected because it would change shared foundation behavior beyond the clarified Phase 8 requirement.
- Treat suspended targets as visible team members by default: rejected because the spec excludes suspended employees from team data sets unless a later endpoint-specific phase documents an exception.

## Decision: No HTTP API Contract Changes

**Rationale**: Phase 8 is a foundation phase. Public employee, vacation, and trip behavior remains intentionally unchanged until later phases.

**Alternatives considered**:

- Add diagnostic scope endpoints: rejected as unnecessary and risky for public contract stability.
- Harden employee/vacation/trip endpoints now: rejected because those are explicitly Phase 9, Phase 10, and Phase 11.

## Decision: No Migration

**Rationale**: Existing schema already supports roles, status/deletion state, and manager relationships.

**Alternatives considered**:

- Add new scope columns or tables: rejected because no Phase 8 business rule requires new persistence.

