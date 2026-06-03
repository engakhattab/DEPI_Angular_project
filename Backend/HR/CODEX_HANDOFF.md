# Codex Project Handoff

## 1. Project Overview
This project is an HR Management System backend built with ASP.NET Core 8. The work is organized as a seven-phase architectural refactor. Each phase must preserve backward compatibility unless a later approved specification explicitly permits a behavior change.

## 2. Tech Stack
* **Framework:** ASP.NET Core 8 Web API
* **Language:** C# 12
* **Data Access:** Entity Framework Core 8
* **Database:** SQL Server
* **Authentication:** ASP.NET Core Identity with cookie-based session authentication; no JWT
* **Architecture:** Layered projects: `HR.API`, `HR.Application`, `HR.Infrastructure`, `HR.Domain`, `HR.Shared`
* **Testing:** xUnit with SQLite in-memory storage for focused automated persistence tests

## 3. Project Structure
* `HR.API/`: HTTP endpoints, controllers, middleware, and `Program.cs`.
* `HR.Application/`: Service interfaces and DTOs.
* `HR.Infrastructure/`: EF Core, Identity, repositories, service implementations, entity configurations, and migrations.
* `HR.Domain/`: Entities, enums, and domain exceptions.
* `HR.Shared/`: EF-free utilities including `Result<T>`, `PagedList<T>`, and `ServiceError`.
* `HR.Tests/`: Focused regression coverage for runtime safety, repositories, paging, authentication compatibility, error payloads, and model parity.

## 4. Spec Kit Context
This project uses Spec Kit for phased development.

* The master roadmap is `PLAN.md`.
* The project governance rules are `.specify/memory/constitution.md`.
* Phase-specific artifacts live under `specs/`.
* The completed Phase 4 artifacts live under `specs/004-repository-entity-configurations/`.
* `AGENTS.md` points to the current relevant implementation plan.

## 5. Current Progress
**Phases 0 through 5 out of 7 are implemented. Phase 5 code changes are complete and automated validation is passing.**

Completed phases:
* Phase 0: Foundation & Project Restructure
* Phase 1: Global Exception Handling & Pagination Infrastructure
* Phase 2: Session-Based Authentication & Authorization
* Phase 3: Service Layer Extraction
* Phase 4: Repository Pattern & Entity Configurations

Current phase status:
* Phase 5: HR Business Logic Improvements
* Implementation is in place with full automated build/test coverage passing.
* Manual quickstart validation still requires a live SQL Server environment plus known employee credentials or seeded data.

Compatibility decisions:
* Use secure, HttpOnly cookie sessions; do not introduce JWT.
* Preserve JSON `401` and `403` responses instead of HTML redirects.
* Preserve existing routes, response JSON, cookies, claims, status codes, and error codes unless an approved future phase explicitly changes them.
* Preserve existing database schema and migrations unless an approved future phase explicitly requires a migration.
* Keep Phase 5 domain rules and Phase 6 DI restructuring out of completed Phase 4 work.

## 6. Completed Phases
### Phase 0: Foundation & Project Restructure
* Created the layered solution structure.
* Moved API, application, infrastructure, domain, and shared concerns into their intended projects.

### Phase 1: Global Exception Handling & Pagination Infrastructure
* Added `GlobalExceptionMiddleware`.
* Added structured error responses.
* Added shared pagination support.

### Phase 2: Session-Based Authentication & Authorization
* Configured ASP.NET Core Identity cookie authentication in `Program.cs`.
* Added login, logout, and `GET /api/auth/me`.
* Secured protected controllers with authorization.

### Phase 3: Service Layer Extraction
* Moved department, vacation-request, trip, employee, and authentication orchestration into service interfaces and implementations.
* Kept controllers as thin HTTP adapters.
* Added pagination and cancellation forwarding.
* Preserved employee create/delete transaction safety and generic global `500` handling.

### Phase 4: Repository Pattern & Entity Configurations
* **Final status:** Complete
* **Goal:** Abstract data access behind tailored repositories and split EF Core mappings into independently reviewable entity configurations without changing HTTP behavior or schema.

Major changes:
* Introduced tailored repositories for departments, vacation requests, trips, and employees.
* Introduced read-only `IIdentityUserLookup`.
* Introduced `IUnitOfWork` and `IDataTransaction`.
* Removed service-level direct `ApplicationDbContext` usage.
* Centralized EF paging in `HR.Infrastructure/Data/Pagination/PagedQueryExecutor.cs`, including the provider-aware SQLite test fallback.
* Made `HR.Shared` fully EF-free.
* Removed `PagedList<T>.CreateAsync` after migrating all callers.
* Removed the raw `Employee` entity from the internal authentication result boundary and mapped to `EmployeeResponse` inside `AuthService`.
* Extracted department, employee, vacation-request, and trip mappings into `IEntityTypeConfiguration<T>` classes.
* Reduced `ApplicationDbContext.OnModelCreating` to:

