# HR System Client Installation Guide

## 1. Introduction

This guide explains how to install and run the HR backend API on a new client machine using a client-owned SQL Server database.

By the end of this guide, you should have:

- the backend API running
- your own local SQL Server database
- Entity Framework Core migrations applied
- an initial System Administrator created from secure configuration
- Swagger/API ready for testing

The commands in this guide are written for Windows PowerShell and should be run from the project root folder unless another folder is explicitly mentioned.

## 2. System Requirements

| Requirement | Notes |
|-------------|-------|
| Windows | Recommended for this setup because the current connection examples use SQL Server Express and Windows authentication. |
| .NET SDK | The projects target `net8.0`. Install .NET SDK 8.x or a newer SDK that can build `net8.0` projects. |
| SQL Server | SQL Server Developer, SQL Server Express, or a reachable SQL Server instance. |
| SQL Server Management Studio | Recommended for creating/checking databases and viewing tables. |
| Git | Required only if cloning from a repository. |
| Visual Studio or VS Code | Optional, but useful for editing configuration and running/debugging the API. |
| Postman or Swagger | Optional. Swagger is enabled by the API and is usually enough for basic API testing. |
| EF Core CLI | Required for migration commands. If missing, install with `dotnet tool install --global dotnet-ef --version 8.0.20`. |

Main package versions used by the project include:

- Entity Framework Core `8.0.20`
- ASP.NET Core Identity EF stores `8.0.20`
- Swashbuckle/Swagger `6.6.2`
- xUnit `2.5.3`

## 3. Project Folder Overview

| Folder | Purpose |
|--------|---------|
| `HR.API` | ASP.NET Core Web API host. Contains controllers, `Program.cs`, authentication/cookie setup, Swagger, CORS, and app configuration files. |
| `HR.Application` | Application contracts and DTOs. Contains service interfaces and request/response models without infrastructure implementation details. |
| `HR.Infrastructure` | Database, EF Core, Identity stores, repositories, service implementations, document storage, audit writer, migrations, and dependency injection setup. |
| `HR.Domain` | Core domain entities and enums such as employees, departments, vacations, trips, roles, attendance, compensation, documents, and audit logs. |
| `HR.Shared` | Shared utilities such as result/error models, pagination, and serialization helpers. |
| `HR.Tests` | xUnit test project with integration, service, controller, configuration, model, and boundary tests. |

The solution file is:

```powershell
.\HR.slnx
```

## 4. Getting the Project Files

You can receive the project in either of these ways.

### Option A: ZIP File

1. Receive the ZIP file from the project owner.
2. Extract it to a folder such as:

   ```text
   C:\Projects\HR
   ```

3. Open PowerShell in the extracted project root folder containing `HR.slnx`.

### Option B: Git Clone

```powershell
git clone <REPOSITORY_URL>
cd <PROJECT_FOLDER>\Backend\HR
```

Replace `<REPOSITORY_URL>` and `<PROJECT_FOLDER>` with the actual repository URL and local folder name.

## 5. Opening the Project

Open the folder that contains:

```text
HR.slnx
HR.API
HR.Application
HR.Infrastructure
HR.Domain
HR.Shared
HR.Tests
```

You can open it with:

- Visual Studio: open `HR.slnx`
- VS Code: open the project root folder
- Terminal: run commands from the folder containing `HR.slnx`

Example command location:

```powershell
cd D:\YourPath\Backend\HR
dotnet restore .\HR.slnx
```

## 6. SQL Server Database Setup

Each client should use their own database. Do not share a development database between clients.

### Create a Database

Using SQL Server Management Studio, connect to your SQL Server instance and create a database, for example:

```sql
CREATE DATABASE HrSystemDb_Client;
```

**Note for Phase 12 manual retest**: Use the disposable database name `HrSystemDb_Phase12LifecycleTest` for local validation. This database is created fresh, tested, and discarded. It avoids mutating your client project database.

You may choose any database name, such as:

```text
HrSystemDb
HrSystemDb_Client
CompanyHrDb
```

### SQL Server Connection Examples

Windows authentication example:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=<SERVER_NAME>\\SQLEXPRESS;Database=<DATABASE_NAME>;Trusted_Connection=True;TrustServerCertificate=True"
}
```

LocalDB example:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=<DATABASE_NAME>;Trusted_Connection=True;TrustServerCertificate=True"
}
```

