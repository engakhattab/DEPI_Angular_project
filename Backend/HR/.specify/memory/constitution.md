<!--
  Sync Impact Report
  ==================
  Version change: 1.0.0 → 1.1.0
  Modified principles:
    - I. Layered Architecture (clarified EF-free HR.Shared boundary)
    - III. Service Layer Separation (clarified contract and implementation placement)
    - IV. Domain Integrity & Business Rules (added staged activation)
    - V. Global Error Handling & API Consistency (allowed compatibility codes)
    - VI. Data Access Abstraction (clarified Identity model call order and Phase 6 DI activation)
  Added sections:
    - Core Principles (7 principles)
    - Technology Stack & Constraints
    - Development Workflow
    - Governance
  Removed sections: None
  Templates requiring updates:
    - .specify/templates/plan-template.md        ✅ reviewed (no update needed)
    - .specify/templates/spec-template.md         ✅ reviewed (no update needed)
    - .specify/templates/tasks-template.md        ✅ reviewed (no update needed)
  Runtime guidance requiring updates:
    - PLAN.md                                    ✅ updated
    - specs/004-repository-entity-configurations ✅ updated
  Follow-up TODOs: None
-->

# HR Management System Constitution

## Core Principles

### I. Layered Architecture

All production code MUST be organized into exactly five projects
following a strict dependency direction:

- `HR.API` composes `HR.Application` and `HR.Infrastructure`.
- `HR.Infrastructure` implements contracts from `HR.Application`.
- `HR.Application` and `HR.Infrastructure` reference `HR.Domain`.
- `HR.Shared` is referenced by all layers.

**Rules:**

- `HR.Domain` MUST have zero external NuGet dependencies
  (entities, enums, domain exceptions only).
- `HR.Application` MUST NOT reference `HR.Infrastructure` directly;
  depend on interfaces that Infrastructure implements.
- `HR.API` MUST NOT contain business logic — controllers are thin
  HTTP adapters that parse requests, call services, and map results.
- `HR.Infrastructure` owns EF Core, Identity, repositories, and
  data-access concerns exclusively.
- Cross-cutting utilities (`Result<T>`, `PagedList<T>`,
  `ServiceError`, JSON converters) live in `HR.Shared`.
- `HR.Shared` MUST remain free of EF Core dependencies. Shared
  pagination utilities MAY expose result models, constants, and
  normalization helpers only. EF Core query execution belongs in
  `HR.Infrastructure`.

**Rationale:** Enforcing dependency direction keeps the domain model
portable, the business logic testable in isolation, and the data
access swappable.

### II. Cookie-Based Session Authentication

All API endpoints MUST be secured with ASP.NET Core cookie-based
session authentication. JWT MUST NOT be used.

**Rules:**

- Authentication uses `AddAuthentication().AddCookie()` with
  `HttpOnly`, `SameSite=Strict`, `SecurePolicy=Always`.
- `UserManager<ApplicationUser>` and ASP.NET Core Identity serve
  as the credential store — no custom password hashing.
- Login establishes a `ClaimsPrincipal` via `HttpContext.SignInAsync`;
  logout calls `HttpContext.SignOutAsync`.
- Unauthenticated requests MUST receive a JSON `401` response —
  never an HTML redirect.
- `[Authorize]` MUST be applied globally; only login is
  `[AllowAnonymous]`.

**Rationale:** Cookie auth is simpler to reason about for this
system, avoids token storage/refresh complexity, and integrates
natively with ASP.NET Core's authentication pipeline.

### III. Service Layer Separation

Business logic MUST live exclusively in service classes exposed
through interfaces inside `HR.Application`. Implementations MAY live
in `HR.Application` or `HR.Infrastructure` when they require
data-access internals. Controllers MUST NOT contain conditional
business logic.

**Rules:**

- Every service method that performs writes MUST return `Result<T>`,
  or `Result` when the successful operation has no response payload.
- Read methods return DTOs or `PagedList<T>` — never raw entities.
- Every list endpoint MUST support pagination with `page` and
  `pageSize` parameters (default 25, max 100).
- Every async method MUST accept and forward `CancellationToken`.
- Service interfaces are defined in `HR.Application`; implementations
  may live in `HR.Application` or `HR.Infrastructure` depending on
  whether they need data-access internals.

**Rationale:** Thin controllers with no business logic enable
unit testing of all rules without HTTP infrastructure. The
`Result<T>` pattern eliminates exception-driven control flow for
expected failure cases.

### IV. Domain Integrity & Business Rules

All domain invariants MUST be enforced in the service layer.
No invalid state transitions, duplicate records, or constraint
violations may reach the database.

**Rules:**

The rules in this principle are staged project requirements. They
become mandatory when their planned implementation phase is active or
completed. Phase 4 MUST preserve current runtime behavior and MUST NOT
implement the Phase 5 rules early.

- State transitions (employee status, vacation request status)
  MUST follow explicitly defined state machines — no ad-hoc
  status changes.
- Vacation requests MUST be validated for: date overlap,
  sufficient balance, active employee status, future dates,
  minimum notice period, and self-approval prevention.
- Employee numbers are immutable after creation.
- Employees MUST be soft-deleted (mark `IsDeleted = true`,
  set `TerminatedAt`) — never hard-deleted.
- Circular manager chains MUST be detected and rejected.
- Duplicate email addresses MUST be rejected for active employees.

**Rationale:** Explicit business rules prevent data corruption
and ensure the HR system behaves predictably under all edge cases.

### V. Global Error Handling & API Consistency

