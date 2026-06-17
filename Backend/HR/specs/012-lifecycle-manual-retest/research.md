# Research: Phase 12 - Lifecycle Documentation and Manual Retest

## Decision: Use a Disposable Local Database for Manual Retest

**Decision**: Use `HrSystemDb_Phase12LifecycleTest` as the Phase 12 manual retest database.

**Rationale**: A disposable database lets the retest run from a known fresh state without mutating the current project database or relying on stale rows from previous manual testing. It also gives a clear place to apply existing approved migrations through Phase 11 and create local-only validation data.

**Alternatives considered**:

- Reuse `HrSystemDb`: rejected because it risks changing the user's active project data.
- Reuse `HrSystemDb_LifecycleTest`: rejected because prior validation history can hide fresh setup issues.
- Create a new migration: rejected because Phase 12 is documentation/manual validation only and the current schema already represents the required behavior.

## Decision: Documentation-First Implementation

**Decision**: Update `API_LIFECYCLE_TESTING_GUIDE.md` as the primary artifact, review/update `CLIENT_INSTALLATION_GUIDE.md` where stale migrations or permission notes exist, and record Phase 12 evidence in `specs/012-lifecycle-manual-retest/implementation-summary.md`.

**Rationale**: The roadmap identifies the lifecycle guide and client installation guide as the stale artifacts after scope-hardening phases. Keeping retest evidence inside the Phase 12 spec folder makes acceptance evidence reviewable without mixing runtime logs into task tracking.

**Alternatives considered**:

- Update only the lifecycle guide: rejected because client installation migration notes are known to risk staleness after Phase 10/11 migrations.
- Record results in `tasks.md`: rejected because task files are planning/tracking artifacts, not runtime failure logs or test evidence.

## Decision: Preserve Runtime Compatibility Boundary

**Decision**: Phase 12 does not modify source code, routes, response JSON, cookies, claims, status codes, error codes, schema, or migrations. Runtime defects discovered during manual retest are documented and paused for separate approval.

**Rationale**: This phase exists to validate and document completed behavior, not to harden another endpoint group. Separating documentation retest from defect fixes preserves reviewability and prevents accidental Phase 13 or Phase 9-11 rework.

**Alternatives considered**:

- Patch defects as soon as manual retest finds them: rejected because it would mix implementation remediation into a documentation/manual retest phase.
- Add Swagger annotations now: rejected because Swagger/OpenAPI response documentation is explicitly deferred to Phase 13.

## Decision: Manual Retest Uses Cookie-Preserving HTTP Tooling

**Decision**: The guide should support Swagger UI or another HTTP client that preserves authentication cookies between requests.

**Rationale**: The backend uses ASP.NET Core cookie-based sessions, not JWT. Manual role switching and authorization checks are only valid when the tester explicitly logs in as the intended actor and keeps that actor's cookie scoped to the request set.

**Alternatives considered**:

- Use bearer tokens: rejected because JWT is forbidden by the constitution and not used by the project.
- Use direct database checks only: rejected because database state does not prove controller/service authorization behavior.

## Decision: Required Evidence Format

**Decision**: Each manual retest scenario records actor, endpoint/action, setup dependency, expected status/result, actual status/result, pass/fail, and notes/follow-up.

**Rationale**: The goal is not just to write instructions but to prove the current implementation matches them. This structure is compact enough for manual execution and detailed enough to identify regressions in role scope, status code, or structured error behavior.

**Alternatives considered**:

- Free-form notes only: rejected because they do not prove scenario coverage.
- Full automated test suite creation: rejected because Phase 12 is manual documentation/retest and no new source test work is requested.
