# Quickstart: Phase 4 - Repository Pattern and Entity Configurations

## Purpose

Use this guide to validate that repository extraction and entity-configuration extraction preserve Phase 3 behavior without schema changes.

## Prerequisites

- Phase 3 automated checks pass.
- Pending Phase 3 authenticated manual checkpoints are complete.
- SQL Server is available through the existing `DefaultConnection`.
- The database contains a valid Identity user and employee record for login.

## Build and Test

From the backend root:

```powershell
dotnet restore .\HR.slnx
dotnet build .\HR.slnx -c Release
dotnet test .\HR.slnx -c Release --no-build
git diff --check
```

## Structural Review

```powershell
rg -n "ApplicationDbContext" .\HR.Infrastructure -g "*Service.cs"
rg -n "builder\.Entity<" .\HR.Infrastructure\Data\ApplicationDbContext.cs
rg -n "IEntityTypeConfiguration<" .\HR.Infrastructure\Data\Configurations
rg -n "AddScoped<I(Department|VacationRequest|Trip|Employee)Repository|AddScoped<IIdentityUserLookup|AddScoped<IUnitOfWork" .\HR.Infrastructure\DependencyInjection.cs
rg -n "Microsoft\.EntityFrameworkCore|CountAsync|ToListAsync|CreateAsync" .\HR.Shared
rg -n "PagedList<.*>\.CreateAsync|PagedList<.*>\s*\.\s*CreateAsync" .\HR.API .\HR.Application .\HR.Infrastructure .\HR.Shared
```

Expected results:

- No service references `ApplicationDbContext`.
- `ApplicationDbContext.OnModelCreating` contains no inline `builder.Entity<T>` mappings.
- Four HR entity configuration classes are found.
- Four repositories, `IIdentityUserLookup`, and `IUnitOfWork` are registered.
- `HR.Shared` contains no EF Core package, namespace, or query-execution references.
- No `PagedList<T>.CreateAsync` caller remains.

Inspect `ApplicationDbContext.OnModelCreating` and confirm the only statements are ordered exactly as follows:

```csharp
base.OnModelCreating(builder);
builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
```

## Model Parity

Run the EF Core pending-model-change check:

```powershell
dotnet ef migrations has-pending-model-changes `
  --project .\HR.Infrastructure\HR.Infrastructure.csproj `
  --startup-project .\HR.API\HR.API.csproj
```

Expected result: no pending model changes. No migration files should be added or modified.

## Run Isolated API

Use a free port so an existing local process is not disrupted:

```powershell
dotnet run --project .\HR.API\HR.API.csproj -c Release --no-launch-profile --urls http://127.0.0.1:5124
```

## Authentication Regression

1. Call a protected endpoint without a session cookie and confirm JSON `401`.
2. Sign in through `POST /api/auth/login` using an email address.
3. Sign out and sign in using an employee number.
4. Call `GET /api/auth/me`.
5. Call `POST /api/auth/logout`.
6. Compare login response JSON before and after the refactor.
7. Inspect the isolated API `Set-Cookie` response and confirm the session cookie retains its existing flags and behavior. A host-level integration test may replace this manual step only if one is explicitly added.
8. Confirm claims retain `NameIdentifier`, email, `employee_id`, `employee_number`, and `full_name`.

## HR Workflow Regression

### Departments

- Browse a page of departments and confirm alphabetical ordering.
- Create, update, retrieve, and delete a department.
- Confirm duplicate names and delete-with-employees return structured errors.

### Vacation Requests

- Browse with pagination and existing `status` / `employeeId` filters.
- Create a valid request and confirm invalid ranges and unknown employees remain rejected.
- Update status and delete a request.

### Trips

- Browse a page of trips and confirm newest-first ordering.
- Create a trip and confirm generated `TRIP-xxxxxx` and `REQ-xxxxxx` shapes.
- Retrieve and delete the trip.

### Employees

- Browse paginated employees with and without status filtering.
- Create an employee with a supplied password and with a generated temporary password.
- Update profile data and email.
- Confirm duplicate employee number, unknown department, unknown manager, and self-manager errors remain structured.
- Delete an employee with direct reports and vacation requests and confirm cleanup completes.

## Scope Reminder

Do not add migrations, schema changes, new HR rules, frontend changes, or Phase 6 dependency-registration cleanup while completing Phase 4.

## Compatibility Review

- Verify authentication errors preserve existing HTTP statuses and error codes.
- Verify representative validation, conflict, and not-found responses preserve existing payloads.
- Verify an unexpected exception still returns the generic `SERVER_ERROR` HTTP `500` payload.
- Verify no Phase 5 soft deletion, state machine, overlap, or circular-manager rule was added.
- Verify no Phase 6 `HR.Application/DependencyInjection.cs` or startup-wiring restructuring was added.
- Verify `HR.API/Program.cs` remains unchanged unless a necessary Phase 4 registration gap is explicitly documented.
