# Quickstart: Phase 3 - Service Layer Extraction

## Purpose

Use this guide to validate the Phase 3 implementation incrementally. Do not wait until all controllers have been extracted before checking behavior.

## Prerequisites

- Phases 0, 1, and 2 are complete.
- SQL Server is available through the existing `DefaultConnection`.
- The database contains a valid Identity user and employee record for login.

## Build

From the backend root:

```powershell
dotnet restore
dotnet build
```

## Run

```powershell
dotnet run --project .\HR.API\HR.API.csproj
```

Open Swagger at the URL printed by the application.

## Authentication Regression

1. Call a protected endpoint without a session cookie and confirm JSON `401`.
2. Call `POST /api/auth/login` with valid credentials.
3. Confirm subsequent protected requests use the session cookie.
4. Call `GET /api/auth/me`.
5. Call `POST /api/auth/logout`.

## Per-Feature Validation Order

### 1. Departments

- Browse `GET /api/departments?page=1&pageSize=25`.
- Check page normalization with `page=0`, `pageSize=0`, and `pageSize=101`.
- Create, update, retrieve, and delete a department.
- Confirm duplicate names and delete-with-employees return structured errors.

### 2. Vacation Requests

- Browse with pagination and existing `status` / `employeeId` filters.
- Create a valid request.
- Confirm invalid date ranges and unknown employees return structured errors.
- Update request status and delete a request.

### 3. Trips

- Browse paginated trips.
- Create a trip and confirm generated trip and request code shapes remain unchanged.
- Retrieve and delete the trip.
- Confirm unknown trips return structured errors.

### 4. Employees

- Browse paginated employees with and without the status filter.
- Create an employee with a supplied password and with a generated temporary password.
- Update profile data and email.
- Confirm duplicate employee number, unknown department, unknown manager, and self-manager errors remain structured.
- Delete an employee with related vacation requests or direct reports and confirm existing cleanup behavior.

## Final Structural Review

After all feature checks:

```powershell
rg -n "ApplicationDbContext|UserManager<" .\HR.API\Controllers
dotnet build
```

The search should find no direct controller dependency on `ApplicationDbContext` or `UserManager<ApplicationUser>`.

## Scope Reminder

Do not add repositories, schema migrations, new HR rules, frontend changes, or advanced HR features during Phase 3.
