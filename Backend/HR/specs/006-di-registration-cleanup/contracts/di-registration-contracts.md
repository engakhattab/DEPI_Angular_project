# Internal Contracts: Phase 6 - DI Registration Cleanup

Phase 6 changes internal composition contracts only. No public HTTP contracts change.

## Application Registration Contract

### `HR.Application.DependencyInjection.AddApplication`

Purpose: provide the application-owned registration entry point.

Expected behavior:

- Accept an existing service collection.
- Return the same service collection for chaining.
- Register application-only dependencies when they exist.
- Not reference `HR.Infrastructure`.
- Not register infrastructure-backed service implementations.
- Not register EF Core, Identity stores, repositories, HTTP pipeline services, or host-specific options.

Current Phase 6 behavior:

- May be intentionally empty because current service implementations are infrastructure-backed.

## Infrastructure Registration Contract

### `HR.Infrastructure.DependencyInjection.AddInfrastructure`

Purpose: provide the infrastructure-owned registration entry point.

Expected behavior:

- Accept an existing service collection and application configuration.
- Resolve `DefaultConnection` with the same missing-connection failure behavior as the current host startup.
- Register `ApplicationDbContext` against the configured SQL Server connection.
- Register Identity role/store integration and preserve existing password options.
- Register repositories and unit-of-work boundaries.
- Register infrastructure-backed implementations of application service contracts.
- Register Phase 5 support services, including time provider, working-day helper, and employee session validator.
- Return the same service collection for chaining.

Must not:

- Configure cookie authentication challenge/forbid/session events.
- Configure controllers, serialization, CORS, Swagger, middleware order, or endpoint mapping.
- Create or apply migrations.
- Change public API contracts.

## Host Composition Contract

### `HR.API.Program`

Purpose: compose host-level request handling and call project-owned registration entry points.

Expected behavior:

- Call `AddApplication()`.
- Call `AddInfrastructure(builder.Configuration)`.
- Keep authentication cookie settings and events host-owned.
- Keep authorization, controllers, JSON converters, CORS, Swagger, middleware ordering, and endpoint mapping host-owned.
- Contain no direct `AddScoped<>` registrations for application or infrastructure services.
- Contain no direct `AddDbContext`, `AddIdentityCore`, or `AddEntityFrameworkStores` registration calls.

## Compatibility Contract

The following must remain stable:

- Routes and HTTP verbs.
- Request and response DTO shapes.
- Pagination envelopes.
- Cookie/session settings and claim names.
- JSON `401` and `403` behavior.
- Structured error payload shape: `code` and `message`.
- Phase 5 business-rule behavior.
- Cancellation forwarding and scoped lifetimes.
