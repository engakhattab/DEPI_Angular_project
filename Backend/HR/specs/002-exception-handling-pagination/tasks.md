---
description: "Task list for Phase 1 — Global Exception Handling & Pagination Infrastructure"
---

# Tasks: Phase 1 — Global Exception Handling & Pagination Infrastructure

**Input**: Design documents from `specs/002-exception-handling-pagination/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/, research.md, quickstart.md

**Note**: The .NET solution lives at `d:\DEPI_Angular_project\Backend\Company-02\HR\`. All file paths below are relative to that solution root.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Ensure the HR.Shared project has the EF Core dependency needed for PagedList<T>

- [x] T001 Add the NuGet package `Microsoft.EntityFrameworkCore` (version 8.0.20) to `HR.Shared/HR.Shared.csproj`. Add this ItemGroup block after the PropertyGroup: `<ItemGroup><PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.20" /></ItemGroup>`. This is needed because PagedList<T> uses `CountAsync` and `ToListAsync` extension methods from EF Core.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No additional foundational work needed — Phase 0 already created domain exceptions and Result types.

**Checkpoint**: Phase 0 artifacts confirmed present (NotFoundException.cs, ConflictException.cs, BusinessRuleException.cs in HR.Domain/Exceptions/).

---

## Phase 3: User Story 1 - Consistent Error Responses (Priority: P1) 🎯 MVP

**Goal**: Create a middleware that catches all exceptions thrown anywhere in the API pipeline and returns a consistent JSON error response `{ "code": "...", "message": "..." }` with the correct HTTP status code. No raw exception text or stack traces should ever reach the client.

**Independent Test**: Start the API, hit a non-existent endpoint or throw an exception from a controller — verify the response is always JSON with `code` and `message` fields, and the correct HTTP status code.

### Implementation for User Story 1

- [x] T002 [US1] Create the directory `HR.API/Middleware/` if it does not already exist. Then create the file `HR.API/Middleware/GlobalExceptionMiddleware.cs` with the following EXACT content:

```csharp
using HR.Domain.Exceptions;

namespace HR.API.Middleware;

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

- [x] T003 [US1] Register the middleware in `HR.API/Program.cs`. Find the line `var app = builder.Build();` and add the middleware registration IMMEDIATELY after it (before any other `app.Use...` calls). Add this line: `app.UseMiddleware<HR.API.Middleware.GlobalExceptionMiddleware>();`. Also add the using statement `using HR.API.Middleware;` at the top of the file if not already present. The middleware MUST be the FIRST middleware in the pipeline (before UseSwagger, UseHttpsRedirection, UseCors, UseAuthorization, etc.). The final pipeline order should be:

```
var app = builder.Build();
app.UseMiddleware<GlobalExceptionMiddleware>();  // ← ADD THIS LINE
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();
app.Run();
```

**Checkpoint**: At this point, the API should compile and run. Any unhandled exception from any controller should return `{ "code": "...", "message": "..." }` JSON. Test by throwing `new NotFoundException("test")` from any controller action temporarily — you should get a 404 JSON response.

---

## Phase 4: User Story 2 - Pagination Infrastructure (Priority: P2)

**Goal**: Create a reusable `PagedList<T>` class in `HR.Shared` that any service can use to paginate `IQueryable<T>` results. This class is NOT wired to any controller in this phase — it is infrastructure for Phase 3.

**Independent Test**: The project compiles and `PagedList<T>` is accessible from `HR.Application` via the project reference to `HR.Shared`.

### Implementation for User Story 2

- [x] T004 [US2] Create the directory `HR.Shared/Pagination/` if it does not already exist. Then create the file `HR.Shared/Pagination/PagedList.cs` with the following EXACT content:

```csharp
using Microsoft.EntityFrameworkCore;

namespace HR.Shared.Pagination;

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

**Checkpoint**: The entire solution should compile with `dotnet build`. `PagedList<T>` is ready for use in Phase 3 service layer extraction.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Final validation

- [x] T005 Run `dotnet build` from the solution root directory to verify zero compilation errors across all 5 projects (HR.API, HR.Application, HR.Infrastructure, HR.Domain, HR.Shared)
- [x] T006 Run the API with `dotnet run --project HR.API` and verify Swagger UI loads at `https://localhost:<port>/swagger`. Confirm all existing endpoints are listed and return expected responses.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — add NuGet package first
- **User Story 1 (Phase 3)**: Depends on Phase 1 (needs existing domain exceptions from Phase 0, but no dependency on T001)
- **User Story 2 (Phase 4)**: Depends on Phase 1 (T001 — needs EF Core package in HR.Shared)
- **Polish (Phase 5)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Independent — can start after Phase 1
- **User Story 2 (P2)**: Independent — can start after Phase 1
- **US1 and US2 can run in parallel** — they touch different projects and files

### Parallel Opportunities

- T002 and T004 can be created in parallel (different projects: HR.API vs HR.Shared)
- T003 depends on T002 (middleware must exist before registering it)

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete T001 (add EF Core package to HR.Shared)
2. Complete T002 + T003 (create middleware + register it)
3. **STOP and VALIDATE**: Run the API, throw an exception — verify JSON error response
4. This is a deployable MVP — all errors are now handled consistently

### Incremental Delivery

1. Complete MVP (T001–T003)
2. Add T004 (PagedList<T>)
3. Complete T005–T006 (build + verify)
4. Phase 1 is complete and ready for code review
