# HR System — Architecture Refactor Spec

**Project:** HR Management System  
**Team:** pentaRae  
**Prepared by:** Senior Architecture Review  
**Date:** 2026-05-18  
**Status:** Approved for implementation

---

## Executive Summary

The current HR system is a functional but architecturally immature ASP.NET Core 8 application. All business logic lives inside controllers, `ApplicationDbContext` is injected directly into HTTP handlers, no authentication protects any endpoint, and list endpoints have no pagination. The system works at small scale but will become increasingly difficult to test, maintain, and extend as features grow.

This document defines a phased refactor plan that transforms the project into a properly layered architecture without breaking existing functionality. Each phase is an independent, reviewable unit of work. Phases are ordered so that each one lowers the risk of the next.

### What is NOT changing

- The database schema and existing migrations remain untouched.
- The frontend API contract (routes, request/response shapes) remains backward-compatible.
- Authentication will use simple username/password session-based auth — not JWT. The existing `AspNetIdentity` setup (`UserManager`, `ApplicationUser`) is retained as the credential store.
- No new business domains are introduced. All improvements are grounded in what already exists.

### Current Critical Issues

| # | Issue | Severity |
|---|-------|----------|
| 1 | All business logic is inside controllers | Critical |
| 2 | `ApplicationDbContext` injected directly into controllers | Critical |
| 3 | No authentication on any endpoint — all routes are open | Critical |
| 4 | No global error handling — raw exceptions leak to clients | High |
| 5 | List endpoints load entire tables into memory — no pagination | High |
| 6 | No service layer — logic is untestable in isolation | High |
| 7 | `OnModelCreating` is a 120-line monolith | Medium |
| 8 | Business rules scattered across HTTP handlers | Medium |
| 9 | No vacation overlap validation | Medium |
| 10 | No circular manager chain guard | Low |

---

## Proposed Architecture

**Pattern:** Layered Architecture (4 projects + 1 shared kernel)

```
Dependency direction: HR.API → HR.Application → HR.Infrastructure → HR.Domain
                                                                  ↑
                                                             HR.Shared (all layers)
```

### Projects

| Project | Responsibility | May reference |
|---------|---------------|---------------|
| `HR.API` | HTTP only — controllers, middleware, DI wiring, `Program.cs` | Application, Shared |
| `HR.Application` | Business logic — services, interfaces, DTOs, validators | Domain, Shared |
| `HR.Infrastructure` | Data access — EF Core, repositories, Identity, migrations | Domain, Shared |
| `HR.Domain` | Core entities, enums, domain exceptions — **zero external dependencies** | Shared |
| `HR.Shared` | Pure utilities — `Result<T>`, `PagedList<T>`, `ServiceError`, converters | *(none)* |

### Final Folder Structure

```
HR/
├── HR.API/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── EmployeesController.cs
│   │   ├── DepartmentsController.cs
│   │   ├── VacationRequestsController.cs
│   │   └── TripsController.cs
│   ├── Middleware/
│   │   └── GlobalExceptionMiddleware.cs
│   ├── Extensions/
│   │   ├── ServiceCollectionExtensions.cs
│   │   └── ApplicationBuilderExtensions.cs
│   └── Program.cs
│
├── HR.Application/
│   ├── Common/
│   │   ├── Interfaces/
│   │   │   ├── IEmployeeService.cs
│   │   │   ├── IDepartmentService.cs
│   │   │   ├── IVacationService.cs
│   │   │   ├── ITripService.cs
│   │   │   └── IAuthService.cs
│   │   └── Models/
│   │       └── PaginationParams.cs
│   ├── Employees/
│   │   ├── EmployeeService.cs
│   │   └── DTOs/
│   │       ├── EmployeeCreateRequest.cs
│   │       ├── EmployeeUpdateRequest.cs
│   │       └── EmployeeResponse.cs
│   ├── Departments/
│   │   ├── DepartmentService.cs
│   │   └── DTOs/
│   ├── VacationRequests/
│   │   ├── VacationService.cs
│   │   └── DTOs/
│   ├── Transportation/
│   │   ├── TripService.cs
│   │   └── DTOs/
│   └── Auth/
│       ├── AuthService.cs
│       └── DTOs/
│           ├── LoginRequest.cs
│           └── LoginResponse.cs
│
├── HR.Infrastructure/
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   └── Configurations/
│   │       ├── EmployeeConfiguration.cs
│   │       ├── DepartmentConfiguration.cs
│   │       ├── VacationRequestConfiguration.cs
│   │       └── TripConfiguration.cs
│   ├── Repositories/
│   │   ├── IEmployeeRepository.cs
│   │   ├── EmployeeRepository.cs
│   │   ├── IDepartmentRepository.cs
│   │   ├── DepartmentRepository.cs
│   │   ├── IVacationRepository.cs
│   │   ├── VacationRepository.cs
│   │   ├── ITripRepository.cs
│   │   └── TripRepository.cs
│   ├── Auth/
│   │   └── SessionAuthService.cs
│   ├── Migrations/
│   └── DependencyInjection.cs
│
├── HR.Domain/
│   ├── Entities/
│   │   ├── Employee.cs
│   │   ├── Department.cs
│   │   ├── VacationRequest.cs
│   │   └── Trip.cs
│   ├── Enums/
│   │   ├── EmployeeStatus.cs
│   │   └── VacationRequestStatus.cs
│   └── Exceptions/
│       ├── NotFoundException.cs
│       ├── ConflictException.cs
│       └── BusinessRuleException.cs
│
└── HR.Shared/
    ├── Results/
    │   ├── Result.cs
    │   └── ServiceError.cs
    └── Pagination/
        └── PagedList.cs
```

