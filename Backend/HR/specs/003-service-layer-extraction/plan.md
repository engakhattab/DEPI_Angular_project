# Implementation Plan: Phase 3 - Service Layer Extraction

**Branch**: `003-service-layer-extraction` | **Date**: 2026-06-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/003-service-layer-extraction/spec.md`

## Summary

Extract department, vacation request, trip, and employee behavior from API controllers into dedicated services while preserving completed authentication, authorization, middleware, entity, and database behavior. Service interfaces will live in `HR.Application`; EF Core and Identity dependent implementations will live in `HR.Infrastructure`, matching the existing `IAuthService` / `AuthService` pattern and preserving the dependency rules. Controllers will become thin HTTP adapters. All list operations will return the existing shared pagination envelope with normalized page inputs.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core 8, Entity Framework Core 8.0.20, ASP.NET Core Identity, Swashbuckle

**Storage**: SQL Server through the existing `ApplicationDbContext`

**Testing**: xUnit for focused service tests when introduced; `dotnet build`, Swagger/manual API regression checks, and authentication regression checks

**Target Platform**: Windows or Linux server hosted with Kestrel or IIS

**Project Type**: Layered ASP.NET Core Web API

**Performance Goals**: List operations query only the requested page and return no more than 100 records; write-operation behavior remains equivalent to the completed phases

**Constraints**: Preserve existing routes and successful single-record/write response DTOs; preserve secure cookie authentication; no schema migration; no new HR rules; no repositories until Phase 4; forward cancellation tokens wherever the called API supports them

**Scale/Scope**: Four existing business controllers, four new service interfaces, four new concrete service implementations, one shared API error-mapping helper, DI registration updates, and pagination normalization

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Layered Architecture (I)**: PASS. Interfaces remain in `HR.Application`. EF Core and Identity dependent implementations live in `HR.Infrastructure`. Controllers no longer depend directly on infrastructure data access.
- **Cookie-Based Session Authentication (II)**: PASS. Phase 2 cookie authentication, global authorization, JSON `401`/`403`, login, logout, and `/api/auth/me` remain unchanged.
- **Service Layer Separation (III)**: PASS. Write methods return `Result<T>` or `Result`; reads return DTOs or `PagedList<T>`; list endpoints gain pagination; async paths carry `CancellationToken`.
- **Domain Integrity (IV)**: PASS WITH PHASE BOUNDARY. Existing validations are relocated without adding Phase 5 business rules.
- **Global Error Handling (V)**: PASS. Expected service failures map to structured `{ "code", "message" }` responses; unexpected exceptions continue to flow to the existing middleware.
- **Data Access Abstraction (VI)**: PASS WITH PHASE BOUNDARY. Direct `ApplicationDbContext` use is permitted inside infrastructure service implementations until repositories are introduced in Phase 4.
- **Simplicity & YAGNI (VII)**: PASS. No MediatR, CQRS, repositories, new schema, or unrelated abstractions are introduced.

### Post-Design Re-check

The Phase 1 design artifacts preserve all gates. The only transitional design choice is concrete service placement in `HR.Infrastructure`; this is explicitly allowed for implementations that need data-access internals and avoids a forbidden `HR.Application` to `HR.Infrastructure` reference.

## Technical Approach

### 1. Extract One Feature at a Time

Implement and verify services in the required order:

1. Departments
2. Vacation requests
3. Trips
4. Employees

For each feature:

1. Add the service interface in `HR.Application`.
2. Add the concrete service in `HR.Infrastructure`.
3. Register the service in `HR.Infrastructure/DependencyInjection.cs`.
4. Rewrite the controller to inject the interface.
5. Verify build and endpoint behavior before proceeding.

### 2. Keep Interfaces in Application and Implementations in Infrastructure

The master plan requires services to use `ApplicationDbContext` directly during Phase 3 because repositories are deferred to Phase 4. `ApplicationDbContext` belongs to `HR.Infrastructure`, and `HR.Application` must not reference `HR.Infrastructure`. Therefore:

- Service interfaces and DTO contracts live in `HR.Application`.
- EF Core and Identity dependent service implementations live in `HR.Infrastructure`.
- API controllers depend only on service interfaces from `HR.Application`.
- Phase 4 may introduce repository interfaces and move pure orchestration logic if beneficial.

### 3. Normalize Pagination Centrally

Update `HR.Shared/Pagination/PagedList.cs` so pagination inputs are normalized consistently:

- `page <= 0` becomes `1`
- `pageSize <= 0` becomes `25`
- `pageSize > 100` becomes `100`

Each list service applies filters and ordering before paging. When entity results require post-query DTO mapping, create the entity page first and construct a DTO `PagedList<T>` with the normalized metadata.

### 4. Use Structured Service Results

Write operations return `Result<T>` or `Result`. Expected failures use `ServiceError`:

- `NotFound` -> HTTP `404`
- `Conflict` -> HTTP `409`
- `Validation` -> HTTP `400`
- `BusinessRule` -> HTTP `422`
- `Unauthorized` -> HTTP `401`
- `Forbidden` -> HTTP `403`
- `Internal` -> HTTP `500`

Add one API-layer mapper so all four controllers return `{ "code", "message" }`. Unexpected exceptions are not converted into expected failures; they continue to reach `GlobalExceptionMiddleware`, which logs them and returns the safe `SERVER_ERROR` response.

### 5. Preserve Existing Behavior by Feature

#### Departments

- Preserve alphabetical list ordering.
- Preserve unique-name conflict checks.
- Preserve not-found handling.
- Preserve refusal to delete a department with assigned employees.

#### Vacation Requests

- Preserve optional status and employee filters.
- Preserve newest-first ordering.
- Preserve start-date-before-end-date validation.
- Preserve employee existence validation.
- Preserve pending status on creation.
- Preserve existing unrestricted status update behavior; Phase 5 adds transition rules.

#### Trips

- Preserve newest-first ordering.
- Preserve generated `TRIP-xxxxxx` and `REQ-xxxxxx` identifiers.
- Preserve create, read, and delete behavior.

#### Employees

- Preserve optional status filtering and employee-number ordering.
- Preserve page-scoped identity lookups while mapping employee responses.
- Preserve employee-number uniqueness checks.
- Preserve department, manager-existence, and self-manager checks.
- Preserve generated temporary-password behavior.
- Preserve transactional employee and Identity creation.
- Preserve email synchronization with the Identity user.
- Preserve deletion cleanup for direct reports, vacation requests, employee records, and Identity users.
- Preserve logging for unexpected create/delete failures while allowing the global middleware to produce the safe response.

## Files and Modules Likely to Change

### Existing Files

```text
HR.API/
|-- Controllers/
|   |-- DepartmentsController.cs
|   |-- VacationRequestsController.cs
|   |-- TripsController.cs
|   `-- EmployeesController.cs
`-- Extensions/
    `-- ServiceErrorMappingExtensions.cs       # new

HR.Infrastructure/
|-- DependencyInjection.cs
|-- Departments/
|   `-- DepartmentService.cs                  # new
|-- VacationRequests/
|   `-- VacationRequestService.cs             # new
|-- Transportation/
|   `-- TripService.cs                        # new
`-- Employees/
    `-- EmployeeService.cs                    # new

