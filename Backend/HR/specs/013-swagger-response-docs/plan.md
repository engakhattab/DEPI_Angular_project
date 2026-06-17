# Implementation Plan: Phase 13 - Swagger/OpenAPI Response Documentation Pass

**Branch**: `013-swagger-response-docs` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/013-swagger-response-docs/spec.md`

## Summary

Phase 13 adds accurate Swagger/OpenAPI response documentation for the existing HR backend endpoints so expected success and error responses no longer appear as `Undocumented`. The implementation approach is controller metadata only: inspect the current controller/service outcomes, add accurate response documentation annotations and any inert documentation-only response schema needed to describe the existing `{ code, message }` error shape, then verify through build, tests, Swagger UI, and OpenAPI output review.

No business logic, route, request DTO, response DTO runtime shape, authentication, authorization, validation rule, database schema, migration, seed data, or lifecycle-retesting behavior is in scope. Any discovered behavior mismatch must be recorded as follow-up work instead of being fixed in Phase 13.

## Technical Context

**Language/Version**: C# on .NET 8 / ASP.NET Core 8 Web API

**Primary Dependencies**: Existing HR backend solution (`HR.slnx`), ASP.NET Core MVC controllers, ASP.NET Core cookie authentication/authorization, Swashbuckle.AspNetCore 6.6.2, existing HR.Shared result/error conventions

**Storage**: N/A for Phase 13. No database schema, seed data, connection string, or EF Core migration changes are planned.

**Testing**: `dotnet restore`, `dotnet build -c Release`, `dotnet test -c Release`, manual Swagger UI review, OpenAPI JSON review, and `git diff --check`

**Target Platform**: Local developer Windows environment for validation; generated Swagger/OpenAPI documentation remains applicable to deployed API hosts

**Project Type**: ASP.NET Core Web API backend with layered architecture and controller-level API documentation update

**Performance Goals**: No runtime performance change. Swagger UI and OpenAPI JSON should remain loadable during local validation.

**Constraints**: Behavior-neutral documentation pass only; preserve cookie-based sessions, global JSON 401/403 behavior, structured `{ code, message }` errors, current HTTP statuses, controller routes, DTO shapes, pagination envelopes, file upload/download behavior, and all Phase 8-12 completed behavior.

**Scale/Scope**: Ten controller groups and their current operations: Auth, Employees, Departments, Attendance, Vacation Requests, Trips, Compensation, Employee Documents, Dashboard, and Audit Logs.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate Result | Notes |
|-----------|-------------|-------|
| I. Layered Architecture | PASS | Phase 13 touches API documentation metadata only. Any optional documentation-only error schema must stay dependency-safe and must not move business logic across layers. |
| II. Cookie-Based Session Authentication | PASS | Documentation must describe existing cookie-auth 401/403 behavior and must not introduce bearer-token/JWT expectations. |
| III. Service Layer Separation | PASS | No business logic enters controllers; controllers remain HTTP adapters. Response metadata must not change service contracts. |
| IV. Domain Integrity & Business Rules | PASS | Current domain/business outcomes are documented but not changed. Runtime mismatches become follow-up findings. |
| V. Global Error Handling & API Consistency | PASS | Phase 13 documents the existing `{ code, message }` shape and compatibility error codes without normalizing or renaming them. |
| VI. Data Access Abstraction | PASS | No repositories, EF Core context usage, migrations, or database access changes are planned. |
| VII. Simplicity & YAGNI | PASS | Prefer direct endpoint response annotations over broad custom Swagger filters unless implementation proves a small shared helper is necessary to avoid duplication. |

No constitutional violations are introduced.

## Project Structure

### Documentation (this feature)

```text
specs/013-swagger-response-docs/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- openapi-response-documentation-contract.md
|-- checklists/
|   `-- requirements.md
|-- spec.md
`-- tasks.md              # Created later by /speckit-tasks
```

### Source Code Targets (repository root)

```text
HR.API/
|-- Controllers/
|   |-- AuthController.cs
|   |-- EmployeesController.cs
|   |-- DepartmentsController.cs
|   |-- AttendanceController.cs
|   |-- VacationRequestsController.cs
|   |-- TripsController.cs
|   |-- CompensationController.cs
|   |-- EmployeeDocumentsController.cs
|   |-- DashboardController.cs
|   `-- AuditLogsController.cs
|-- Extensions/
|   `-- ServiceErrorMappingExtensions.cs       # Read-only source of current error/status behavior
|-- Middleware/
|   `-- GlobalExceptionMiddleware.cs           # Read-only source of unhandled exception error shape
`-- Program.cs                                # Read-only Swagger/cookie-auth setup context

HR.Application/
|-- DTOs/                                     # Existing success response/request DTOs to reference in documentation
`-- */I*Service.cs                            # Read-only source of Result/PagedList contracts

HR.Shared/
|-- Pagination/PagedList.cs                   # Existing paged response shape
`-- Results/ServiceError.cs                   # Existing error categories and compatibility codes

HR.Tests/                                    # Existing automated test suite; add focused documentation tests only if useful
```

**Structure Decision**: Implementation is limited to API-layer response documentation metadata and any minimal documentation-only schema needed to represent the existing structured error response. No service, repository, domain, migration, or infrastructure behavior changes are planned. Existing controller and service code is the source of truth for which statuses and payloads should be documented.

## Phase 0: Research Output

Completed in [research.md](./research.md).

## Phase 1: Design Output

Generated artifacts:

- [data-model.md](./data-model.md)
- [contracts/openapi-response-documentation-contract.md](./contracts/openapi-response-documentation-contract.md)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Check

| Principle | Gate Result | Notes |
|-----------|-------------|-------|
| I. Layered Architecture | PASS | The design keeps Phase 13 at the API documentation surface and does not change layer responsibilities. |
| II. Cookie-Based Session Authentication | PASS | The contract requires cookie-auth 401/403 documentation and forbids bearer-token assumptions. |
| III. Service Layer Separation | PASS | No service or controller business logic changes are designed. |
| IV. Domain Integrity & Business Rules | PASS | Current business-rule statuses are documented only; behavior defects are deferred. |
| V. Global Error Handling & API Consistency | PASS | The design records existing structured error payloads without changing status or code compatibility. |
| VI. Data Access Abstraction | PASS | No data-access artifacts are introduced. |
| VII. Simplicity & YAGNI | PASS | The plan favors explicit controller documentation and a small verification matrix over new Swagger infrastructure. |

## Complexity Tracking

No constitution violations require tracking.
