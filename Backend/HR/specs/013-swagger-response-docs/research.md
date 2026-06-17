# Phase 13 Research: Swagger/OpenAPI Response Documentation Pass

## Decision: Use Current Controller Outcomes as Source of Truth

**Decision**: Derive response documentation from the current controller actions, `ServiceErrorMappingExtensions`, `GlobalExceptionMiddleware`, and existing response DTOs.

**Rationale**: Phase 13 is behavior-neutral. The implementation must document what the API already returns instead of introducing new statuses, changing error codes, or normalizing inconsistent behavior.

**Alternatives considered**:

- Redesign error/status behavior before documenting it: rejected because runtime behavior changes are out of scope.
- Document a desired future contract instead of current behavior: rejected because Swagger must match the deployed API clients call today.

## Decision: Prefer Explicit Controller Response Metadata

**Decision**: Add explicit response documentation to controller operations using existing ASP.NET Core/Swashbuckle-compatible metadata patterns, with success and error statuses listed per endpoint.

**Rationale**: Endpoint outcomes vary by controller, policy, service result, and file behavior. Explicit per-action metadata is simple to review and reduces the risk of a global rule documenting unsupported statuses.

**Alternatives considered**:

- Global Swagger operation filter for all endpoints: rejected as too broad for Phase 13 because it can imply statuses that specific endpoints do not support.
- Only update external Markdown docs: rejected because the known defect appears in Swagger/OpenAPI output itself.

## Decision: Document Structured Errors Without Normalizing Codes

**Decision**: Document the existing structured error payload shape as `{ code, message }` and preserve current compatibility codes such as `UNAUTHORIZED`, `FORBIDDEN`, `VALIDATION_ERROR`, `BUSINESS_RULE_VIOLATION`, and related service/middleware codes.

**Rationale**: The constitution explicitly allows existing compatibility codes until a separately approved compatibility phase. Phase 13 must improve discoverability without changing client-facing error semantics.

**Alternatives considered**:

- Rename error codes for consistency: rejected because it is a runtime compatibility change.
- Document errors as generic objects only: rejected because it does not help client integrators understand the actual shape.

## Decision: Treat File Upload and Download Responses Separately

**Decision**: Document document upload, list, download, and delete responses according to their current behavior, including multipart upload, file response, no-content deletion, and payload-too-large behavior.

**Rationale**: File endpoints cannot be accurately represented as normal JSON-only operations. The upload action currently has a size limit and explicit 413 response path, while download returns a file response on success.

**Alternatives considered**:

- Document all document endpoints as JSON: rejected because downloads return file content.
- Remove file-specific documentation from Phase 13: rejected because Employee Documents is one of the required endpoint groups.

## Decision: Keep Authentication Documentation Cookie-Based

**Decision**: Protected endpoints must document unauthenticated and forbidden outcomes in terms of existing cookie-based sessions and authorization policies, with no bearer-token authorization scheme introduced.

**Rationale**: The constitution requires cookie-based session authentication and explicitly forbids JWT. Phase 13 should not create a misleading Swagger security model.

**Alternatives considered**:

- Add bearer/JWT Swagger auth support: rejected because it conflicts with the project constitution and runtime behavior.
- Omit 401/403 documentation: rejected because role-scope behavior is a major part of Phases 8-11 and must be visible to testers.

## Decision: Validate With Build, Tests, Swagger UI, and OpenAPI Output

**Decision**: Completion requires `dotnet restore`, `dotnet build -c Release`, `dotnet test -c Release`, `git diff --check`, manual Swagger UI review, and OpenAPI JSON inspection for key statuses/routes.

**Rationale**: Build/tests guard against accidental code regressions, while Swagger UI/OpenAPI inspection verifies the actual documentation defect has been addressed.

**Alternatives considered**:

- Rely on build/tests only: rejected because tests may not validate rendered Swagger/OpenAPI metadata.
- Manual Swagger review only: rejected because behavior-neutral code changes still need normal project validation.