HR.Application/
|-- Departments/
|   `-- IDepartmentService.cs                 # new
|-- VacationRequests/
|   `-- IVacationRequestService.cs            # new
|-- Transportation/
|   `-- ITripService.cs                       # new
`-- Employees/
    `-- IEmployeeService.cs                   # new

HR.Shared/
`-- Pagination/
    `-- PagedList.cs
```

### Files Expected to Remain Unchanged

```text
HR.API/Program.cs
HR.API/Controllers/AuthController.cs
HR.API/Middleware/GlobalExceptionMiddleware.cs
HR.Infrastructure/Auth/AuthService.cs
HR.Infrastructure/Data/ApplicationDbContext.cs
HR.Infrastructure/Data/Migrations/
HR.Domain/
```

`Program.cs` should remain unchanged because it already calls `AddInfrastructure()`. The infrastructure registration extension is the appropriate Phase 3 registration point.

## Data Model Changes

No persistent data model or database schema changes are included. Existing entities, relationships, EF configuration, and migrations remain unchanged. See [data-model.md](./data-model.md).

## API and Route Changes

No route, verb, authentication, or authorization changes are included.

The only intentional response-contract change is for list endpoints:

| Endpoint | Existing Response | Phase 3 Response |
|----------|-------------------|------------------|
| `GET /api/departments` | Array | Paginated result |
| `GET /api/vacationrequests` | Array | Paginated result |
| `GET /api/trips` | Array | Paginated result |
| `GET /api/employees` | Array | Paginated result |

Each list endpoint adds optional `page` and `pageSize` query parameters. Existing feature-specific filters remain available. See [contracts/api-contract.md](./contracts/api-contract.md).

## UI and Component Changes

