# Research: Phase 3 - Service Layer Extraction

**Status**: Complete

## Decision 1: Place EF-Backed Service Implementations in Infrastructure

**Decision**: Define business service interfaces in `HR.Application` and place Phase 3 concrete implementations in `HR.Infrastructure`.

**Rationale**: Phase 3 intentionally uses `ApplicationDbContext` directly until repositories are introduced in Phase 4. `ApplicationDbContext` and Identity belong to `HR.Infrastructure`. Placing implementations there follows the existing `IAuthService` / `AuthService` pattern and prevents a forbidden `HR.Application` to `HR.Infrastructure` project reference.

**Alternatives considered**:

- Put implementations in `HR.Application` and add an infrastructure reference: rejected because it reverses the required dependency direction.
- Introduce repositories now: rejected because repositories are explicitly Phase 4 scope.
- Keep logic in controllers: rejected because service extraction is the purpose of Phase 3.

## Decision 2: Preserve the Required Incremental Extraction Order

**Decision**: Extract departments, vacation requests, trips, and employees in that order, validating each feature before starting the next.

**Rationale**: Departments are the smallest feature and provide a low-risk pattern. Vacation requests and trips add filtering and generated values. Employees are last because they include Identity, transactions, related-record cleanup, and the largest behavioral surface.

**Alternatives considered**:

- Extract all controllers in one change: rejected because it increases regression risk and makes failures harder to isolate.
- Start with employees: rejected because the most complex feature should use patterns validated by smaller extractions.

## Decision 3: Normalize Pagination in the Shared Pagination Utility

**Decision**: Extend `PagedList<T>` so it consistently normalizes invalid page values and enforces the maximum page size.

**Rationale**: Every list endpoint has the same page rules. Keeping normalization close to the shared paging operation avoids repeated checks and ensures all services use identical behavior.

**Alternatives considered**:

- Repeat normalization in every service: rejected because duplication can create inconsistent edge-case handling.
- Add a new pagination abstraction: rejected because the existing utility is sufficient.

## Decision 4: Use One API-Layer Mapper for Expected Service Errors

**Decision**: Add a small API-layer mapping extension that converts `ServiceError` to the standard structured response and status code.

**Rationale**: Four controllers need the same translation. A single mapper keeps controllers thin and aligns with the existing global error response contract. Unexpected exceptions still flow to `GlobalExceptionMiddleware`.

**Alternatives considered**:

- Duplicate a private mapper in every controller: rejected because it repeats contract logic.
- Throw domain exceptions for all expected failures: rejected because the constitution requires `Result<T>` for expected write failures.
- Add a new error-handling library: rejected because the existing middleware and result types are sufficient.

## Decision 5: Preserve Existing HR Rules and Schema

**Decision**: Relocate only existing behavior. Do not introduce new entities, migrations, repositories, or Phase 5 rules.

**Rationale**: Phase 3 is already the largest behavioral refactor. Combining it with new rules or schema changes would make regressions harder to diagnose and violate the required phase order.

**Alternatives considered**:

- Add overlap checks, soft deletes, and state machines now: rejected because they belong to Phase 5.
- Split EF configurations now: rejected because that belongs to Phase 4.

## Decision 6: Keep Frontend Work Outside This Backend Phase

**Decision**: Document the paginated list envelope and query parameters, but do not modify Angular files.

**Rationale**: The active repository is the backend. Pagination intentionally changes list responses, so frontend integration must consume the new envelope separately.

**Alternatives considered**:

- Preserve raw arrays and add metadata headers: rejected because the approved pagination contract is a response envelope.
- Modify the separate frontend project during backend extraction: rejected because it expands scope and repository boundaries.

## Decision 7: Scale Verification to the Refactor Risk

**Decision**: Use per-feature build and manual API checkpoints, then add focused xUnit service coverage where task generation includes test scaffolding.

**Rationale**: No test project exists today. Phase 3 is high risk, so verification must be incremental. Automated tests should focus on pagination, expected failures, and employee transaction-sensitive behavior without introducing production dependencies.

**Alternatives considered**:

- Rely only on a final build: rejected because compilation cannot catch behavior changes.
- Introduce a broad end-to-end framework: rejected because it is unnecessary for this phase.