All API responses MUST follow a consistent error format. Raw
exceptions MUST NEVER leak to API clients.

**Rules:**

- A `GlobalExceptionMiddleware` MUST be the first middleware in
  the pipeline, catching all unhandled exceptions.
- Error responses MUST use the format `{ "code": "...", "message": "..." }`.
- Domain exceptions (`NotFoundException`, `ConflictException`,
  `BusinessRuleException`) map to `404`, `409`, `422` respectively.
- Unhandled exceptions map to `500` with a generic message;
  details are logged server-side only.
- New `ServiceError` usage SHOULD prefer: `NOT_FOUND`, `CONFLICT`,
  `VALIDATION`, `BUSINESS_RULE`, `SERVER_ERROR`.
- Existing compatibility codes `UNAUTHORIZED`, `FORBIDDEN`,
  `VALIDATION_ERROR`, `INTERNAL_ERROR`, and `BUSINESS_RULE_VIOLATION`
  remain allowed until a separately approved compatibility phase.
  Existing HTTP statuses and error codes MUST NOT be renamed as part
  of an unrelated refactor.

**Rationale:** Consistent error shapes simplify frontend error
handling and prevent information leakage.

### VI. Data Access Abstraction

Data access MUST be abstracted behind repository interfaces.
Services MUST NOT reference `ApplicationDbContext` directly
after the repository layer is introduced.

**Rules:**

- Each aggregate root has its own `IXxxRepository` interface
  in `HR.Infrastructure/Repositories/`.
- Repositories are registered as `Scoped` in DI.
- EF Core entity configurations MUST use
  `IEntityTypeConfiguration<T>` per entity. Because the project uses
  ASP.NET Core Identity, `OnModelCreating` MUST contain only the
  required `base.OnModelCreating(builder)` call followed by
  `ApplyConfigurationsFromAssembly`. The order MUST NOT be reversed.
- Full project-owned DI registration cleanup activates in Phase 6.
  When Phase 6 is active or completed, each project (`HR.Application`,
  `HR.Infrastructure`) MUST own its own `DependencyInjection.cs`
  extension method for service registration. Before then, a phase MAY
  add only the narrow registrations required by its own boundaries.
- `Program.cs` MUST NOT contain direct `AddScoped<>` calls for
  application or infrastructure services.

**Rationale:** Repository abstraction enables service-level unit
testing with mocked data access and keeps EF Core concerns
encapsulated.

### VII. Simplicity & YAGNI

Do not introduce patterns, abstractions, or features that are
not required by the current plan.

**Rules:**

- No MediatR, CQRS, event sourcing, or microservice boundaries
  unless explicitly approved in a future plan amendment.
- No generic repository base class — each repository is concrete
  and tailored to its aggregate.
- Each phase in the plan is an independent, reviewable unit of
  work. Do not combine phases.
- If a decision is ambiguous, choose the simpler option and
  document the trade-off.

**Rationale:** Premature abstraction increases complexity without
delivering value. The layered architecture already provides
sufficient separation for this system's scale.

## Technology Stack & Constraints

- **Runtime:** .NET 8 (ASP.NET Core 8 WebAPI)
- **Frontend:** Angular 17 (separate repository / project)
- **Database:** SQL Server with Entity Framework Core 8
- **Identity:** ASP.NET Core Identity (`UserManager`,
  `ApplicationUser`) — credential store only
- **Authentication:** Cookie-based session auth (NO JWT)
- **Architecture:** Layered — HR.API, HR.Application,
  HR.Infrastructure, HR.Domain, HR.Shared
- **Serialization:** `System.Text.Json` with custom
  `DateOnlyJsonConverter` in `HR.Shared`
- **Testing:** xUnit (when tests are required)
- **Target:** Windows / Linux server, IIS or Kestrel
- **CORS:** Credentials allowed (`AllowCredentials()`) for
  cross-origin cookie auth with Angular frontend

## Development Workflow

- **Phase execution:** Phases MUST be implemented in the order
  defined in `PLAN.md`. Each phase is a standalone pull request.
- **Backward compatibility:** Existing API routes and
  request/response shapes MUST remain backward-compatible unless
  the plan explicitly permits a breaking change.
- **Migration discipline:** Database schema changes require an
  EF Core migration. Existing migrations MUST NOT be modified.
- **Code review:** Every phase MUST be reviewed and merged before
  the next phase begins.
- **Commit granularity:** Commit after each logical task or
  sub-task within a phase.
- **Error-first validation:** Service methods MUST validate all
  preconditions and return `Result.Failure` before performing
  any state mutation.

## Governance

This constitution is the authoritative source of architectural
and process rules for the HR Management System backend. All
implementation work, code reviews, and design decisions MUST
comply with the principles defined above.

**Amendment procedure:**

1. Propose the change with rationale in a constitution amendment
   PR.
2. Document the principle being added, modified, or removed.
3. Update the version according to semantic versioning:
   - MAJOR: Principle removal or backward-incompatible redefinition.
   - MINOR: New principle or materially expanded guidance.
   - PATCH: Clarifications, typo fixes, non-semantic refinements.
4. Update `LAST_AMENDED_DATE` to the date of the amendment.
5. Propagate changes to dependent templates and documentation.

**Compliance review:**

- Every PR MUST be checked against the constitution principles.
- Violations MUST be justified in the PR description or resolved
  before merge.
- The constitution supersedes informal conventions or ad-hoc
  decisions.

**Version**: 1.1.0 | **Ratified**: 2026-05-18 | **Last Amended**: 2026-06-01