---

## Phase 0 — Foundation & Project Restructure

**Goal:** Create the new solution structure without breaking any existing functionality.  
**Risk:** Low — compile-only changes, no behavioral changes.  
**Effort:** Medium  
**Prerequisite:** None

### Tasks

1. Create four new class library projects in the solution: `HR.Domain`, `HR.Application`, `HR.Infrastructure`, `HR.Shared`.
2. Add project references following the dependency rule above.
3. Move entity models (`Employee`, `Department`, `Trip`, `VacationRequest`, `ApplicationUser`) to `HR.Domain/Entities/`.
4. Move enums (`EmployeeStatus`, `VacationRequestStatus`) to `HR.Domain/Enums/`.
5. Move `ApplicationDbContext` and the entire `Migrations/` folder to `HR.Infrastructure/Data/`.
6. Move all DTOs to `HR.Application/` under the matching feature folder.
7. Move `DateOnlyJsonConverter` and `NullableDateOnlyJsonConverter` to `HR.Shared/`.
8. Add `HR.Shared`, `HR.Application`, and `HR.Infrastructure` references to `HR.API`.
9. Keep all controllers as-is for now — they still inject `ApplicationDbContext` directly. That is intentional and gets fixed in Phase 3.
10. Add domain exceptions to `HR.Domain/Exceptions/`:

```csharp
// NotFoundException.cs
public class NotFoundException(string message) : Exception(message);

// ConflictException.cs
public class ConflictException(string message) : Exception(message);

// BusinessRuleException.cs
public class BusinessRuleException(string message) : Exception(message);
```

11. Add `Result<T>` and `ServiceError` to `HR.Shared/Results/`:

```csharp
// Result.cs
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public ServiceError? Error { get; }

    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(ServiceError error) { IsSuccess = false; Error = error; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(ServiceError error) => new(error);
}

// ServiceError.cs
public record ServiceError(string Code, string Message)
{
    public static ServiceError NotFound(string message)     => new("NOT_FOUND", message);
    public static ServiceError Conflict(string message)     => new("CONFLICT", message);
    public static ServiceError Validation(string message)   => new("VALIDATION", message);
    public static ServiceError BusinessRule(string message) => new("BUSINESS_RULE", message);
}
```

### Done Criteria

- Solution compiles with zero errors.
- All existing API endpoints work identically to before.
- No behavioral changes whatsoever.

---

## Phase 1 — Global Exception Handling & Pagination Infrastructure

**Goal:** Centralize error handling so all phases after this benefit from consistent error responses. Add the pagination helper class for use in Phase 3.  
**Risk:** Low — additive changes only.  
**Effort:** Low  
**Prerequisite:** Phase 0

### Tasks

1. Create `GlobalExceptionMiddleware` in `HR.API/Middleware/`:

```csharp
public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteError(context, 404, "NOT_FOUND", ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteError(context, 409, "CONFLICT", ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            await WriteError(context, 422, "BUSINESS_RULE", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteError(context, 500, "SERVER_ERROR", "An unexpected error occurred.");
        }
    }

    private static async Task WriteError(
        HttpContext ctx, int status, string code, string message)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new { code, message });
    }
}
```

2. Register the middleware as the **first** entry in the pipeline in `Program.cs`, before `UseHttpsRedirection`.

3. Create `PagedList<T>` in `HR.Shared/Pagination/`:

```csharp
public class PagedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;

    public PagedList(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public static async Task<PagedList<T>> CreateAsync(
        IQueryable<T> source, int page, int pageSize, CancellationToken ct = default)
    {
        var count = await source.CountAsync(ct);
        var items = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return new PagedList<T>(items, count, page, pageSize);
    }
}
```

### Done Criteria

- Any unhandled exception returns `{ "code": "...", "message": "..." }` JSON — no raw exception text leaks to the client.
- `PagedList<T>` is available for use in Phase 3.
- All existing endpoints still work.

---

## Phase 2 — Session-Based Authentication & Authorization

**Goal:** Secure all endpoints with ASP.NET Core cookie-based session auth using the existing `AspNetIdentity` user store. Login validates credentials against `UserManager<ApplicationUser>` and establishes a server-side session. All other controllers require an authenticated session.  
**Risk:** Medium — functional change; frontend must send credentials and hold a session cookie.  
**Effort:** Medium  
**Prerequisite:** Phase 1

