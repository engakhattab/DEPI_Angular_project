# Quickstart: Phase 5 - HR Business Logic Improvements

Use this guide after implementation to validate Phase 5 behavior on an isolated database.

Record baseline failures and manual validation notes in the implementation report, handoff notes, or final Phase 5 completion summary. Do not use `tasks.md` as a runtime failure log.

## Prerequisites

- .NET 8 SDK
- SQL Server connection configured for `HR.API`
- EF Core CLI available through `dotnet ef`
- Phases 0 through 4 already complete

## Build and Test

```powershell
dotnet restore .\HR.slnx
dotnet build .\HR.slnx -c Release
dotnet test .\HR.slnx -c Release --no-build
```

## Migration Check

Generate exactly one Phase 5 migration after code changes:

```powershell
dotnet ef migrations add Phase5HrBusinessRules --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj --output-dir Data\Migrations
dotnet ef database update --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

Confirm existing migration files were not edited.

## Manual API Regression

Start the API on an isolated port:

```powershell
dotnet run --project .\HR.API\HR.API.csproj --urls "https://localhost:7085"
```

### 1. Authentication Revocation

1. Sign in as an active employee and confirm the session cookie works.
2. Terminate that employee through `PUT /api/employees/{id}`.
3. Retry `GET /api/auth/me` with the same cookie.
4. Expected: `401` JSON response without waiting for logout or cookie expiry.
5. Attempt a new login with the same employee credentials.
6. Expected: login is denied.

Repeat after soft-deleting an employee through `DELETE /api/employees/{id}`.

### 2. Vacation Submission Rules

1. Create an active employee with default vacation balance.
2. Submit a valid future Sunday-through-Thursday vacation request with at least three full working days of notice.
3. Expected: `201`, `Pending`, calculated `workingDayCount`.
4. Submit another pending request with an overlapping range.
5. Expected: `422` business-rule response.
6. Submit a request that overlaps only a rejected request.
7. Expected: normal validation continues; overlap alone does not reject it.
8. Submit a request for a suspended, terminated, or soft-deleted employee.
9. Expected: `422` business-rule response.
10. Submit a request in the past or inside the three-full-working-day notice window.
11. Expected: `422` business-rule response.

### 3. Vacation Review Rules

1. Log in as an employee who is not the requester.
2. Approve a pending request through `PUT /api/vacationrequests/{id}/status`.
3. Expected: status becomes `Approved`, reviewer fields are set, and employee balance is reduced by `workingDayCount`.
4. Repeat approval on the approved request.
5. Expected: same-status update succeeds as a no-op, reviewer/timestamp fields are not updated again, and balance is unchanged.
6. Reject the approved request as cancellation.
7. Expected: status becomes `Rejected`, reviewer fields update, and the same balance amount is restored once.
8. Attempt any transition from `Rejected`.
9. Expected: transition is rejected.
10. Attempt approval or rejection as the requester.
11. Expected: self-review is rejected.

### 4. Vacation Deletion Rules

1. Delete a pending vacation request.
2. Expected: `204`.
3. Delete an approved or rejected request.
4. Expected: `422`, and the request remains available for audit.

### 5. Employee Lifecycle Rules

1. Update an active employee to suspended.
2. Expected: success.
3. Update suspended back to active.
4. Expected: success.
5. Update active or suspended to terminated.
6. Expected: `terminatedAt` is set, pending vacation requests are rejected, and access is revoked.
7. Repeat the same terminated status update.
8. Expected: same-status update succeeds as a no-op without changing `terminatedAt` or repeating side effects.
9. Attempt to change the terminated employee back to active or suspended.
10. Expected: `422`.
11. Assign a manager chain that would point back to the employee.
12. Expected: `422`.
13. Assign a manager from another department.
14. Expected: success and server log warning.
15. Create or update another active employee with the same email.
16. Expected: `409`.
17. Soft-delete an employee.
18. Expected: employee is excluded from normal employee list/detail results, Identity user is retained, and login/session access is denied.

### 6. Trip Rules

1. Submit a trip for an active employee on a future Sunday-through-Thursday date.
2. Expected: `201` with requester fields.
3. Submit a trip for a missing, suspended, terminated, or soft-deleted employee.
4. Expected: `404` or `422` according to the failure type.
5. Submit a trip in the past.
6. Expected: `422`.
7. Submit a trip on Friday or Saturday.
8. Expected: `422`.

### 7. Department Counts

1. View departments after creating active employees.
2. Expected: `employeeCount` reflects assigned non-deleted employees.
3. Move an employee between departments.
4. Expected: both counts update.
5. Terminate an employee without soft deletion.
6. Expected: count remains because the profile is still visible.
7. Soft-delete an employee.
8. Expected: count decreases.

## Final Static Checks

```powershell
rg -n "ApplicationDbContext" .\HR.Infrastructure -g "*Service.cs"
rg -n "AddScoped<" .\HR.API\Program.cs
git diff --check
```

The service search should have no matches. `Program.cs` should not contain direct application/infrastructure `AddScoped<>` registrations.
