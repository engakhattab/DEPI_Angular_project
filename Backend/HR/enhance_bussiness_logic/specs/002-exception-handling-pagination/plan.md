# Implementation Plan: Phase 1 — Global Exception Handling & Pagination Infrastructure

**Branch**: `002-exception-handling-pagination` | **Date**: 2026-05-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/002-exception-handling-pagination/spec.md`

## Summary

Add a global exception-handling middleware to centralize all API error responses into a consistent `{ "code", "message" }` JSON format, and create a reusable `PagedList<T>` utility in the shared layer for use by future paginated list endpoints. Both changes are purely additive — no existing endpoint behavior changes.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core 8, Entity Framework Core 8 (for `IQueryable` extensions in `PagedList<T>`)

**Storage**: SQL Server (unchanged; no new migrations)

**Testing**: xUnit (if tests are added)

**Target Platform**: Windows/Linux Server (Kestrel/IIS)

**Project Type**: ASP.NET Core WebAPI (layered architecture)

**Performance Goals**: Middleware overhead must be negligible (< 1ms added latency per request)

**Constraints**: Zero changes to existing API contracts. Middleware must be first in the pipeline.

**Scale/Scope**: Additive infrastructure — 2 new files, 1 pipeline registration change

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Layered Architecture (I)**: Middleware lives in `HR.API` (correct layer). `PagedList<T>` lives in `HR.Shared` (correct layer). ✅
- **Cookie-Based Session Auth (II)**: Not affected by this phase. ✅
- **Service Layer Separation (III)**: `PagedList<T>` prepares infrastructure for paginated service returns in Phase 3. ✅
- **Domain Integrity (IV)**: Not affected — no domain logic changes. ✅
- **Global Error Handling & API Consistency (V)**: This phase directly implements this principle — exception → HTTP status mapping, consistent error shape, server-side-only logging for 500s. ✅
- **Data Access Abstraction (VI)**: Not affected. ✅
- **Simplicity & YAGNI (VII)**: Only two types added, both mandated by PLAN.md. No unnecessary abstractions. ✅

## Project Structure

### Documentation (this feature)

```text
specs/002-exception-handling-pagination/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit-tasks)
```

### Source Code (repository root)

```text
HR/
├── HR.API/
│   ├── Middleware/
│   │   └── GlobalExceptionMiddleware.cs   ← NEW
│   └── Program.cs                         ← MODIFIED (register middleware)
│
└── HR.Shared/
    └── Pagination/
        └── PagedList.cs                   ← NEW
```

**Structure Decision**: Follows the existing 5-project layered architecture from Phase 0. Middleware is an API-layer concern; pagination is a shared utility.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

*No violations.*