### Design Decisions

- Auth mechanism: `AddAuthentication().AddCookie()` with ASP.NET Core's built-in cookie middleware. No JWT, no tokens.
- The login endpoint calls `UserManager.CheckPasswordAsync`, then `HttpContext.SignInAsync` with a `ClaimsPrincipal` built from the employee's data.
- The session cookie is `HttpOnly`, `SameSite=Strict`, and scoped to the API domain.
- `[Authorize]` is added globally via a convention; `[AllowAnonymous]` is placed only on the login action.
- The `IAuthService` interface lives in `HR.Application/Auth/` to keep the business logic of "who can log in" separate from the HTTP mechanics.

### Tasks

1. Register cookie auth in `Program.cs`:

```csharp
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = null;          // API — return 401, don't redirect
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
```

2. Add `app.UseAuthentication()` and `app.UseAuthorization()` to the pipeline (in that order, after exception middleware).

3. Create `IAuthService` in `HR.Application/Auth/`:

```csharp
public interface IAuthService
{
    Task<Result<AuthenticatedEmployee>> ValidateCredentialsAsync(
        string identifier, string password, CancellationToken ct);
}

public record AuthenticatedEmployee(
    Employee Employee,
    ApplicationUser User);
```

4. Implement `AuthService` in `HR.Infrastructure/Auth/`:

```csharp
public class AuthService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager) : IAuthService
{
    public async Task<Result<AuthenticatedEmployee>> ValidateCredentialsAsync(
        string identifier, string password, CancellationToken ct)
    {
        ApplicationUser? user = null;
        Employee? employee = null;

        if (identifier.Contains('@'))
        {
            user = await userManager.FindByEmailAsync(identifier);
            if (user is not null)
                employee = await context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Manager)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id, ct);
        }

        if (user is null)
        {
            employee = await context.Employees
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .Include(e => e.IdentityUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeNumber == identifier, ct);
            user = employee?.IdentityUser;
        }

        if (user is null || employee is null)
            return Result<AuthenticatedEmployee>.Failure(
                ServiceError.Validation("Invalid credentials."));

        var valid = await userManager.CheckPasswordAsync(user, password);
        if (!valid)
            return Result<AuthenticatedEmployee>.Failure(
                ServiceError.Validation("Invalid credentials."));

        return Result<AuthenticatedEmployee>.Success(
            new AuthenticatedEmployee(employee, user));
    }
}
```

