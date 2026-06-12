# Quickstart: Phase 7 - Advanced HR Features

This guide is for Phase 7 implementation and validation. Do not run migration or database commands until implementation creates the approved Phase 7 migration.

## 1. Baseline

```powershell
dotnet restore .\HR.slnx
dotnet build .\HR.slnx -c Release
dotnet test .\HR.slnx -c Release --no-build
```

Expected: all existing Phase 5 and Phase 6 tests pass before Phase 7 changes.

## 2. Required Configuration

Set development values in `HR.API/appsettings.Development.json`, user secrets, or environment variables.

```json
{
  "BusinessSettings": {
    "TimeZoneId": "Africa/Cairo"
  },
  "InitialAdminBootstrap": {
    "Enabled": true,
    "Mode": "CreateInitialAdmin",
    "EmployeeNumber": "set-per-deployment",
    "Email": "set-per-deployment",
    "FullName": "set-per-deployment",
    "DepartmentId": "00000000-0000-0000-0000-000000000000",
    "TemporaryPassword": "set-via-user-secrets-or-environment",
    "ForcePasswordChange": true
  },
  "DocumentStorage": {
    "RootPath": "App_Data/EmployeeDocuments",
    "AllowedExtensions": [".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx"],
    "MaxFileSizeBytes": 10485760
  }
}
```

PowerShell environment variable examples:

```powershell
$env:BusinessSettings__TimeZoneId = "Africa/Cairo"
$env:InitialAdminBootstrap__Enabled = "true"
$env:InitialAdminBootstrap__Mode = "CreateInitialAdmin"
$env:InitialAdminBootstrap__EmployeeNumber = "<set-per-client>"
$env:InitialAdminBootstrap__Email = "<set-per-client>"
$env:InitialAdminBootstrap__FullName = "<set-per-client>"
$env:InitialAdminBootstrap__DepartmentId = "<existing-department-guid>"
$env:InitialAdminBootstrap__TemporaryPassword = "<set-via-secret-store>"
$env:InitialAdminBootstrap__ForcePasswordChange = "true"
$env:DocumentStorage__RootPath = "App_Data/EmployeeDocuments"
$env:DocumentStorage__MaxFileSizeBytes = "10485760"
```

Validation requirements:

- Missing or invalid `BusinessSettings:TimeZoneId` must fail startup clearly.
- If no active System administrator exists, `InitialAdminBootstrap` must create one admin from secure configuration or fail clearly without partial records.
- If an active System administrator already exists, bootstrap must do nothing and must not require the creation secrets.
- `DocumentStorage:RootPath` must not be a public static folder unless explicitly protected.

## 3. Migration Workflow

After implementation adds the Phase 7 model changes:

```powershell
dotnet ef migrations add Phase7AdvancedHrFeatures --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
dotnet ef database update --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

Expected:

- One new Phase 7 migration pair is created.
- Existing migrations are not edited.
- Pending model changes check passes after migration.

## 4. Automated Validation

```powershell
dotnet restore .\HR.slnx
dotnet build .\HR.slnx -c Release
dotnet test .\HR.slnx -c Release --no-build
```

Focused coverage expected:

- RBAC access matrix for Employee, Manager, HR administrator, and System administrator.
- Initial System Administrator bootstrap creation, idempotency, existing-admin no-op, and failure cases.
- Attendance timezone behavior with pinned timezone and UTC boundary cases.
- Attendance duplicate clock-in and clock-out-before-clock-in failures.
- Compensation isolation from normal employee responses.
- Salary history creation only on value changes.
- Document upload validation, safe stored names, authorized download, and partial-failure cleanup.
- Dashboard role scoping and metric correctness.
- Audit-log redaction/summarization and filtering.
- Phase 5 vacation, employee, department, trip, and auth revocation regressions.

## 5. Static Checks

```powershell
rg -n "HR.Infrastructure" .\HR.Application
rg -n "BaseSalary|Salary|Compensation" .\HR.Application\DTOs\Employees
rg -n "AddScoped<" .\HR.API\Program.cs
git status --short .\HR.Infrastructure\Data\Migrations
git diff --check
```

Expected:

- `HR.Application` does not reference `HR.Infrastructure`.
- Normal employee DTOs do not expose compensation values.
- `Program.cs` does not directly register application/infrastructure scoped services.
- Migration status shows exactly the approved Phase 7 migration changes.

## 6. Local SQL Server Smoke

Use local `HrSystemDb` after applying Phase 7 migration.

Suggested smoke flow:

1. Start `HR.API`.
2. Confirm invalid timezone configuration fails startup with a clear message, then restore valid config.
3. Confirm the configured initial administrator is created when no active System administrator exists, can log in, and receives additive role context.
4. Confirm an ordinary employee cannot perform admin-only actions.
5. Promote a manager and confirm manager-scoped access is limited to direct/indirect reports.
6. Clock in and clock out as an active employee.
7. Attempt duplicate clock-in and clock-out without clock-in.
8. View and update compensation as HR administrator; confirm normal employee response excludes salary.
9. Upload, list, download, and remove a document; confirm unauthorized download is denied.
10. View dashboard summary as manager and HR administrator.
11. Query audit logs for role, attendance, compensation, document, and employee changes.
12. Re-run representative Phase 5 vacation, employee, department, and trip workflows.

## 7. Handoff Notes

Record these in the Phase 7 completion summary, not in `tasks.md`:

- Commands run and results.
- Migration name and database update status.
- Initial System Administrator bootstrap configuration used in local validation. Do not record temporary passwords.
- Business timezone used in local validation.
- Document storage root used in local validation.
- Any baseline failures or environment-specific issues.
