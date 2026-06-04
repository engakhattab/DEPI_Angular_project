# Quickstart: Phase 6 - DI Registration Cleanup

Use this guide after implementation to validate that dependency registration ownership changed without changing runtime behavior.

## Prerequisites

- .NET 8 SDK
- SQL Server local database configured in `HR.API/appsettings.json`
- Phase 5 migration applied to `HrSystemDb`
- Existing or test employee credentials for manual login

## Apply Existing Phase 5 Migration First

Phase 6 does not create a migration, but manual runtime validation expects the Phase 5 schema.

```powershell
dotnet ef database update --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

Confirm the Phase 5 migration exists in the database:

```sql
SELECT MigrationId
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;
```

Expected migration:

```text
20260603014628_Phase5HrBusinessRules
```

## Automated Validation

```powershell
dotnet restore .\HR.slnx
dotnet build .\HR.slnx -c Release
dotnet test .\HR.slnx -c Release --no-build
```

## Static Checks

```powershell
rg -n "AddScoped<" .\HR.API\Program.cs
rg -n "AddDbContext|AddIdentityCore|AddEntityFrameworkStores" .\HR.API\Program.cs
rg -n "ApplicationDbContext" .\HR.Infrastructure -g "*Service.cs"
rg -n "HR.Infrastructure" .\HR.Application
git status --short .\HR.Infrastructure\Data\Migrations
git diff --check
```

Expected:

- No direct application or infrastructure scoped service registrations in `Program.cs`.
- No direct EF Core or Identity store registration calls in `Program.cs`.
- No service-level `ApplicationDbContext` dependencies.
- No `HR.Application` reference to `HR.Infrastructure`.
- No Phase 6 migration changes.

## Manual Smoke Test

Stop any already-running API process before building or running:

```powershell
Get-Process HR.API -ErrorAction SilentlyContinue
```

Start the API:

```powershell
dotnet run --project .\HR.API\HR.API.csproj --launch-profile https
```

Open:

```text
https://localhost:7162/swagger
```

### 1. Authentication

1. Log in with an active employee.
2. Confirm the login response still contains `employee`.
3. Confirm authenticated calls keep working with the session cookie.
4. Confirm invalid credentials still return a structured `401`.

### 2. Representative HR Workflows

Run one read or write smoke check for each area:

- Departments: list departments and confirm `employeeCount` still appears.
- Employees: list employees and fetch one employee detail.
- Vacation requests: list requests or create a valid request using Phase 5 rules.
- Trips: list trips or create a valid trip using Phase 5 rules.

### 3. Auth Revocation Smoke

1. Log in as an active employee.
2. Terminate or soft-delete that employee using an authorized session.
3. Retry an authenticated request with the old session.
4. Expected: access is rejected without waiting for logout or cookie expiry.

## Completion Notes

Record:

- Whether `HrSystemDb` already had the Phase 5 migration.
- Credentials or seeded test data used.
- Any environment-specific startup issues.
- Final automated and manual validation results.