5. Rewrite `AuthController` to use `IAuthService` and `HttpContext.SignInAsync`:

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var result = await authService.ValidateCredentialsAsync(
            request.Identifier.Trim(), request.Password, ct);

        if (!result.IsSuccess)
            return Unauthorized(new { message = result.Error!.Message });

        var employee = result.Value!.Employee;
        var user     = result.Value!.User;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? ""),
            new("employee_id", employee.Id.ToString()),
            new("employee_number", employee.EmployeeNumber),
            new("full_name", employee.FullName),
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = false });

        return Ok(new LoginResponse
        {
            Employee = EmployeeResponse.FromEntity(employee)
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    public ActionResult<CurrentUserResponse> Me()
    {
        var employeeId = User.FindFirstValue("employee_id");
        var fullName   = User.FindFirstValue("full_name");
        var email      = User.FindFirstValue(ClaimTypes.Email);

        return Ok(new CurrentUserResponse
        {
            EmployeeId     = Guid.Parse(employeeId!),
            FullName       = fullName ?? "",
            Email          = email ?? ""
        });
    }
}
```

6. Add `[Authorize]` to all other controllers (`EmployeesController`, `DepartmentsController`, `VacationRequestsController`, `TripsController`).

7. Add `GET /api/auth/me` endpoint as shown above — the frontend uses this to restore session state on page refresh.

8. Update CORS policy to allow credentials:

```csharp
policy.AllowCredentials();  // required for cookie-based auth cross-origin
```

### Done Criteria

- `POST /api/auth/login` with valid credentials sets a session cookie and returns the employee object.
- `POST /api/auth/logout` clears the session.
- `GET /api/auth/me` returns the current user from the active session.
- All other endpoints return `401` when called without a valid session cookie.
- Unauthenticated requests are never redirected — they always get a JSON `401`.

---

## Phase 3 — Service Layer Extraction

**Goal:** Move all business logic out of controllers into dedicated service classes. Controllers become thin HTTP adapters — they parse requests, call services, and map results to HTTP responses.  
**Risk:** High — largest behavioral refactor. Must be done feature by feature, not all at once.  
**Effort:** High  
**Prerequisite:** Phase 2

### Approach

Extract one feature at a time in this order: Departments → VacationRequests → Trips → Employees (most complex, saved for last).

For each feature:
1. Define the `IXxxService` interface in `HR.Application/Feature/`.
2. Implement `XxxService` using `ApplicationDbContext` directly (repositories come in Phase 4).
3. Register the service in DI.
4. Rewrite the controller to inject the interface and call the service.
5. Verify the feature works end-to-end before moving to the next.

### Service Interface Pattern

Every service method returns `Result<T>` for write operations and plain types (or `null`) for reads:

```csharp
public interface IEmployeeService
{
    Task<PagedList<EmployeeResponse>> GetEmployeesAsync(
        EmployeeStatus? status, int page, int pageSize, CancellationToken ct);

    Task<EmployeeResponse?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<Result<EmployeeCreatedResponse>> CreateAsync(
        EmployeeCreateRequest request, CancellationToken ct);

    Task<Result<EmployeeResponse>> UpdateAsync(
        Guid id, EmployeeUpdateRequest request, CancellationToken ct);

    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
}
```

### Thin Controller Pattern

After extraction, every controller action follows the same four-line pattern:

```csharp
[HttpPost]
public async Task<ActionResult<EmployeeCreatedResponse>> CreateEmployee(
    [FromBody] EmployeeCreateRequest request, CancellationToken ct)
{
    if (!ModelState.IsValid) return ValidationProblem(ModelState);
    var result = await _employeeService.CreateAsync(request, ct);
    if (!result.IsSuccess) return MapError(result.Error!);
    return CreatedAtAction(nameof(GetEmployee), new { id = result.Value!.Employee.Id }, result.Value);
}

private ActionResult MapError(ServiceError error) => error.Code switch
{
    "NOT_FOUND"     => NotFound(new { error.Message }),
    "CONFLICT"      => Conflict(new { error.Message }),
    "BUSINESS_RULE" => UnprocessableEntity(new { error.Message }),
    _               => BadRequest(new { error.Message })
};
```

### Pagination on List Endpoints

All `GetXxx` list endpoints must accept `page` and `pageSize` query parameters with sensible defaults:

```csharp
[HttpGet]
public async Task<ActionResult<PagedList<EmployeeResponse>>> GetEmployees(
    [FromQuery] EmployeeStatus? status,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 25,
    CancellationToken ct = default)
{
    return Ok(await _employeeService.GetEmployeesAsync(status, page, pageSize, ct));
}
```

Default `pageSize` is 25. Maximum allowed `pageSize` is 100 (enforced in the service).

### CancellationToken

Every async method in every controller and service must accept and forward `CancellationToken`. No exceptions.

### Done Criteria

- No controller directly references `ApplicationDbContext`.
- No controller contains `if/else` business validation logic.
- Every list endpoint is paginated.
- All async paths carry `CancellationToken`.
- Each service is independently unit-testable by mocking its dependencies.

---

## Phase 4 — Repository Pattern & Entity Configurations

**Goal:** Abstract data access behind repository interfaces, and split `OnModelCreating` into per-entity `IEntityTypeConfiguration<T>` classes.  
**Risk:** Low — changes are behind interfaces already established in Phase 3.  
**Effort:** Medium  
**Prerequisite:** Phase 3

### Repository Interface Pattern

Use tailored repository methods for the reads and mutations required by
current workflows. Repositories do not expose raw `IQueryable<T>` values
or own `SaveChangesAsync`; the infrastructure-owned `IUnitOfWork`
coordinates saves and transactions. The Phase 4 contracts are documented
in [Internal Repository Contracts](specs/004-repository-entity-configurations/contracts/repository-contracts.md).

Repositories are registered as `Scoped`. They are injected into services; services no longer reference `ApplicationDbContext` directly.

Phase 4 implementation safety amendments:

- Use an infrastructure-owned unit-of-work boundary for atomic employee operations that span HR records and Identity.
- Move EF paging execution into one infrastructure-owned `PagedQueryExecutor`; keep `HR.Shared` EF-free after caller migration.
- Replace the raw employee entity in the internal authentication result with an application DTO while preserving public login behavior.
- Add only the narrow Infrastructure registrations required by Phase 4. Full DI ownership cleanup remains Phase 6.

### Entity Configuration Pattern

Replace the 120-line `OnModelCreating` with individual configuration classes:

```csharp
// HR.Infrastructure/Data/Configurations/EmployeeConfiguration.cs
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> entity)
    {
        entity.Property(e => e.EmployeeNumber).HasMaxLength(20).IsRequired();
        entity.HasIndex(e => e.EmployeeNumber).IsUnique();
        entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
        entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);

        entity.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Manager)
            .WithMany(e => e.DirectReports)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<ApplicationUser>()
            .WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

`OnModelCreating` becomes a minimal Identity-compatible body:

```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);
    builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
}
```

### Done Criteria

- Services reference only repository interfaces, not `ApplicationDbContext`.
- `OnModelCreating` contains the required Identity base call first, followed by assembly scanning, with no inline custom entity mappings.
- Each entity has its own configuration class in `HR.Infrastructure/Data/Configurations/`.

---

## Phase 5 — HR Business Logic Improvements

