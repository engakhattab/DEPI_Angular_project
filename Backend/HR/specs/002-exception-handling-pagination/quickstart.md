# Quickstart: Phase 1 — Global Exception Handling & Pagination Infrastructure

## Prerequisites

- Phase 0 is complete (5-project layered architecture in place).
- Domain exceptions (`NotFoundException`, `ConflictException`, `BusinessRuleException`) exist in `HR.Domain/Exceptions/`.

## What This Phase Adds

1. **`GlobalExceptionMiddleware`** in `HR.API/Middleware/` — catches all unhandled exceptions and returns structured JSON errors.
2. **`PagedList<T>`** in `HR.Shared/Pagination/` — reusable pagination wrapper for `IQueryable<T>` sources.

## Verify After Implementation

### 1. Build

```bash
dotnet build
```

Verify zero compilation errors.

### 2. Test Error Handling

Start the API and trigger error scenarios:

- **404**: Request a non-existent resource (e.g. `GET /api/employees/00000000-0000-0000-0000-000000000000`). Expect:
  ```json
  { "code": "NOT_FOUND", "message": "..." }
  ```

- **500**: Temporarily introduce a `throw new Exception("test")` in any controller action. Expect:
  ```json
  { "code": "SERVER_ERROR", "message": "An unexpected error occurred." }
  ```
  Verify the actual exception details appear in the server logs, NOT in the response body.

### 3. Verify Existing Endpoints

Navigate to Swagger UI and confirm all existing endpoints return their normal responses (2xx) without any change in shape.

### 4. PagedList<T>

`PagedList<T>` is not wired into any endpoint in this phase. It will be used starting in Phase 3. You can verify it compiles and is accessible from `HR.Application` by checking the project reference to `HR.Shared`.
