# Quickstart: Phase 11 Trip Ownership and Scope Hardening

Use this guide after implementation to manually smoke test Phase 11 behavior. It assumes Phase 8, Phase 9, and Phase 10 are already complete.

## Preconditions

- Run the API against a local SQL Server database with representative employees and trips.
- Have test users for:
  - normal employee
  - manager with active direct and indirect reports
  - HR administrator
  - system administrator
- Include trips for:
  - the normal employee
  - the manager
  - an active direct report
  - an active indirect report
  - a peer or unrelated employee
  - a soft-deleted report
  - a terminated report

## Build and Test

From the repository root:

```powershell
dotnet restore .\HR.slnx
dotnet build .\HR.slnx -c Release
dotnet test .\HR.slnx -c Release --no-build
dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

Expected:

- restore succeeds
- build succeeds
- tests succeed
- EF reports no pending model changes unless a requester migration was explicitly approved and applied

## Migration Approval Gate

Phase 11 requires separate requester storage, but migration creation is not automatic.

Before creating a migration, stop and get explicit approval for:

- migration name: `AddTripRequesterEmployee`
- table: `Trips`
- nullable column: `RequesterEmployeeId`
- FK/index details
- existing data impact
- backfill rule: existing rows keep null requester metadata

Do not invent historical requester values for existing trips.

## Manual API Smoke

### Employee

1. Login as a normal employee.
2. `GET /api/trips`
   - expect only the employee's own trips.
3. `GET /api/trips/{ownTripId}`
   - expect success.
4. `GET /api/trips/{otherEmployeeTripId}`
   - expect `403` structured error.
5. `POST /api/trips` with `requestedByEmployeeId` equal to self.
   - expect `201 Created` if existing trip validation passes.
6. `POST /api/trips` with `requestedByEmployeeId` equal to another employee.
   - expect `403`; no trip is created.
7. `DELETE /api/trips/{ownTripId}`
   - expect current hard-delete success.
8. `DELETE /api/trips/{otherEmployeeTripId}`
   - expect `403`.

### Manager

1. Login as a manager.
2. `GET /api/trips`
   - expect manager-owned plus active direct/indirect team trips only.
3. `GET /api/trips?travelerEmployeeId={activeReportId}`
   - expect only that report's trips.
4. `GET /api/trips?travelerEmployeeId={peerOrUnrelatedId}`
   - expect an empty paged response, not `403` or target details.
5. `GET /api/trips/{teamTripId}`
   - expect success.
6. `GET /api/trips/{peerTripId}`
   - expect `403`.
7. `POST /api/trips` for self or active direct/indirect report.
   - expect `201 Created` if existing trip validation passes.
8. `POST /api/trips` for peer, unrelated employee, soft-deleted report, or terminated report.
   - expect rejection and no trip creation.
9. `DELETE /api/trips/{teamTripId}`
   - expect current hard-delete success.
10. `DELETE /api/trips/{peerTripId}`
   - expect `403`.

### HR Administrator and System Administrator

1. Login as HR administrator.
2. Verify `GET /api/trips` returns organization-wide trips.
3. Verify detail succeeds for any existing trip.
4. Verify create succeeds for any eligible active employee.
5. Verify create fails for missing, inactive, soft-deleted, or terminated employees using existing trip validation conventions.
6. Verify delete succeeds for any existing trip.
7. Repeat the same checks as system administrator.

### Missing vs Out-of-Scope

1. Use a random missing trip ID.
   - detail/delete should return existing `404 NOT_FOUND`.
2. Use an existing trip ID outside the requester's scope.
   - detail/delete should return `403 FORBIDDEN`.

### Compatibility

Confirm Phase 11 did not change:

- auth login response
- `/api/auth/me`
- cookies
- claims
- employee endpoints
- vacation endpoints
- compensation, document, attendance, dashboard, audit, bootstrap, and Swagger behavior

## Notes for Local SQL Server

Local validation may use a machine-specific connection string through environment variables, user secrets, or uncommitted local config. Do not commit a shared default that points to a personal test database.