**Goal:** Improve the correctness and realism of existing HR workflows. Add all the missing business rules that a practical HR system must enforce. Changes build on the Phase 4 repository/entity-configuration refactor and should not be implemented against the old direct-`ApplicationDbContext` service structure.
**Risk:** Low–Medium — additive business-rule changes through services/repositories, plus one migration for new fields.
**Effort:** Medium
**Prerequisite:** Phase 4

> Phase 5 dependency clarification: Phase 5 now depends on completed Phase 4 repository and entity-configuration work. Implement Phase 5 through the repository and unit-of-work boundaries introduced in Phase 4, not by reintroducing direct `ApplicationDbContext` service access.

### 5a — Vacation Overlap Validation

Before creating a vacation request, reject it if the employee already has a pending or approved request that overlaps the requested date range.

```csharp
var overlaps = await _vacationRequestRepository
    .HasOverlappingPendingOrApprovedAsync(
        request.EmployeeId,
        request.StartDate,
        request.EndDate,
        ct);

if (overlaps)
    return Result<VacationRequestResponse>.Failure(
        ServiceError.BusinessRule(
            "Employee already has a vacation request overlapping this period."));
```

### 5b — Active Employee Guard on Vacation Requests

Suspended or terminated employees may not submit vacation requests.

```csharp
if (employee.Status != EmployeeStatus.Active)
    return Result<VacationRequestResponse>.Failure(
        ServiceError.BusinessRule(
            "Only active employees can submit vacation requests."));
```

### 5c — Self-Approval Prevention

When a manager updates a vacation request status, prevent the requester from approving their own request. The approver's `employee_id` is read from the authenticated session claims.

```csharp
var approverId = Guid.Parse(User.FindFirstValue("employee_id")!);
if (approverId == vacationRequest.EmployeeId)
    return Result<VacationRequestResponse>.Failure(
        ServiceError.BusinessRule(
            "Employees cannot approve or reject their own vacation requests."));
```

### 5d — Circular Manager Chain Guard

The existing code prevents `ManagerId == EmployeeId` but not deeper circular chains (A → B → A). Add a depth-limited traversal on manager updates:

```csharp
private async Task<bool> WouldCreateCycleAsync(
    Guid employeeId, Guid proposedManagerId, CancellationToken ct)
{
    var current = proposedManagerId;
    for (var depth = 0; depth < 20; depth++)
    {
        if (current == employeeId) return true;
        var managerId = await _employeeRepo.GetManagerIdAsync(current, ct);
        if (managerId is null) return false;
        current = managerId.Value;
    }
    return false;
}
```

### 5e — Reviewer Audit on Vacation Requests

Add `ReviewedByEmployeeId` and `ReviewedAt` to `VacationRequest` for a clear audit trail. Requires a new migration.

```csharp
// VacationRequest.cs (Domain)
public Guid? ReviewedByEmployeeId { get; set; }
public Employee? ReviewedBy { get; set; }
public DateTimeOffset? ReviewedAt { get; set; }

// Set in VacationService.UpdateStatusAsync
vacationRequest.ReviewedByEmployeeId = approverId;
vacationRequest.ReviewedAt = DateTimeOffset.UtcNow;
vacationRequest.UpdatedAt = DateTimeOffset.UtcNow;
```

### 5f — Vacation Status Transition Rules

Only certain status transitions are valid. Same-status requests are idempotent no-op successes: they do not update reviewer fields, timestamps, or balances again. Different-status changes must follow the state machine:

| From | Allowed To |
|------|-----------|
| Pending | Approved, Rejected |
| Approved | Rejected (cancellation by manager) |
| Rejected | *(terminal — no transitions)* |

```csharp
private static readonly Dictionary<VacationRequestStatus, HashSet<VacationRequestStatus>> _allowedTransitions = new()
{
    [VacationRequestStatus.Pending]  = new() { VacationRequestStatus.Approved, VacationRequestStatus.Rejected },
    [VacationRequestStatus.Approved] = new() { VacationRequestStatus.Rejected },
    [VacationRequestStatus.Rejected] = new()
};

if (!_allowedTransitions[vacationRequest.Status].Contains(request.Status))
    return Result<VacationRequestResponse>.Failure(
        ServiceError.BusinessRule(
            $"Cannot transition from '{vacationRequest.Status}' to '{request.Status}'."));
```

### 5g — Vacation Balance Tracking

Add `VacationBalanceDays` (int, default 21) to `Employee`. Each approved vacation deducts the number of working days. Creating a request validates sufficient balance before submission.

