# Implementation Plan: Phase 12 - Lifecycle Documentation and Manual Retest

**Branch**: `012-lifecycle-manual-retest` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/012-lifecycle-manual-retest/spec.md`

## Summary

Phase 12 updates the lifecycle testing and installation documentation to match completed Phase 8-11 authorization scope behavior, then executes and records a local fresh-database manual retest. The implementation approach is documentation-first: update `API_LIFECYCLE_TESTING_GUIDE.md`, review/update `CLIENT_INSTALLATION_GUIDE.md`, create Phase 12 manual retest evidence, and validate against a disposable local SQL Server database named `HrSystemDb_Phase12LifecycleTest` using existing approved migrations only.

No source code, route, DTO, cookie, claim, runtime behavior, database schema, or new migration changes are in scope. Any manual retest failure that indicates a runtime defect must be recorded as a follow-up and stopped for separate approval.

## Technical Context

**Language/Version**: Markdown documentation, PowerShell validation commands, .NET 8 backend context

**Primary Dependencies**: Existing HR backend solution (`HR.slnx`), ASP.NET Core cookie authentication, EF Core migration tooling, SQL Server LocalDB/SQLEXPRESS-compatible local SQL Server

**Storage**: Disposable local SQL Server database `HrSystemDb_Phase12LifecycleTest` for manual retest only; no committed connection-string change and no new schema/migration design

**Testing**: Manual API lifecycle retest through Swagger or a cookie-preserving HTTP client; validation commands include `dotnet restore`, `dotnet build -c Release`, `dotnet test -c Release`, EF database update with existing migrations, EF pending model check, and `git diff --check`

**Target Platform**: Local developer Windows environment for retest; documentation remains applicable to client deployment setup

**Project Type**: ASP.NET Core Web API backend with layered architecture and documentation/manual validation phase

**Performance Goals**: N/A for runtime performance because Phase 12 does not change runtime code. Manual retest documentation should remain executable by a developer in one local validation session.

**Constraints**: Documentation-only plus local validation evidence; no source code changes; no new migrations; no committed machine-specific connection strings; no production/customer credentials; no Phase 13 Swagger/OpenAPI pass

**Scale/Scope**: One lifecycle guide, one client installation guide, one Phase 12 manual retest checklist/evidence summary, and the required four-role local dataset (EMP001-EMP004)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate Result | Notes |
|-----------|-------------|-------|
| I. Layered Architecture | PASS | No production source changes; documentation must not move responsibilities between layers. |
| II. Cookie-Based Session Authentication | PASS | Manual retest must preserve cookie-auth expectations and document cookie-preserving clients. |
| III. Service Layer Separation | PASS | No service/controller implementation changes in this phase. |
| IV. Domain Integrity & Business Rules | PASS | Phase 12 validates already implemented rules; it does not introduce new business rules. |
| V. Global Error Handling & API Consistency | PASS | Documentation must preserve existing `{ code, message }` expectations and current statuses. |
| VI. Data Access Abstraction | PASS | No repository/EF source changes; local DB validation uses existing approved migrations only. |
| VII. Simplicity & YAGNI | PASS | Scope is limited to docs, manual retest checklist, and recorded evidence. |

No constitutional violations are introduced.

## Project Structure

### Documentation (this feature)

```text
specs/012-lifecycle-manual-retest/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- manual-retest-contract.md
|-- checklists/
|   |-- requirements.md
|-- spec.md
`-- tasks.md              # Created later by /speckit-tasks
```

### Repository Documentation Targets

```text
API_LIFECYCLE_TESTING_GUIDE.md       # Primary lifecycle/manual retest guide
CLIENT_INSTALLATION_GUIDE.md         # Client setup guide; update stale migration/permission notes if found
specs/012-lifecycle-manual-retest/
|-- implementation-summary.md        # Created during implementation to record retest evidence
`-- manual-retest-checklist.md       # Created during implementation from the contract/checklist requirements
```

### Source Code (repository root)

```text
HR.API/
HR.Application/
HR.Domain/
HR.Infrastructure/
HR.Shared/
HR.Tests/
```

**Structure Decision**: Source projects are read-only context for Phase 12. The implementation changes are limited to Markdown documentation and Spec Kit artifacts. Local SQL Server data can be created/reset for validation evidence, but no source files or new migration files are planned.

## Phase 0: Research Output

Completed in [research.md](./research.md).

## Phase 1: Design Output

Generated artifacts:

- [data-model.md](./data-model.md)
- [contracts/manual-retest-contract.md](./contracts/manual-retest-contract.md)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Check

| Principle | Gate Result | Notes |
|-----------|-------------|-------|
| I. Layered Architecture | PASS | Design artifacts do not alter layered source architecture. |
| II. Cookie-Based Session Authentication | PASS | Retest contract requires cookie-preserving login/session behavior. |
| III. Service Layer Separation | PASS | No runtime implementation tasks are planned. |
| IV. Domain Integrity & Business Rules | PASS | Retest verifies completed rules and records mismatches. |
| V. Global Error Handling & API Consistency | PASS | Manual scenarios require status and `{ code, message }` checks. |
| VI. Data Access Abstraction | PASS | Existing migrations only; no schema design work. |
| VII. Simplicity & YAGNI | PASS | Documentation/retest only; Phase 13 Swagger work remains deferred. |

## Complexity Tracking

No constitution violations require tracking.
