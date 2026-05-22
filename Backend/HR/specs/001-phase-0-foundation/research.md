# Phase 0 Research

**Feature**: Phase 0 — Foundation & Project Restructure
**Status**: Complete

## Summary of Findings

This feature consists entirely of structural refactoring to establish a .NET 8 layered architecture. The target architecture, projects, and dependency rules are explicitly defined in the `PLAN.md` and project constitution. 

No areas required clarification or technical research. The approach relies on standard `.NET Core` project referencing and namespace reorganization.

## Decisions

### 1. Project Creation
- **Decision**: Create four new `.NET 8` Class Libraries (`HR.Domain`, `HR.Application`, `HR.Infrastructure`, `HR.Shared`).
- **Rationale**: Meets Constitution Principle I for a 5-project layered architecture (with the existing `HR.API` project).
- **Alternatives**: None considered. Mandated by `PLAN.md`.

### 2. Dependency Management
- **Decision**: Remove all NuGet packages from `HR.Domain`.
- **Rationale**: Ensures the domain is pure and portable.
- **Alternatives**: Leaving EF Core annotations in the domain was rejected; configuration will be moved to `HR.Infrastructure` in a later phase.

### 3. API Compatibility
- **Decision**: Controllers remain in `HR.API` unchanged, injecting `ApplicationDbContext` directly.
- **Rationale**: Phase 0 is purely structural. Moving logic out of controllers is reserved for Phase 3 to minimize risk.
