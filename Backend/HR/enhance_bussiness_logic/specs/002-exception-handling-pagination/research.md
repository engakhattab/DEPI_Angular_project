# Phase 1 Research

**Feature**: Phase 1 — Global Exception Handling & Pagination Infrastructure
**Status**: Complete

## Summary of Findings

This phase adds two additive infrastructure components to the existing layered architecture. No unknowns or clarifications were needed — the PLAN.md specifies exact implementations, and ASP.NET Core middleware and EF Core `IQueryable` extensions are well-established patterns.

## Decisions

### 1. Exception-to-HTTP Mapping Strategy
- **Decision**: Use a custom ASP.NET Core middleware (`GlobalExceptionMiddleware`) that wraps the entire pipeline in a try-catch, mapping domain exceptions to specific HTTP status codes.
- **Rationale**: Simpler than `IExceptionHandler` (new in .NET 8) for this use case — a single middleware class with explicit catch blocks is easier for a cheaper LLM to implement correctly and easier to debug. Constitution Principle V mandates this middleware be first in the pipeline.
- **Alternatives considered**:
  - `IExceptionHandler` (.NET 8): More idiomatic but adds indirection. Rejected for simplicity (Constitution Principle VII).
  - `UseExceptionHandler` built-in: Does not give fine-grained control over domain exception types. Rejected.

### 2. Pagination Utility Location
- **Decision**: Place `PagedList<T>` in `HR.Shared/Pagination/` as a standalone class with a static `CreateAsync` factory method that operates on `IQueryable<T>`.
- **Rationale**: `HR.Shared` is referenced by all layers, making the type available to both `HR.Application` (services) and `HR.Infrastructure` (repositories). The `IQueryable<T>` dependency requires EF Core's `Microsoft.EntityFrameworkCore` package in `HR.Shared`.
- **Alternatives considered**:
  - Placing in `HR.Application`: Would work but limits reuse. `HR.Shared` is the canonical location per Constitution Principle I.
  - Separate `IPaginatable` interface: Over-engineering for the current need (Constitution Principle VII).

### 3. Error Response Format
- **Decision**: Use anonymous object serialization: `new { code, message }`. No dedicated `ErrorResponse` DTO class.
- **Rationale**: The middleware uses `WriteAsJsonAsync(new { code, message })`, which produces the exact `{ "code": "...", "message": "..." }` shape. A separate DTO class adds no value for this simple two-field contract (Constitution Principle VII).
- **Alternatives considered**:
  - Typed `ErrorResponse` record: Adds a class for no gain. Can always be introduced later if the error shape grows.
  - `ProblemDetails` (RFC 7807): Different shape from what PLAN.md specifies. Rejected to maintain backward compatibility with the specified format.