SQL authentication example:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=<SERVER_NAME>;Database=<DATABASE_NAME>;User Id=<USERNAME>;Password=<PASSWORD>;TrustServerCertificate=True"
}
```

Do not commit real production passwords to source control. Prefer environment variables, user secrets, or deployment secrets.

The current checked-in development example points to:

```text
Server=DESKTOP-5IHGJ9F\SQLEXPRESS;Database=HrSystemDb;Trusted_Connection=True;TrustServerCertificate=True
```

Change this for each client machine.

## 7. Application Configuration

Review these configuration areas before running the API.

Configuration files are in:

```text
HR.API\appsettings.json
HR.API\appsettings.Development.json
```

Environment variables override JSON configuration in normal ASP.NET Core configuration order.

### Database Connection

Set:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<SERVER_NAME>\\SQLEXPRESS;Database=<DATABASE_NAME>;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

PowerShell environment variable example:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=<SERVER_NAME>\SQLEXPRESS;Database=<DATABASE_NAME>;Trusted_Connection=True;TrustServerCertificate=True"
```

### Initial Admin Bootstrap

The system creates the first `SystemAdministrator` through startup bootstrap when no active System Administrator exists.

The bootstrap path is:

```text
InitialAdminBootstrap:Mode = CreateInitialAdmin
```

The bootstrap creates:

- an ASP.NET Identity user
- a linked `Employee`
- `Employee.Role = SystemAdministrator`
- a `SYSTEM_BOOTSTRAP` audit entry

Important behavior:

- Bootstrap is a startup path, not a public setup endpoint.
- If an active System Administrator already exists, bootstrap is a no-op.
- Bootstrap does not fall back to the first active employee.
- Bootstrap fails clearly if required values are missing or invalid.
- Temporary passwords must not be committed to source control.
- Disable bootstrap after the first admin is created and verified.

The current committed config has bootstrap disabled:

```json
{
  "InitialAdminBootstrap": {
    "Enabled": false,
    "Mode": "CreateInitialAdmin",
    "ForcePasswordChange": true
  }
}
```

Required first-run values:

- `Enabled`
- `Mode`
- `EmployeeNumber`
- `Email`
- `FullName`
- `DepartmentId`
- `TemporaryPassword`
- `ForcePasswordChange`

`ForcePasswordChange` is currently accepted in configuration. First-login password-change enforcement should be verified from the current implementation before relying on it as a production control.

Safe placeholder example:

```json
{
  "InitialAdminBootstrap": {
    "Enabled": true,
    "Mode": "CreateInitialAdmin",
    "EmployeeNumber": "EMP001",
    "Email": "admin@client.com",
    "FullName": "Client System Admin",
    "DepartmentId": "<EXISTING_DEPARTMENT_GUID>",
    "TemporaryPassword": "<TEMPORARY_STRONG_PASSWORD>",
    "ForcePasswordChange": true
  }
}
```

PowerShell environment variable example:

```powershell
$env:InitialAdminBootstrap__Enabled="true"
$env:InitialAdminBootstrap__Mode="CreateInitialAdmin"
$env:InitialAdminBootstrap__EmployeeNumber="EMP001"
$env:InitialAdminBootstrap__Email="admin@client.com"
$env:InitialAdminBootstrap__FullName="Client System Admin"
$env:InitialAdminBootstrap__DepartmentId="<EXISTING_DEPARTMENT_GUID>"
$env:InitialAdminBootstrap__TemporaryPassword="<TEMPORARY_STRONG_PASSWORD>"
$env:InitialAdminBootstrap__ForcePasswordChange="true"
```

Password policy from the current Identity setup:

- minimum length: 8
- requires digit
- requires uppercase letter
- requires lowercase letter
- non-alphanumeric character is not required by current code, but a strong password is still recommended

### Endpoint Permission Updates (Phases 8-11)

The following authorization scope hardening has been applied:

- **Employee endpoints**: Employee role receives `403` for list, create, update, delete. Manager role sees only team-scoped employees. HR/System roles have org-wide access. Only SystemAdministrator can assign roles (HRAdministrator receives `403`).
- **Vacation endpoints**: Employee role sees/creates own requests only. Manager role sees/reviews team requests only (self-review blocked). HR/System roles have org-wide access.
- **Trip endpoints**: Employee role accesses own trips only. Manager role accesses own + active team trips. HR/System roles have org-wide access. Requester metadata is stored separately from traveler data.
- **Last-active-SystemAdministrator guard**: The last remaining active SystemAdministrator cannot delete, terminate, demote, or change their own role assignment.
- For detailed role-specific scenarios, see the [API Lifecycle Testing Guide](../API_LIFECYCLE_TESTING_GUIDE.md).

### Required Department Before First Bootstrap

The initial admin requires an existing department ID. The migrations create the `Departments` table but do not automatically seed a department.

For a new empty database, after applying migrations and before first API startup with bootstrap enabled, create one department and copy its ID:

```sql
DECLARE @DepartmentId uniqueidentifier = NEWID();

INSERT INTO Departments (Id, Name)
VALUES (@DepartmentId, N'Administration');

SELECT @DepartmentId AS DepartmentId;
```

Use the returned `DepartmentId` in:

```text
InitialAdminBootstrap:DepartmentId
```

### Business Timezone / Attendance

Attendance timestamps are stored in UTC. The attendance business date is derived from a configured named timezone.

Current default:

```json
{
  "BusinessSettings": {
    "TimeZoneId": "Africa/Cairo"
  }
}
```

Examples:

| Region | TimeZoneId |
|--------|------------|
| Egypt | `Africa/Cairo` |
| Saudi Arabia | `Asia/Riyadh` |
| UAE | `Asia/Dubai` |
| United Kingdom | `Europe/London` |
| Germany/France | `Europe/Berlin` |

PowerShell example:

```powershell
$env:BusinessSettings__TimeZoneId="Africa/Cairo"
```

Important:

- Do not rely on browser/client-provided dates for attendance.
- Do not rely on unnamed server local time.
- If the timezone is missing or invalid, startup fails with a clear configuration error.

### Document Storage

Employee document files are stored on local backend-managed storage. File metadata is stored in the database; file binary content is not stored inside business records.

Current default:

```json
{
  "DocumentStorage": {
    "RootPath": "App_Data/EmployeeDocuments",
    "AllowedExtensions": [ ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" ],
    "MaxFileSizeBytes": 10485760
  }
}
```

Notes:

- `RootPath` is relative to the API runtime base path unless an absolute path is supplied.
- The configured root must not be under a public static folder such as `wwwroot`.
- Downloads go through authorized API endpoints.
- Back up this folder together with the database.
- Ensure the API process account has read/write/delete permission on the folder.

PowerShell examples:

```powershell
$env:DocumentStorage__RootPath="D:\HRSystem\EmployeeDocuments"
$env:DocumentStorage__MaxFileSizeBytes="10485760"
$env:DocumentStorage__AllowedExtensions__0=".pdf"
$env:DocumentStorage__AllowedExtensions__1=".jpg"
$env:DocumentStorage__AllowedExtensions__2=".jpeg"
$env:DocumentStorage__AllowedExtensions__3=".png"
$env:DocumentStorage__AllowedExtensions__4=".doc"
$env:DocumentStorage__AllowedExtensions__5=".docx"
```

### CORS / Frontend URL

The API currently allows these frontend origins in `Program.cs`:

```text
http://localhost:4200
https://localhost:4200
```

If the Angular/frontend site is hosted on another domain or port, verify and update the allowed CORS origins before deployment.

## 8. Restore Packages

Run:

```powershell
dotnet restore .\HR.slnx
```

If restore fails, verify:

- .NET SDK is installed
- internet access is available for NuGet restore
- you are running the command from the folder containing `HR.slnx`

## 9. Build the Project

Run:

```powershell
dotnet build .\HR.slnx -c Release
```

Build must pass before applying migrations or running the API.

## 10. Apply Database Migrations

Migrations are in:

```text
HR.Infrastructure\Data\Migrations
```

Current migration names (including Phase 10 and Phase 11):

```text
20251114215718_InitialCreate
20260603014628_Phase5HrBusinessRules
20260606235241_Phase7AdvancedHrFeatures
20260615170903_AddVacationRequestCreatedByEmployee
20260615212225_AddTripRequesterEmployee
```

Apply migrations to create/update the client database:

