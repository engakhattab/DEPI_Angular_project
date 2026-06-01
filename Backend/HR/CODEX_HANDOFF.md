# Codex Project Handoff

## 1. Project Overview
This project is an **HR Management System** built with ASP.NET Core 8. The original system was functional but architecturally immature, with all business logic tightly coupled inside controllers. The main goal of the current work is to execute a phased architectural refactor to transform the project into a robust, layered architecture. This ensures the system is maintainable, testable, and extensible without breaking existing backward compatibility with the frontend API contracts.

## 2. Tech Stack
* **Framework:** ASP.NET Core 8 (Web API)
* **Language:** C# 12
* **Data Access:** Entity Framework Core 8
* **Database:** SQL Server
* **Authentication:** ASP.NET Core Identity (Cookie-based session auth, no JWT)
* **Architecture:** Layered (Domain, Application, Infrastructure, API, Shared)

## 3. Project Structure
The repository has been restructured into a 5-project layered architecture:
* `HR.API/`: HTTP endpoints, controllers, middleware, and `Program.cs`. References Application and Shared.
* `HR.Application/`: Business logic, services, interfaces, DTOs. References Domain and Shared.
* `HR.Infrastructure/`: Data access, EF Core, ApplicationDbContext, Identity, migrations. References Domain and Shared (and Application for service implementations).
* `HR.Domain/`: Core entities, enums, domain exceptions. Has **zero** external dependencies.
* `HR.Shared/`: Pure utilities (`Result<T>`, `PagedList<T>`, `ServiceError`).

## 4. Spec Kit Context
This project uses **Spec Kit** for AI-driven development.
* All feature plans, specs, and tasks are located in the `specs/` directory (e.g., `specs/003-session-auth/`).
* Important files in a spec directory:
  * `spec.md`: Functional requirements and user stories.
  * `plan.md`: Technical implementation plan and architectural decisions.
  * `tasks.md`: Actionable checklist of implementation steps.
  * `research.md` / `data-model.md`: Additional context and decisions.
* `AGENTS.md` in the project root points to the current active plan.

## 5. Current Progress
This project was previously worked on using Antigravity and OpenCode with Spec Kit.
**We are currently at Phase 3 out of 7.**

* **Completed:** Phases 0, 1, and 2. The project has been restructured, global exception handling and pagination utilities are in place, and session-based authentication has been implemented.
* **Partially Completed / Not Started:** Phase 3 (Service Layer Extraction) is about to begin.
* **Important Decisions Made:**
  * No JWT tokens; strict use of secure, HttpOnly session cookies.
  * Unauthenticated requests return JSON 401/403 (no HTML redirects).
  * Strict layered architecture where Controllers are only thin HTTP adapters.

## 6. Completed Phases

### Phase 0: Foundation & Project Restructure
* **Goal:** Create the new layered solution structure without breaking existing functionality.
* **Completed work:** Created the 5 projects (`API`, `Application`, `Infrastructure`, `Domain`, `Shared`). Moved entities, enums, DTOs, and DbContext to their correct layers.
* **Important files:** `NotFoundException`, `ConflictException`, `Result<T>`, `ServiceError`.

### Phase 1: Global Exception Handling & Pagination Infrastructure
* **Goal:** Centralize error handling and add a pagination helper for future service layers.
* **Completed work:** Implemented `GlobalExceptionMiddleware` to catch domain exceptions and return standardized JSON error responses. Implemented `PagedList<T>` in `HR.Shared`.
* **Important files:** `GlobalExceptionMiddleware.cs`, `PagedList.cs`.

### Phase 2: Session-Based Authentication & Authorization
* **Goal:** Secure endpoints with cookie-based session auth using `AspNetIdentity`.
* **Completed work:** Configured cookie auth in `Program.cs`. Created `IAuthService` and `AuthService`. Refactored `AuthController` to use `HttpContext.SignInAsync`. Added `[Authorize]` to all other controllers. Added a `GET /api/auth/me` endpoint.
* **Important files:** `AuthController.cs`, `AuthService.cs`, `IAuthService.cs`, `Program.cs`.

## 7. Current Phase: Phase 3
**Phase 3: Service Layer Extraction**