```csharp
// Employee.cs — new field
public int VacationBalanceDays { get; set; } = 21;

// In VacationService.CreateAsync — after overlap check
var requestedDays = CountWorkingDays(request.StartDate, request.EndDate);
if (requestedDays > employee.VacationBalanceDays)
    return Result<VacationRequestResponse>.Failure(
        ServiceError.BusinessRule(
            $"Insufficient vacation balance. Requested {requestedDays} days but only {employee.VacationBalanceDays} remain."));

// In VacationService.UpdateStatusAsync — on approval
if (request.Status == VacationRequestStatus.Approved)
{
    var days = CountWorkingDays(vacationRequest.StartDate, vacationRequest.EndDate);
    employee.VacationBalanceDays -= days;
}

// On rejection of a previously approved request — refund
if (vacationRequest.Status == VacationRequestStatus.Approved
    && request.Status == VacationRequestStatus.Rejected)
{
    var days = CountWorkingDays(vacationRequest.StartDate, vacationRequest.EndDate);
    employee.VacationBalanceDays += days;
}

// Helper
private static int CountWorkingDays(DateOnly start, DateOnly end)
{
    var count = 0;
    for (var d = start; d <= end; d = d.AddDays(1))
        if (d.DayOfWeek != DayOfWeek.Friday && d.DayOfWeek != DayOfWeek.Saturday)
            count++;
    return count;
}
```

> **Note:** Weekend days are Friday+Saturday (common in MENA region). Adjust to Saturday+Sunday if needed.

### 5h — Prevent Vacation Requests in the Past

Reject vacation requests where `StartDate` is before today.

```csharp
if (request.StartDate < DateOnly.FromDateTime(DateTime.UtcNow))
    return Result<VacationRequestResponse>.Failure(
        ServiceError.BusinessRule("Vacation start date cannot be in the past."));
```

### 5i — Minimum Vacation Notice Period

Require at least 3 working days advance notice for vacation requests (configurable).

```csharp
var minNoticeDays = 3;
var today = DateOnly.FromDateTime(DateTime.UtcNow);
var noticeDays = _workingDayCalendar.CountFullWorkingDaysBetween(
    today,
    request.StartDate);

if (noticeDays < minNoticeDays)
    return Result<VacationRequestResponse>.Failure(
        ServiceError.BusinessRule(
            $"Vacation requests must be submitted at least {minNoticeDays} working days in advance."));
```

### 5j — Employee Status Transition Rules

Not all status transitions should be allowed. Enforce valid transitions:

| From | Allowed To |
|------|-----------|
| Active | Suspended, Terminated |
| Suspended | Active, Terminated |
| Terminated | *(terminal — no transitions)* |

```csharp
private static readonly Dictionary<EmployeeStatus, HashSet<EmployeeStatus>> _employeeTransitions = new()
{
    [EmployeeStatus.Active]     = new() { EmployeeStatus.Suspended, EmployeeStatus.Terminated },
    [EmployeeStatus.Suspended]  = new() { EmployeeStatus.Active, EmployeeStatus.Terminated },
    [EmployeeStatus.Terminated] = new()
};

if (employee.Status != request.Status
    && !_employeeTransitions[employee.Status].Contains(request.Status))
    return Result<EmployeeResponse>.Failure(
        ServiceError.BusinessRule(
            $"Cannot transition employee from '{employee.Status}' to '{request.Status}'."));
```

### 5k — Auto-Cancel Pending Vacations on Termination

Same-status requests return success without updating `TerminatedAt`, rejecting pending vacations again, revoking access again, or mutating timestamps solely because the requested status already matches the current status.

When an employee is terminated, automatically reject all their pending vacation requests.

```csharp
if (request.Status == EmployeeStatus.Terminated)
{
    var pendingVacations = await _vacationRequestRepository
        .GetPendingByEmployeeIdAsync(id, ct);

    foreach (var v in pendingVacations)
    {
        v.Status = VacationRequestStatus.Rejected;
        v.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

### 5l — Employee Number Immutability

`EmployeeNumber` must never change after creation. The update endpoint should ignore or reject changes to it.

```csharp
if (employee.EmployeeNumber != request.EmployeeNumber)
    return Result<EmployeeResponse>.Failure(
        ServiceError.BusinessRule("Employee number cannot be changed after creation."));
```

### 5m — Soft Delete for Employees

Instead of hard-deleting employees (which destroys audit history), mark them as terminated and set a `TerminatedAt` timestamp. Preserve their data for reporting.

```csharp
// Employee.cs — new fields
public DateTimeOffset? TerminatedAt { get; set; }
public bool IsDeleted { get; set; } = false;

// In EmployeeService.DeleteAsync — soft delete instead of remove
employee.Status = EmployeeStatus.Terminated;
employee.TerminatedAt = DateTimeOffset.UtcNow;
employee.IsDeleted = true;

// Add global query filter in EF configuration
entity.HasQueryFilter(e => !e.IsDeleted);
```

### 5n — Department Head / Manager Validation

A manager assigned to an employee should ideally be from the same department or a parent department. At minimum, warn or reject if the manager is from a completely unrelated department. Also, prevent deleting a department that has sub-departments if you add hierarchy later.

```csharp
// Soft rule: log a warning if manager is from a different department
if (manager.DepartmentId != request.DepartmentId)
    _logger.LogWarning(
        "Employee {EmpId} assigned manager {MgrId} from a different department",
        id, request.ManagerId);
