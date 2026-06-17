# Phase 13 Quickstart: Swagger/OpenAPI Response Documentation Pass

Use this quickstart to validate the Phase 13 implementation after tasks are generated and completed.

## 1. Restore and Build

Run from the repository backend root:

```powershell
dotnet restore .\HR.slnx
dotnet build .\HR.slnx -c Release --no-restore
```

Expected result:

- Restore succeeds.
- Release build succeeds with no errors.

## 2. Run Automated Tests

```powershell
dotnet test .\HR.slnx -c Release --no-build
```

Expected result:

- Existing test suite passes.
- No Phase 13 change breaks authorization, business-rule, authentication, or controller behavior tests.

## 3. Start the API Locally

```powershell
dotnet run --project .\HR.API\HR.API.csproj
```

Open Swagger at the HTTPS URL printed by the application. Existing local documentation commonly uses:

```text
https://localhost:7162/swagger
```

Expected result:

- Swagger UI loads.
- No existing endpoint group disappears.

## 4. Review OpenAPI JSON

Open:

```text
https://localhost:7162/swagger/v1/swagger.json
```

If the local HTTPS port differs, use the port printed by `dotnet run`.

Expected result:

- All required endpoint routes are present.
- Expected response status codes appear under each reviewed operation.
- No bearer-token/JWT security scheme is introduced.

## 5. Manual Swagger Checks

Review these minimum checks in Swagger UI:

- `POST /api/attendance/clock-in` documents `201 Created`.
- `POST /api/auth/login` documents success and invalid-credential outcomes.
- `POST /api/auth/logout` documents `204 No Content`.
- Employee create/update/delete/role endpoints document applicable 401/403/404/409/422 outcomes.
- Vacation request create/review/delete endpoints document applicable 401/403/404/409/422 outcomes.
- Trip create/delete/detail endpoints document applicable 401/403/404/409/422 outcomes.
- Employee document upload documents multipart upload and `413 Payload Too Large` where current behavior supports it.
- Employee document download is documented as a file/binary response.
- Paged list endpoints document paged list response shapes.

Expected result:

- Expected responses no longer appear as `Undocumented`.
- Empty/no-body responses do not imply JSON bodies.
- Structured service errors are documented as `{ code, message }`.

## 6. Confirm Behavior-Neutral Scope

Review the final diff:

```powershell
git diff --check
git diff -- HR.API HR.Application HR.Domain HR.Infrastructure HR.Shared HR.Tests specs\013-swagger-response-docs AGENTS.md
```

Expected result:

- No route names changed.
- No request or runtime response DTO shapes changed.
- No service, repository, domain, migration, authentication, authorization, or database behavior changed.
- Any runtime mismatch found during Swagger review is recorded as follow-up work instead of being fixed in Phase 13.

## 7. Record Completion Evidence

Create or update the Phase 13 implementation summary during implementation with:

- Validation command results.
- Swagger UI/OpenAPI JSON review notes.
- Endpoint groups reviewed.
- Any follow-up findings.
- Confirmation that Phase 13 changed response documentation only.