* **Goal:** Move all business logic out of controllers into dedicated service classes inside `HR.Application`. Controllers must become thin HTTP adapters that parse requests, call services, and return results.
* **What has been done:** Nothing yet. The feature branch and specs need to be created/reviewed for Phase 3.
* **What needs to be done:**
  * Extract features one by one: Departments → VacationRequests → Trips → Employees.
  * Create `IXxxService` interfaces in `HR.Application`.
  * Implement `XxxService` classes using `ApplicationDbContext` directly.
  * Rewrite controllers to inject services and use the `Result<T>` pattern.
  * Add pagination (`page`, `pageSize`) to all list endpoints.
  * Implement `CancellationToken` in all async paths.
* **Known blockers/warnings:** This is the largest behavioral refactor. It must be done feature by feature and tested thoroughly. Controllers currently inject `ApplicationDbContext` directly, which must be completely eliminated by the end of this phase.

## 8. Remaining Phases

### Phase 4: Repository Pattern & Entity Configurations
* **Expected goal:** Abstract data access behind repository interfaces. Move `OnModelCreating` configurations into separate `IEntityTypeConfiguration<T>` classes.
* **Expected work:** Create repositories (`IEmployeeRepository`, etc.) and refactor services from Phase 3 to use repositories instead of `ApplicationDbContext`.
* **Dependencies:** Requires Phase 3 to be fully complete.

### Phase 5: HR Business Logic Improvements
* **Expected goal:** Implement strict HR business rules (e.g., vacation overlap checks, status transition state machines, soft-deletes).
* **Expected work:** Add new validations in the service layer and update entities (e.g., tracking `VacationBalanceDays`, soft delete flags).
* **Dependencies:** Requires Phase 3 service layer to be in place.

### Phase 6: DI Registration Cleanup
* **Expected goal:** Centralize dependency injection so `Program.cs` is clean.
* **Expected work:** Create `AddApplication()` and `AddInfrastructure()` extension methods in their respective projects.
* **Dependencies:** Requires Phase 4 repositories.

### Phase 7: Advanced HR Features
* **Expected goal:** Add features like Attendance Tracking, Role-Based Access Control (RBAC), Salary tracking, Document management, and Audit logs.
* **Expected work:** New entities, migrations, and endpoints for advanced HR operations.
* **Dependencies:** Requires Phases 5 and 6.

## 9. Architecture Notes
* **Strict Layering:** `API` -> `Application` -> `Infrastructure` -> `Domain`.
* **Domain Purity:** The Domain layer must never reference external packages or other layers.
* **Result Pattern:** Services do not throw exceptions for business logic failures (like validation or not found). They return `Result<T>` or `Result`. Controllers map these results to HTTP status codes (e.g., `404`, `409`, `422`).
* **Thin Controllers:** Controllers should only have ~4 lines of code per action (validate ModelState, call service, map error if failed, return success).

## 10. Development Rules for Codex
* Codex must **not** restart the project from scratch.
* Codex must continue from the existing specs, plan, and tasks.
* Codex must read this file before making changes.
* Codex must read the relevant Spec Kit files (`spec.md`, `plan.md`, `tasks.md`) in the active `specs/` directory before implementing.
* Codex must not skip phases.
* Codex must not make major architecture changes without explicit approval.
* Codex should update task status in `tasks.md` only when a task is actually completed and verified.
* Codex should run relevant tests/build checks (`dotnet build`) after implementation.
* Codex should explain changes after each task.

## 11. Recommended First Step for Codex
1. Read this `CODEX_HANDOFF.md` file completely.
2. Inspect the current Spec Kit files (especially the active `tasks.md` and `plan.md` for Phase 3).
3. Summarize your understanding of the project state and the immediate next steps.
4. **Wait for the user's confirmation before writing any code or modifying any files.**

## 12. Unknowns / Things to Verify
* **Phase 2 Completion Status:** While Phase 2 tasks were executed, Codex should verify that `dotnet build` succeeds and the `[Authorize]` attributes were applied correctly without compilation errors.
* **Spec Kit generation for Phase 3:** It is unclear if `specs/004-service-layer/` (or similar) has been generated yet. If not, the Spec Kit workflow (`/speckit-specify`, `/speckit-plan`, `/speckit-tasks`) for Phase 3 needs to be run before coding begins.