```

### 5o — Duplicate Email Guard

Prevent two active employees from sharing the same email address.

```csharp
if (await _employeeRepository.ExistsActiveWithEmailAsync(
        request.Email,
        excludeEmployeeId: id,
        ct))
    return Result<EmployeeResponse>.Failure(
        ServiceError.Conflict("An active employee with this email already exists."));
```

### 5p — Trip-Employee Relationship

Currently `Trip` has no link to employees. Add `RequestedByEmployeeId` so new trips are traceable, while existing rows may retain a null requester when no reliable historical requester source exists. Requires a migration strategy and one new migration. Do not invent fake requester data for existing trips; any later non-null database constraint requires a separate approved migration after reliable backfill is possible.

```csharp
// Trip.cs — new fields
public Guid? RequestedByEmployeeId { get; set; }
public Employee? RequestedBy { get; set; }

// TripCreateRequest.cs — add
public Guid RequestedByEmployeeId { get; set; }

// In TripService.CreateAsync — validate
var employee = await _employeeRepository.GetByIdAsync(
    request.RequestedByEmployeeId,
    ct);
if (employee is null)
    return Result<TripResponse>.Failure(ServiceError.NotFound("Employee not found."));
if (employee.Status != EmployeeStatus.Active)
    return Result<TripResponse>.Failure(
        ServiceError.BusinessRule("Only active employees can request trips."));
```

### 5q — Trip Date Validation

Reject trips with a date in the past. Validate that trip date is a working day.

```csharp
if (request.TripDate < DateOnly.FromDateTime(DateTime.UtcNow))
    return Result<TripResponse>.Failure(
        ServiceError.BusinessRule("Trip date cannot be in the past."));
```

### 5r — Department Employee Count on Response

Return the employee count in each `DepartmentResponse` so the frontend can display it without a separate call.

```csharp
// DepartmentResponse.cs — add
public int EmployeeCount { get; set; }

// In DepartmentService.GetAllAsync
var departments = await _departmentRepository
    .GetPageWithEmployeeCountsAsync(page, pageSize, ct);

return departments.Map(d => new DepartmentResponse
{
    Id = d.Id,
    Name = d.Name,
    EmployeeCount = d.EmployeeCount
});
```

### Done Criteria

- All original 5a–5e rules still apply.
- Vacation status transitions follow the defined state machine.
- Vacation balance is tracked and enforced.
- Past-date and notice-period rules prevent invalid vacation requests.
- Employee status transitions follow the defined state machine.
- Pending vacations are auto-cancelled on employee termination.
- Employee numbers are immutable after creation.
- Employees are soft-deleted, not hard-deleted.
- Duplicate emails are rejected for active employees.
- Trips are linked to employees with validation.
- Trip dates cannot be in the past.
- Department responses include employee counts.

---

## Phase 6 — DI Registration Cleanup

**Goal:** Centralize dependency injection registration so `Program.cs` is readable and each project manages its own registrations.  
**Risk:** Low — wiring only.  
**Effort:** Low  
**Prerequisite:** Phase 4

### Tasks

1. Create `DependencyInjection.cs` in `HR.Application`:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IVacationService, VacationService>();
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
```

2. Create `DependencyInjection.cs` in `HR.Infrastructure`:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IVacationRepository, VacationRepository>();
        services.AddScoped<ITripRepository, TripRepository>();

        return services;
    }
}
```

3. `Program.cs` wiring becomes:

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

### Done Criteria

- `Program.cs` contains no direct `services.AddScoped<>` calls for application or infrastructure services.
- Each project fully owns its own DI registration.

---

## Phase 7 — Advanced HR Features

**Goal:** Add practical HR features that round out the system for real-world use. These build on the service layer and business rules from previous phases.  
**Risk:** Medium — new domain concepts, new migrations, new endpoints.  
**Effort:** High  
**Prerequisite:** Phase 5, Phase 6

### 7a — Attendance Tracking

Add a simple clock-in/clock-out attendance system.

```csharp
// New entity: AttendanceRecord.cs
public class AttendanceRecord
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? ClockIn { get; set; }
    public TimeOnly? ClockOut { get; set; }
    public double? WorkedHours => (ClockOut.HasValue && ClockIn.HasValue)
        ? (ClockOut.Value.ToTimeSpan() - ClockIn.Value.ToTimeSpan()).TotalHours : null;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

**Business rules:**
- Cannot clock in twice on the same day.
- Cannot clock out without clocking in first.
- Only active employees can record attendance.
- Attendance records cannot be created for future dates.

**Endpoints:**
- `POST /api/attendance/clock-in` — record clock-in for current user
- `POST /api/attendance/clock-out` — record clock-out for current user
- `GET /api/attendance?employeeId=&from=&to=` — list attendance records with date range filter

### 7b — Role-Based Access Control (RBAC)

Add roles so that not every authenticated user can do everything.