No frontend files are changed in this backend phase. The Angular frontend must consume the paginated list envelope instead of a raw array when it is integrated with Phase 3.

## Validation and Error Handling

- Keep request DTO attribute validation at the API boundary through existing model-state handling.
- Move business checks and expected data conflicts from controllers into services.
- Return `Result<T>.Failure(ServiceError...)` for expected failures.
- Use the API mapping extension to return consistent `{ "code", "message" }` bodies.
- Let unexpected exceptions reach `GlobalExceptionMiddleware`.
- Forward `CancellationToken` through controller, service, EF Core query, save, and transaction paths where supported.
- Do not add Phase 5 rules such as overlap checks, soft delete, balance tracking, status state machines, or circular manager-chain detection.

## Testing and Check Strategy

### Per-Feature Checkpoint

After each controller extraction:

1. Run `dotnet build`.
2. Start `HR.API`.
3. Authenticate through `POST /api/auth/login`.
4. Exercise the extracted feature through Swagger or an HTTP client.
5. Verify successful operations, expected error responses, pagination boundaries, filters, ordering, and unauthenticated `401` behavior.

### Focused Automated Coverage

No test project currently exists. During implementation, add focused xUnit coverage if the task breakdown includes test scaffolding. Prioritize:

- Pagination normalization and metadata.
- Department conflict and delete-with-employees behavior.
- Vacation request date-range and employee-not-found behavior.
- Trip generated identifier shape and delete-not-found behavior.
- Employee uniqueness, relationship validation, temporary-password behavior, transactional create failure, email synchronization, and delete cleanup.
- Structured service-error to HTTP-response mapping.

Use the smallest suitable test setup. EF Core in-memory or SQLite-based test storage may be selected during task generation if automated database-backed tests are added; production packages and schema remain unchanged.

### Final Regression Check

- Run `dotnet build`.
- Confirm all four controllers no longer reference `ApplicationDbContext` or `UserManager<ApplicationUser>`.
- Confirm all async controller and service methods carry `CancellationToken` where applicable.
- Confirm login, logout, `/api/auth/me`, JSON `401`, JSON `403`, and global exception handling still behave as completed in Phase 2.

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Behavioral regressions during the largest refactor phase | Extract and verify one feature at a time in the required order. |
| Layering violation caused by direct data-context use | Keep EF-backed concrete services in `HR.Infrastructure`; expose interfaces from `HR.Application`. |
| Frontend breakage from paginated list responses | Document the envelope and query parameters explicitly; preserve all list filters and item DTO shapes. |
| Inconsistent expected-error responses | Use one API-layer `ServiceError` mapping helper for all extracted controllers. |
| Partial employee or Identity writes | Preserve transactions and execution strategy for multi-step employee operations. |
| Cancellation gaps | Add `CancellationToken` to controller and service signatures and forward it through supported async calls. |
| Accidentally introducing Phase 5 rules | Treat Phase 3 as behavior-preserving extraction only; list deferred rules explicitly in tasks and review. |

## Dependencies on Previous Phases

- **Phase 0**: Five-project structure, DTOs, entities, domain exceptions, `Result<T>`, and `ServiceError`.
- **Phase 1**: `GlobalExceptionMiddleware` and `PagedList<T>`.
- **Phase 2**: Cookie authentication, global authorization, `IAuthService`, `AuthService`, JSON `401`/`403`, and current-user session restoration.

## Out of Scope

- Repository interfaces and implementations.
- Per-entity EF Core configuration classes.
- Changes to `ApplicationDbContext.OnModelCreating`.
- Database migrations or entity changes.
- New HR business rules from Phase 5.
- DI cleanup beyond adding Phase 3 registrations to the existing infrastructure extension.
- Frontend code changes.
- New endpoints, new roles, RBAC, attendance, salary, documents, dashboard statistics, or audit logs.

## Project Structure

### Documentation (this feature)

```text
specs/003-service-layer-extraction/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- api-contract.md
|-- checklists/
|   `-- requirements.md
`-- tasks.md                 # generated later by /speckit-tasks
```

### Source Code (repository root)

```text
HR.API/                     # HTTP boundary and service-error mapping
HR.Application/             # DTOs and service interfaces
HR.Infrastructure/          # EF/Identity-backed service implementations and DI
HR.Domain/                  # unchanged entities, enums, and exceptions
HR.Shared/                  # Result<T>, ServiceError, pagination, serialization
```

**Structure Decision**: Continue the existing five-project layered solution. Introduce only the service interface and implementation folders needed for Phase 3.

## Complexity Tracking

No constitution violations require justification.