```powershell
dotnet ef database update --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

This command uses the configured `ConnectionStrings:DefaultConnection`.

Important:

- Do not delete or edit migration files.
- Each client should use their own database.
- Back up an existing production database before applying migrations.
- For a new database, this command creates the schema.
- For an existing database, this command applies missing migrations.

Check pending model changes:

```powershell
dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

Expected after migrations are current:

```text
No changes have been made to the model since the last migration.
```

List migrations:

```powershell
dotnet ef migrations list --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
```

## 11. Run the API

Run:

```powershell
dotnet run --project .\HR.API\HR.API.csproj
```

The launch settings define these development URLs:

```text
https://localhost:7162
http://localhost:5098
```

Swagger is enabled. Open:

```text
https://localhost:7162/swagger
```

or use the exact URL shown in the console output.

Authentication cookies are configured with `SecurePolicy = Always`, so HTTPS is recommended for browser/Postman testing.

## 12. First Login / Initial Admin Setup

For a new empty client database:

1. Configure the database connection string.
2. Apply migrations.
3. Insert an initial department and copy its ID:

   ```sql
   DECLARE @DepartmentId uniqueidentifier = NEWID();

   INSERT INTO Departments (Id, Name)
   VALUES (@DepartmentId, N'Administration');

   SELECT @DepartmentId AS DepartmentId;
   ```

4. Configure `InitialAdminBootstrap` with secure values.
5. Run the API.
6. The system creates the initial System Administrator during startup.
7. Log in using the configured admin email and temporary password.
8. Verify admin access.
9. Disable bootstrap:

   ```powershell
   $env:InitialAdminBootstrap__Enabled="false"
   ```

   Or set `"Enabled": false` in the appropriate deployment configuration.

10. Restart the app if needed.

Login endpoint:

```http
POST /api/Auth/login
```

Example body:

```json
{
  "identifier": "admin@client.com",
  "password": "<TEMPORARY_STRONG_PASSWORD>"
}
```

Verify current user:

```http
GET /api/Auth/me
```

If bootstrap fails, check:

- database connection string
- migrations were applied
- `DepartmentId` exists
- `EmployeeNumber` is unique
- `Email` is unique
- password satisfies the current Identity policy
- `InitialAdminBootstrap:Enabled` is true for first run
- `InitialAdminBootstrap:Mode` is `CreateInitialAdmin`
- API logs for the exact configuration error

## 13. Verifying the Installation

Use this checklist after setup:

- API starts without errors.
- Swagger opens.
- Database tables exist.
- Migrations are listed in `__EFMigrationsHistory`.
- Initial System Administrator can log in.
- `/api/Auth/me` returns the current user and role.
- Bootstrap audit row exists if you inspect audit logs or the database.
- Employee endpoints are protected.
- Document storage folder exists and is not public.
- Attendance timezone is configured correctly.
- `InitialAdminBootstrap:Enabled` is disabled after setup.

Useful SQL check:

```sql
SELECT MigrationId
FROM __EFMigrationsHistory
ORDER BY MigrationId;

SELECT Id, EmployeeNumber, Email, Role, Status, IsDeleted
FROM Employees
WHERE Role = N'SystemAdministrator';
```

## 14. Running Tests

Run all tests:

```powershell
dotnet test .\HR.slnx -c Release
```

If you already built the solution:

```powershell
dotnet test .\HR.slnx -c Release --no-build
```

Passing tests confirm that the project builds and the tested business rules are healthy in the local development environment.

## 15. Common Commands

| Action | Command |
|--------|---------|
| Restore packages | `dotnet restore .\HR.slnx` |
| Build Release | `dotnet build .\HR.slnx -c Release` |
| Run tests | `dotnet test .\HR.slnx -c Release` |
| Run tests without rebuilding | `dotnet test .\HR.slnx -c Release --no-build` |
| Apply migrations | `dotnet ef database update --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj` |
| List migrations | `dotnet ef migrations list --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj` |
| Check pending model changes | `dotnet ef migrations has-pending-model-changes --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj` |
| Run API | `dotnet run --project .\HR.API\HR.API.csproj` |
| Run API on a specific URL | `dotnet run --project .\HR.API\HR.API.csproj --urls https://localhost:7162` |

## 16. Troubleshooting

### Build Fails Because DLL Is Locked

