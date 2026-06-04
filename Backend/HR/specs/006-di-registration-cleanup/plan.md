# Implementation Plan: Phase 6 - DI Registration Cleanup

**Branch**: `006-di-registration-cleanup` | **Date**: 2026-06-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/006-di-registration-cleanup/spec.md`

## Summary

Move dependency-registration ownership out of the host startup surface and into the layers that own those dependencies. Add an application-layer registration entry point for future application-owned services, update infrastructure registration to accept configuration and own EF Core, Identity store integration, repositories, time/rule helpers, unit-of-work boundaries, session validation, and infrastructure-backed application service implementations, then simplify `Program.cs` to compose `AddApplication()` and `AddInfrastructure(builder.Configuration)` while preserving all Phase 5 runtime behavior.

Phase 6 is wiring-only. It must not add tables, columns, migrations, seed data, routes, request/response changes, authorization changes, or new business rules.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core 8, Entity Framework Core 8.0.20, ASP.NET Core Identity, Swashbuckle, Microsoft.Extensions.DependencyInjection abstractions, Microsoft.Extensions.Configuration abstractions

**Storage**: Existing SQL Server database through `ApplicationDbContext`; no new schema changes. SQLite in-memory remains the automated test store.

**Testing**: xUnit, `dotnet restore`, `dotnet build`, `dotnet test`, DI/static registration scans, no-new-migration check, and focused local SQL Server smoke validation after the Phase 5 migration is applied.

**Target Platform**: Windows or Linux server hosted with Kestrel or IIS

**Project Type**: Layered ASP.NET Core Web API

**Performance Goals**: No measurable runtime performance change; startup should continue resolving all services and serving representative HR requests without additional user-facing latency.

**Constraints**: Preserve routes, JSON response shapes, cookie/session settings, claims, structured error payloads, Phase 5 business rules, cancellation behavior, scoped lifetimes, transaction behavior, and configured database target. Do not create Phase 6 migrations or schema changes.

**Scale/Scope**: One application registration entry point, one infrastructure registration entry point update, one startup composition cleanup, focused DI regression coverage, static scans, and local database smoke verification.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Layered Architecture (I)**: PASS. `HR.Application` gains only an application-owned registration extension and does not reference `HR.Infrastructure`. `HR.Infrastructure` continues to own EF Core, Identity, repositories, and infrastructure service implementations. `HR.Shared` remains EF-free.
- **Cookie-Based Session Authentication (II)**: PASS. Cookie/session configuration stays host-owned in `Program.cs`; login, logout, `/api/auth/me`, session validation, secure cookie settings, and JSON `401`/`403` behavior are preserved.
- **Service Layer Separation (III)**: PASS. Service interfaces remain in `HR.Application`; infrastructure-backed implementations remain in `HR.Infrastructure` because they require data-access internals. Controllers remain thin HTTP adapters.
- **Domain Integrity (IV)**: PASS. Phase 5 business rules remain active and are not changed by this wiring cleanup.
- **Global Error Handling (V)**: PASS. `GlobalExceptionMiddleware` remains first in the pipeline and structured error response compatibility is preserved.
- **Data Access Abstraction (VI)**: PASS. Phase 6 activates full DI ownership cleanup: `HR.Application` and `HR.Infrastructure` each expose project-owned registration entry points, and `Program.cs` no longer directly registers application/infrastructure scoped services or persistence services.
- **Simplicity & YAGNI (VII)**: PASS. No generic service registry, MediatR, CQRS, new container, reflection scanner, source generator, schema change, or Phase 7 feature is introduced.

### Post-Design Re-check

The design artifacts preserve all gates. The only dependency addition is the minimal abstraction package needed for `HR.Application` to expose `IServiceCollection` extension methods without referencing infrastructure. No constitution violations require complexity tracking.

## Technical Approach

### 1. Add Application Registration Entry Point

Create `HR.Application/DependencyInjection.cs` with `AddApplication(this IServiceCollection services)`.

Current Phase 6 clarification: application service contracts remain application-owned, while infrastructure-backed implementations remain registered by infrastructure. Therefore `AddApplication()` may be intentionally empty for now, returning `services` after future-proofing the composition boundary. If application-only validators or policies appear later, they belong here.

Add the smallest required dependency for the extension method type if the project does not already expose it transitively.

### 2. Move Infrastructure Composition Into Infrastructure Registration

Update `HR.Infrastructure/DependencyInjection.cs` from `AddInfrastructure(this IServiceCollection services)` to `AddInfrastructure(this IServiceCollection services, IConfiguration configuration)`.

Infrastructure registration owns:

1. `ApplicationDbContext` SQL Server registration using `DefaultConnection`.
2. ASP.NET Core Identity store integration and the existing password option values.
3. Repositories: departments, vacation requests, trips, employees, identity lookup, and unit-of-work boundaries.
4. Infrastructure-backed service implementations for authentication, departments, employees, vacation requests, and trips.
5. Phase 5 support services: `TimeProvider.System`, `WorkingDayCalendar`, and `IEmployeeSessionValidator`.

Keep cookie authentication events in the host because they are request-pipeline behavior and resolve `IEmployeeSessionValidator` from the request service provider.

### 3. Simplify Host Startup Composition

Update `HR.API/Program.cs` so it:

- Calls `builder.Services.AddApplication()`.
- Calls `builder.Services.AddInfrastructure(builder.Configuration)`.
- Removes direct `AddDbContext`, `AddIdentityCore`, Identity role/store setup, and infrastructure/application scoped service registrations from the host surface.
- Keeps host-owned authentication cookie settings, authorization, controllers, JSON converters, CORS, Swagger, middleware order, and endpoint mapping unchanged.

This preserves API composition while making ownership reviewable.

### 4. Add Focused Registration Regression Coverage

Add tests that build a service provider through the Phase 6 registration entry points and prove representative required services resolve:

- `IAuthService`
- `IEmployeeService`
- `IDepartmentService`
- `IVacationRequestService`
- `ITripService`
- `IEmployeeSessionValidator`
- repositories and `IUnitOfWork`
- `TimeProvider`
- `WorkingDayCalendar`
- `ApplicationDbContext` configured against SQLite or a test-safe provider where appropriate

Also add or preserve controller/auth compatibility tests to ensure login response, claims, and structured errors remain stable.

Automated test provider strategy:

- Production `AddInfrastructure(configuration)` owns SQL Server `ApplicationDbContext` registration through `DefaultConnection`.
- Existing automated tests continue to use SQLite through `HR.Tests/TestInfrastructure/SqliteTestEnvironment.cs`.
- The test harness must avoid double-registering conflicting SQL Server and SQLite `ApplicationDbContext` providers in the same service provider.
- The preferred implementation is an explicit test-safe registration path or service replacement contained in test infrastructure; production registration must not depend on test flags, SQLite defaults, or environment-specific branching.
- DI resolution tests may use a test configuration only when the provider setup is isolated from existing SQLite integration tests.

### 5. Validate Scope Boundaries

Run static checks that prove:

- `Program.cs` has no direct `AddScoped<>` calls.
- `Program.cs` has no direct `AddDbContext` or `AddIdentityCore` calls.
- `HR.Application` does not reference `HR.Infrastructure`.
- Service classes do not reference `ApplicationDbContext`.
- No Phase 6 migration files were added.
- Existing Phase 5 business-rule tests still pass.

### 6. Local Database Smoke Validation

Before local SQL Server smoke validation, ensure the Phase 5 migration is applied to `HrSystemDb`. Phase 6 itself does not create a migration.

Use the local database only for startup and representative workflow smoke checks:

- login/session validation
- departments
- employees
- vacation requests
- trips

Record environment-specific notes in handoff or completion summary instead of task-tracking docs.

## Files and Modules Likely to Change

### Existing Files

```text
HR.API/
|-- Program.cs

