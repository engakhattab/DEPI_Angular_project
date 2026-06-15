# Phase 8 Quickstart: Authorization Scope Foundation

Phase 8 is a shared-service foundation phase. It should not create migrations, change endpoint behavior, or require manual database setup beyond the existing migrated database.

## Prerequisites

- Phase 7 is complete.
- Existing migrations are applied if local SQL Server smoke testing is used.
- The API builds before Phase 8 work starts.

## Implementation Boundary Checklist

Before coding, confirm:

- [ ] Work is limited to `IEmployeeAccessService`, its infrastructure implementation, repository helpers if needed, and focused tests.
- [ ] No employee endpoint hardening is planned in Phase 8.
- [ ] No vacation request hardening is planned in Phase 8.
- [ ] No trip hardening is planned in Phase 8.
- [ ] No migration is planned.
- [ ] No route, response JSON, cookie, claim, status-code, or error-code change is planned.

## Recommended Automated Validation

Run from the repository root:

```powershell
dotnet restore .\HR.slnx
dotnet build .\HR.slnx -c Release
dotnet test .\HR.slnx -c Release --no-build --filter "FullyQualifiedName~Authorization"
dotnet test .\HR.slnx -c Release --no-build
dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

Expected:

- Restore passes.
- Build passes.
- Focused authorization tests pass.
- Full test suite passes.
- EF reports no pending model changes.
- No migration files are created.

## Focused Test Data Shape

Automated tests should create:

- normal employee
- peer employee
- manager
- direct report
- indirect report
- manager's own manager
- unrelated employee
- suspended report
- deleted report
- terminated report
- HR administrator
- System administrator

## Expected Scope Results

| Scenario | Expected |
|----------|----------|
| Employee accesses self | allowed |
| Employee accesses peer | denied |
| Manager accesses self through employee access | allowed |
| Manager accesses direct report | allowed |
| Manager accesses indirect report | allowed |
| Manager accesses peer | denied |
| Manager accesses unrelated employee | denied |
| Manager accesses own manager | denied unless self/organization applies |
| Manager visible IDs include suspended report | no |
| Manager visible IDs include deleted report | no |
| Manager visible IDs include terminated report | no |
| HR administrator has organization scope | yes, unless deleted or terminated |
| System administrator has organization scope | yes, unless deleted or terminated |
| Suspended administrator has organization scope in Phase 8 | yes |
| Deleted requester has scope | no |
| Terminated requester has scope | no |
| Missing requester visible IDs | empty set |

## Optional Manual Smoke

If manual smoke is needed, use existing local SQL Server setup from `API_LIFECYCLE_TESTING_GUIDE.md`.

Minimal smoke:

1. Start API with current migrated local database.
2. Login as System administrator.
3. Call `/api/auth/me` and verify current role is unchanged.
4. Call a representative existing endpoint from each existing module and verify no unexpected 401/403/route/response-shape change occurred.
5. Stop the API.

Do not interpret Phase 8 as a manual endpoint hardening phase. Employee, vacation, and trip over-broad behavior remains known until Phases 9, 10, and 11.