This usually means the API is still running.

Find the process:

```powershell
tasklist | findstr HR.API
tasklist | findstr dotnet
```

Stop the API process:

```powershell
taskkill /IM HR.API.exe /F
```

If the app is running through `dotnet run`, you may need:

```powershell
taskkill /IM dotnet.exe /F
```

Warning: killing all `dotnet.exe` processes may stop other .NET applications on the machine.

### Database Connection Fails

Common causes:

- wrong SQL Server name
- SQL Server service is not running
- database does not exist
- wrong username/password
- Windows account does not have access
- SQL authentication is disabled
- typo in the connection string
- missing `TrustServerCertificate=True` for local development certificates

Check the connection in SQL Server Management Studio first.

### Migration Fails

Check:

- you are running from the folder containing `HR.slnx`
- the connection string points to the correct database
- SQL Server is reachable
- the database user has schema change permissions
- EF CLI is installed

Install EF CLI if needed:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.20
```

Or update it:

```powershell
dotnet tool update --global dotnet-ef --version 8.0.20
```

### Login Fails

Check:

- initial admin bootstrap ran successfully
- `InitialAdminBootstrap:Enabled` was not disabled before the first admin was created
- temporary password is correct
- password satisfies the Identity policy
- admin email/employee number are unique
- the admin employee has `Role = SystemAdministrator`
- the employee is active and not soft-deleted

### Swagger Does Not Open

Check:

- the API is running
- use the URL shown in console output
- try `https://localhost:7162/swagger`
- try `http://localhost:5098/swagger`
- check firewall or port conflicts
- check the terminal for startup exceptions

### File Upload Fails

Check:

- file extension is allowed
- file size is under `DocumentStorage:MaxFileSizeBytes`
- storage folder exists or can be created
- API process has folder permissions
- storage root is not inside `wwwroot`
- document actions are being performed by an authorized HR/System admin user

## 17. Deployment Notes

For client or production deployment:

- use environment variables or a secret store for passwords and connection strings
- do not commit production secrets
- configure a separate database per client
- configure business timezone per client
- configure document storage path per client
- apply migrations before production use
- back up the database before updates
- back up uploaded employee documents
- disable bootstrap after the initial System Administrator is created
- use HTTPS
- restrict server and database access

## 18. Security Notes

- Do not expose setup/bootstrap secrets.
- Change the temporary admin password immediately after first login.
- Keep bootstrap disabled after setup.
- Restrict document storage folder permissions.
- Do not share production connection strings.
- Use HTTPS in production.
- Back up both the database and uploaded documents.
- Keep SQL Server credentials outside source control.
- Review CORS origins before exposing the API outside local development.

## 19. Upgrade / Update Process

For future updates:

1. Back up the database.
2. Back up uploaded documents.
3. Stop the running API.
4. Pull or copy the new project version.
5. Restore packages:

   ```powershell
   dotnet restore .\HR.slnx
   ```

6. Build:

   ```powershell
   dotnet build .\HR.slnx -c Release
   ```

7. Apply migrations:

   ```powershell
   dotnet ef database update --project .\HR.Infrastructure\HR.Infrastructure.csproj --startup-project .\HR.API\HR.API.csproj
   ```

8. Run tests if available:

   ```powershell
   dotnet test .\HR.slnx -c Release --no-build
   ```

9. Start the API.
10. Verify login and core endpoints.
11. Confirm document upload/download still works.
12. Confirm audit logs and dashboard work.

## 20. Final Setup Checklist

- [ ] .NET SDK installed
- [ ] SQL Server installed and running
- [ ] SQL Server Management Studio installed or equivalent DB tool available
- [ ] project files copied or cloned
- [ ] project opened from folder containing `HR.slnx`
- [ ] connection string configured
- [ ] client database created
- [ ] business timezone configured
- [ ] document storage configured
- [ ] document storage folder permissions checked
- [ ] packages restored
- [ ] project built successfully
- [ ] migrations applied
- [ ] initial department created
- [ ] initial admin bootstrap configured
- [ ] API started
- [ ] Swagger opened
- [ ] initial admin logged in
- [ ] admin role verified
- [ ] bootstrap disabled after setup
- [ ] tests passed if run locally
- [ ] backup plan confirmed for database
- [ ] backup plan confirmed for uploaded documents
