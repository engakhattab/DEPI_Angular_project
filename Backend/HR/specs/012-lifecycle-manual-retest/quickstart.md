# Quickstart: Phase 12 - Lifecycle Documentation and Manual Retest

This quickstart is for the Phase 12 implementer. It updates documentation and executes local manual validation only.

## 1. Confirm Active Feature

```powershell
Get-Content .\.specify\feature.json
```

Expected feature directory:

```json
{
  "feature_directory": "specs/012-lifecycle-manual-retest"
}
```

## 2. Set Local Validation Connection String

Use the disposable Phase 12 database. Do not commit the server name or connection string.

```powershell
$env:ConnectionStrings__DefaultConnection="Server=<LOCAL_SQL_SERVER>;Database=HrSystemDb_Phase12LifecycleTest;Trusted_Connection=True;TrustServerCertificate=True"
```

## 3. Baseline Validation

```powershell
dotnet restore .\HR.slnx
dotnet build .\HR.slnx -c Release -p:UseSharedCompilation=false
dotnet test .\HR.slnx -c Release --no-build
dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

If `--no-build` is not safe because the Release output is stale, run:

```powershell
dotnet test .\HR.slnx -c Release
```

## 4. Prepare Disposable Database

Apply existing approved migrations only.

```powershell
dotnet ef database update --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
dotnet ef migrations list --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

Expected migration history includes:

```text
20251114215718_InitialCreate
20260603014628_Phase5HrBusinessRules
20260606235241_Phase7AdvancedHrFeatures
20260615170903_AddVacationRequestCreatedByEmployee
20260615212225_AddTripRequesterEmployee
```

## 5. Update Documentation

Review and update:

- `API_LIFECYCLE_TESTING_GUIDE.md`
- `CLIENT_INSTALLATION_GUIDE.md`
- `specs/012-lifecycle-manual-retest/manual-retest-checklist.md`
- `specs/012-lifecycle-manual-retest/implementation-summary.md`

Do not update source code. Do not create migrations.

## 6. Create/Verify Local Test Actors

Use local-only placeholder identities:

| Employee Number | Email | Role |
|-----------------|-------|------|
| `EMP001` | `admin@test.com` | `SystemAdministrator` |
| `EMP002` | `hr.admin@test.com` | `HRAdministrator` |
| `EMP003` | `manager@test.com` | `Manager` |
| `EMP004` | `employee@test.com` | `Employee` |

Ensure `EMP003` manages `EMP004`, and at least one outside-scope target exists for negative Employee/Manager checks.

## 7. Execute Manual Retest

Run role-specific scenarios from the manual retest checklist:

- Auth/session and `/api/auth/me`
- Employee list/detail/create/update/delete/role assignment
- Vacation list/detail/create/review/delete and self-review denial
- Trip list/detail/create/delete and traveler/requester behavior
- Attendance, compensation, documents, dashboard, and audit sensitive access checks
- Expected 401/403/404/409/422 structured error checks

Use Swagger or another HTTP client that preserves cookies. Log out or clear cookies before switching actors.

## 8. Record Evidence

Record in `specs/012-lifecycle-manual-retest/implementation-summary.md`:

- Database name: `HrSystemDb_Phase12LifecycleTest`
- Connection string source, without secrets
- Commands run and results
- Manual checklist summary
- Failed or blocked scenarios
- Follow-up defects requiring separate approval
- Confirmation that no source code was changed
- Confirmation that no new migration was created

## 9. Final Checks

```powershell
git diff --check
git status --short
```

Confirm changed files are documentation/Spec Kit artifacts only unless the user separately approves a defect fix.