HR.Application/
`-- HR.Application.csproj

HR.Infrastructure/
|-- DependencyInjection.cs
`-- HR.Infrastructure.csproj

HR.Tests/
|-- Auth/AuthControllerCompatibilityTests.cs
|-- Compatibility/ErrorResponseParityTests.cs
`-- TestInfrastructure/SqliteTestEnvironment.cs

AGENTS.md
PLAN.md
```

### New Files

```text
HR.Application/
`-- DependencyInjection.cs

HR.Tests/
`-- DependencyInjection/
    |-- DependencyRegistrationTests.cs
    `-- RegistrationBoundaryTests.cs
```

### Files Expected to Remain Unchanged

```text
HR.Domain/
HR.Shared/
HR.Infrastructure/Data/Migrations/
HR.API/Controllers/
HR.Application/DTOs/
```

## Data Model Changes

None. See [data-model.md](./data-model.md).

## Internal Contracts

See [contracts/di-registration-contracts.md](./contracts/di-registration-contracts.md).

## API and Route Changes

No route, verb, request DTO, response DTO, pagination envelope, cookie, claim, status code, structured error payload, authorization policy, or business-rule behavior changes are included.

## UI and Component Changes

No frontend or UI changes are included.

## Validation and Error Handling

- Preserve the current middleware order with global exception handling first.
- Preserve JSON `401` and `403` behavior for cookie authentication challenges.
- Preserve `OnValidatePrincipal` session rejection for terminated or soft-deleted employees.
- Preserve `Result<T>` expected-failure mapping and compatibility error codes.
- Preserve cancellation-token forwarding and scoped lifetimes.
- Fail startup clearly if `DefaultConnection` is missing, matching the current behavior.

## Testing and Check Strategy

### Automated Checks

1. Run `dotnet restore .\HR.slnx`.
2. Run `dotnet build .\HR.slnx -c Release`.
3. Run `dotnet test .\HR.slnx -c Release --no-build`.
4. Run focused DI registration tests.
5. Run focused auth compatibility and structured-error compatibility tests.

### Static Checks

```powershell
rg -n "AddScoped<" .\HR.API\Program.cs
rg -n "AddDbContext|AddIdentityCore|AddEntityFrameworkStores" .\HR.API\Program.cs
rg -n "ApplicationDbContext" .\HR.Infrastructure -g "*Service.cs"
rg -n "HR.Infrastructure" .\HR.Application
git status --short .\HR.Infrastructure\Data\Migrations
git diff --check
```

Expected results:

- The first two searches return no matches.
- The service search returns no direct `ApplicationDbContext` service dependencies.
- The application search returns no infrastructure references.
- Migration status shows no Phase 6 migration additions or edits.
- `git diff --check` reports no whitespace errors, allowing existing line-ending warnings if present.

### Manual Regression

Use [quickstart.md](./quickstart.md) after applying the existing Phase 5 migration to the local `HrSystemDb`. Complete representative smoke checks for startup, login/session behavior, departments, employees, vacation requests, trips, and structured errors.

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Moving Identity registration changes password policy or store behavior | Copy existing password options and role/store integration into infrastructure registration and run auth compatibility tests. |
| Moving `ApplicationDbContext` registration breaks EF tools or startup | Keep `DefaultConnection` lookup behavior and validate `dotnet ef`/startup against the configured startup project. |
| Empty `AddApplication()` looks unnecessary | Document the clarified ownership decision and keep the extension as the project-owned entry point for future application-only registrations. |
| Host-owned cookie events lose access to `IEmployeeSessionValidator` | Keep cookie events in `Program.cs` and resolve the validator from request services after infrastructure registration. |
| Phase 6 accidentally introduces schema or behavior changes | Add no-migration checks, full automated tests, and focused local database smoke validation. |

## Dependencies on Previous Phases

- **Phase 0**: Five-project structure.
- **Phase 1**: Global exception middleware and pagination support.
- **Phase 2**: Cookie/session authentication.
- **Phase 3**: Service interfaces and thin controllers.
- **Phase 4**: Repository pattern, unit-of-work boundary, entity configurations, and existing infrastructure registration.
- **Phase 5**: Business rules, TimeProvider/WorkingDayCalendar dependencies, employee session revocation, and Phase 5 migration readiness for local database smoke checks.

## Out of Scope

- New database tables, columns, constraints, seed data, or migrations.
- Phase 7 attendance, RBAC, salary, documents, dashboard, or audit log features.
- New authentication mechanisms, JWT, roles, authorization policies, or claim changes.
- New public endpoints or request/response DTO changes.
- Moving infrastructure-backed service implementations into `HR.Application`.
- Generic registration scanners or broad reflection-based service registration.
- Changing CORS origins, cookie settings, middleware order, or error codes.

## Project Structure

### Documentation (this feature)

```text
specs/006-di-registration-cleanup/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- di-registration-contracts.md
|-- checklists/
|   |-- requirements.md
|   `-- di-registration.md
`-- tasks.md                 # generated later by /speckit-tasks
```

### Source Code (repository root)

```text
HR.API/
`-- Program.cs              # host-owned pipeline/auth/controller composition

HR.Application/
|-- DependencyInjection.cs   # application-owned registration entry point
`-- HR.Application.csproj

HR.Infrastructure/
|-- DependencyInjection.cs   # EF, Identity store, repositories, services
`-- HR.Infrastructure.csproj

HR.Tests/
|-- DependencyInjection/
|   |-- DependencyRegistrationTests.cs
|   `-- RegistrationBoundaryTests.cs
|-- Auth/
`-- Compatibility/
```

**Structure Decision**: Continue the existing five-project layered solution. Registration extension methods live in the projects that own the dependencies. `HR.API` composes those extensions and keeps only host-level request-pipeline concerns.

## Complexity Tracking

No constitution violations or complexity exceptions are required.