```csharp
base.OnModelCreating(builder);
builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
```

* Added regression tests for paging, repositories, authentication compatibility, error payload parity, model parity, and employee transaction safety.

Final verification:
* `dotnet build .\HR.slnx -c Release`: passed with 0 warnings and 0 errors.
* `dotnet test .\HR.slnx -c Release --no-build`: passed, 26/26 tests.
* EF pending-model-change gate: passed with no model changes.
* Runtime authentication signoff: passed for email login, employee-number login, `/api/auth/me`, logout, invalid login, JSON parity, claims coverage, and runtime `Set-Cookie` inspection.
* Isolated API regression on port `5124`: passed, 43 existing API requests verified across authentication, departments, employees, vacation requests, trips, and representative errors.
* Temporary runtime verification records were removed and the database returned to its original baseline counts.
* No migrations were created or modified.
* No public API behavior changed.
* No Phase 5 business rules or Phase 6 DI restructuring entered Phase 4.

Remaining warnings:
* `git diff --check` reports only LF-to-CRLF conversion warnings.

### Phase 5: HR Business Logic Improvements
* **Status:** Implemented in code; automated validation complete.
* Activated vacation-request business rules: overlap rejection, working-day notice, working-day duration, balance checks, active-employee enforcement, same-status no-op review behavior, reviewer audit fields, and pending-only hard deletion.
* Activated employee lifecycle rules: duplicate active-email rejection, circular-manager rejection, cross-department manager warning, immutable employee number, termination side effects, soft deletion, retained Identity linkage, and immediate auth/session revocation.
* Activated trip rules: requester ownership, active requester enforcement, past-date rejection, Friday/Saturday rejection, and safe handling of historical null requester rows.
* Activated department employee counts excluding soft-deleted employees while retaining terminated visible employees.
* Added a Phase 5 EF migration plus snapshot update and preserved the migration strategy note for nullable historical trip requester rows.

Phase 5 verification:
* `dotnet restore .\HR.slnx`: passed.
* `dotnet build .\HR.slnx -c Release`: passed with 0 warnings and 0 errors.
* `dotnet test .\HR.slnx -c Release --no-build`: passed, 77/77 tests.
* Static checks passed:
  * no `ApplicationDbContext` references remain in `HR.Infrastructure/*Service.cs`
  * no direct `AddScoped<>` registrations were added to `HR.API/Program.cs`
  * `git diff --check` reports only LF-to-CRLF conversion warnings

Manual validation note:
* `dotnet run --project .\HR.API\HR.API.csproj --no-build` entered the normal server run loop during the session, but the full quickstart workflow was not completed because this environment did not provide seeded employee credentials or a dedicated isolated manual-regression dataset for the configured SQL Server instance.

## 7. Remaining Phases
### Phase 6: DI Registration Cleanup
* **Status:** Not started
* **Expected goal:** Complete project-owned dependency-registration cleanup.

### Phase 7: Advanced HR Features
* **Status:** Not started
* **Expected goal:** Add separately approved advanced HR features.

## 8. Architecture Notes
* Controllers are thin HTTP adapters.
* Services own business orchestration.
* Repositories own EF query and mutation access.
* `IUnitOfWork` owns explicit save and transaction coordination.
* `HR.Shared` must remain EF-free.
* `ApplicationDbContext.OnModelCreating` must keep the Identity base call before assembly scanning.
* Existing compatibility error codes remain allowed until a separately approved compatibility phase.

## 9. Development Rules for Codex
* Do not restart or redesign the project.
* Read this handoff, the constitution, the master roadmap, and the relevant Spec Kit artifacts before beginning a new phase.
* Do not skip phases.
* Do not pull Phase 6 cleanup into Phase 5.
* Update task status only after the corresponding work is actually verified.
* Preserve public API behavior unless an approved specification explicitly permits a change.

## 10. Recommended Next Step
1. Perform the manual quickstart checks from `specs/005-hr-business-rules/quickstart.md` against an isolated SQL Server database with known employee credentials.
2. Review the Phase 5 migration before applying it outside isolated environments.
3. Begin Phase 6 only after Phase 5 manual signoff is complete.

## 11. Known Warnings
* `git diff --check` currently reports LF-to-CRLF conversion warnings only.
* Phase 4 intentionally preserved legacy-compatible error codes. Any error-code normalization requires a separate approved compatibility decision.
* Phase 5 manual quickstart coverage still needs a live operator pass against the configured SQL Server environment.
