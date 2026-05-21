# Implementation Plan: Phase 0 — Foundation & Project Restructure

**Branch**: `001-phase-0-foundation` | **Date**: 2026-05-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-phase-0-foundation/spec.md`

## Summary

Restructure the existing ASP.NET Core application into a clean layered architecture (`HR.API`, `HR.Application`, `HR.Infrastructure`, `HR.Domain`, `HR.Shared`) without breaking any existing functionality. Introduce common domain exceptions and Result types to Shared/Domain to prepare for subsequent refactoring phases.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core 8, Entity Framework Core 8, ASP.NET Core Identity

**Storage**: SQL Server

**Testing**: xUnit

**Target Platform**: Windows/Linux Server (Kestrel/IIS)

**Project Type**: ASP.NET Core WebAPI + Class Libraries

**Performance Goals**: Identical to pre-refactor performance

**Constraints**: Zero external dependencies in `HR.Domain`. 100% backward compatibility for API endpoints.

**Scale/Scope**: Refactoring existing system

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Layered Architecture (I)**: Plan creates the exact 5 projects specified. `HR.Domain` has no external dependencies. ✅
- **Cookie-Based Session Auth (II)**: Deferred to Phase 2 (unchanged here). ✅
- **Service Layer Separation (III)**: Prepares `Result<T>` and `ServiceError` types in `HR.Shared` for Phase 3. ✅
- **Domain Integrity (IV)**: Prepares domain exceptions (`NotFoundException`, etc.) in `HR.Domain`. ✅
- **Data Access Abstraction (VI)**: Moves `ApplicationDbContext` and migrations to `HR.Infrastructure`. ✅
- **Simplicity & YAGNI (VII)**: Direct standard class library project references without MediatR/CQRS. ✅

## Project Structure

### Documentation (this feature)

```text
specs/001-phase-0-foundation/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
HR/
├── HR.API/ (existing project)
│   ├── Controllers/
│   └── Program.cs
├── HR.Application/
├── HR.Infrastructure/
│   └── Data/
│       ├── ApplicationDbContext.cs
│       └── Migrations/
├── HR.Domain/
│   ├── Entities/
│   ├── Enums/
│   └── Exceptions/
└── HR.Shared/
    └── Results/
```

**Structure Decision**: 5-project layered solution conforming to the project Constitution Principle I.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

*No violations.*
