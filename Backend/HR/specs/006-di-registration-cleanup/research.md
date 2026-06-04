# Research: Phase 6 - DI Registration Cleanup

## Decision 1: Keep Infrastructure-Backed Service Implementations Registered by Infrastructure

**Decision**: `HR.Application` exposes `AddApplication()` as the application-owned registration entry point, but `HR.Infrastructure` registers current service implementations because they depend on repositories, Identity, EF Core, unit-of-work boundaries, time providers, and infrastructure helpers.

**Rationale**: The constitution allows implementations to live in infrastructure when they need data-access internals. Moving those implementations into application would either break dependency direction or require broad abstractions not needed for Phase 6. An intentionally narrow `AddApplication()` still satisfies project-owned registration ownership and creates a stable future extension point.

**Alternatives considered**:

- Move service implementations into `HR.Application`: rejected because current implementations depend on infrastructure-only persistence and Identity concerns.
- Skip `AddApplication()`: rejected because Phase 6 requires each project to own its registration entry point.
- Use reflection scanning: rejected as unnecessary for a small, explicit registration set.

## Decision 2: Move `ApplicationDbContext` and Identity Store Registration Into Infrastructure

**Decision**: Update `AddInfrastructure()` to accept configuration and own the SQL Server `ApplicationDbContext` registration plus Identity role/store integration, preserving the existing password options.

**Rationale**: EF Core and Identity stores are infrastructure concerns. Keeping them in `Program.cs` leaves host startup responsible for infrastructure composition and fails the Phase 6 ownership goal.

**Alternatives considered**:

- Keep `AddDbContext` and `AddIdentityCore` in `Program.cs`: rejected because it does not complete DI ownership cleanup.
- Create a separate API extension for persistence: rejected because persistence belongs to infrastructure and would split ownership.

## Decision 3: Keep Cookie Authentication Events in Host Startup

**Decision**: Leave cookie authentication configuration and event handlers in `Program.cs`, including `OnValidatePrincipal`.

**Rationale**: Cookie events are request-pipeline behavior. The event can resolve `IEmployeeSessionValidator` from request services after infrastructure registration without moving HTTP concerns into infrastructure.

**Alternatives considered**:

- Move cookie configuration into infrastructure: rejected because it would make infrastructure own host-level HTTP authentication behavior.
- Create a new auth options object: rejected as unnecessary scope expansion.

## Decision 4: Add Minimal DI Abstraction Dependency to Application if Needed

**Decision**: Add only the minimal dependency required for `HR.Application` to expose an `IServiceCollection` extension method.

**Rationale**: `HR.Application` currently does not directly reference DI abstractions. A small abstraction dependency keeps the layer independent from infrastructure while enabling the required registration entry point.

**Alternatives considered**:

- Avoid `AddApplication()`: rejected by spec and constitution activation.
- Add broader ASP.NET Core framework dependencies to `HR.Application`: rejected because the application layer should not gain host/runtime concerns.

## Decision 5: Validate With Automated Tests, Static Scans, and Local Database Smoke

**Decision**: Use full automated tests, focused registration-resolution tests, static scans, and a local SQL Server smoke run after applying the existing Phase 5 migration.

**Rationale**: Phase 6 is wiring-only; the risk is missing registrations or changed startup behavior. Resolution tests and static scans catch wiring mistakes, while local smoke checks prove the configured runtime still starts and reaches representative workflows.

**Alternatives considered**:

- Manual validation only: rejected because registration regressions are cheap to automate.
- New schema validation: rejected because Phase 6 is migration-free.