```csharp
// New enum: EmployeeRole.cs
public enum EmployeeRole
{
    Employee = 1,
    Manager = 2,
    HRAdmin = 3,
    SystemAdmin = 4
}

// Employee.cs — new field
public EmployeeRole Role { get; set; } = EmployeeRole.Employee;
```

**Authorization rules:**

| Action | Employee | Manager | HRAdmin | SystemAdmin |
|--------|----------|---------|---------|-------------|
| View own profile | ✅ | ✅ | ✅ | ✅ |
| View team members | ❌ | ✅ | ✅ | ✅ |
| View all employees | ❌ | ❌ | ✅ | ✅ |
| Create/edit employees | ❌ | ❌ | ✅ | ✅ |
| Approve vacations | ❌ | ✅ (own team) | ✅ | ✅ |
| View all departments | ✅ | ✅ | ✅ | ✅ |
| Create/edit departments | ❌ | ❌ | ✅ | ✅ |
| Manage trips | ❌ | ✅ | ✅ | ✅ |

Add claims-based `[Authorize(Roles = "HRAdmin,SystemAdmin")]` or a custom policy-based approach.

### 7c — Salary & Compensation (Basic)

Track basic salary information per employee.

```csharp
// Employee.cs — new fields
public decimal? BaseSalary { get; set; }
public string? SalaryCurrency { get; set; } = "EGP";
public DateOnly? LastSalaryReviewDate { get; set; }
```

**Business rules:**
- Only HRAdmin/SystemAdmin can view or edit salary data.
- Salary fields are excluded from regular `EmployeeResponse` — returned only in a separate `GET /api/employees/{id}/compensation` endpoint.
- Salary changes are logged (create a `SalaryHistory` entity for audit).

### 7d — Document / Attachment Management

Allow employees to have documents attached (ID copies, contracts, certificates).

```csharp
// New entity: EmployeeDocument.cs
public class EmployeeDocument
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // "ID", "Contract", "Certificate", "Other"
    public string StoragePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public Guid UploadedByEmployeeId { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

**Endpoints:**
- `POST /api/employees/{id}/documents` — upload a document
- `GET /api/employees/{id}/documents` — list documents for an employee
- `DELETE /api/employees/{id}/documents/{docId}` — remove a document

### 7e — Dashboard / Summary Statistics Endpoint

Provide an API endpoint for the frontend dashboard.

```csharp
// GET /api/dashboard/summary
public class DashboardSummary
{
    public int TotalActiveEmployees { get; set; }
    public int TotalDepartments { get; set; }
    public int PendingVacationRequests { get; set; }
    public int ApprovedVacationsThisMonth { get; set; }
    public int EmployeesOnVacationToday { get; set; }
    public int NewHiresThisMonth { get; set; }
    public int UpcomingTripsThisWeek { get; set; }
    public Dictionary<string, int> EmployeesPerDepartment { get; set; } = new();
    public Dictionary<string, int> VacationRequestsByStatus { get; set; } = new();
}
```

### 7f — Audit Log

Track all significant actions (create, update, delete, status changes) for compliance.

```csharp
// New entity: AuditLog.cs
public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;   // "Employee", "VacationRequest", etc.
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;       // "Created", "Updated", "Deleted", "StatusChanged"
    public string? OldValues { get; set; }                    // JSON snapshot
    public string? NewValues { get; set; }                    // JSON snapshot
    public Guid PerformedByEmployeeId { get; set; }
    public Employee? PerformedBy { get; set; }
    public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

**Endpoint:** `GET /api/audit-logs?entityType=&entityId=&from=&to=` — filterable, paginated, HRAdmin+ only.

### Done Criteria

- Attendance clock-in/clock-out works with all validations.
- Role-based access control restricts endpoints appropriately.
- Salary data is stored and access-controlled.
- Documents can be uploaded and retrieved per employee.
- Dashboard endpoint returns real-time summary statistics.
- Audit logs capture all significant write operations.

---

## Phase Summary

| Phase | Name | Risk | Effort | Prerequisite |
|-------|------|------|--------|-------------|
| 0 | Foundation & project restructure | Low | Medium | None |
| 1 | Global exception handling + pagination helper | Low | Low | Phase 0 |
| 2 | Session-based authentication & authorization | Medium | Medium | Phase 1 |
| 3 | Service layer extraction | High | High | Phase 2 |
| 4 | Repository pattern + entity configurations | Low | Medium | Phase 3 |
| 5 | HR business logic improvements | Low–Medium | Medium | Phase 4 |
| 6 | DI registration cleanup | Low | Low | Phase 4 |
| 7 | Advanced HR features (attendance, RBAC, salary, docs, dashboard, audit) | Medium | High | Phase 5 + 6 |

**Implementation order is mandatory.** Do not start Phase 3 until Phase 2 is complete — services need to read the authenticated user's identity from session claims. Do not skip Phase 1 — global error handling makes debugging all subsequent phases significantly easier. Phase 7 is optional but highly recommended for a production-ready HR system.

---

*End of spec. Each phase is a standalone pull request. Review and merge one phase at a time.*
