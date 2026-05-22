# API Contracts: Phase 1 — Global Exception Handling & Pagination Infrastructure

## Error Response Contract

This phase introduces a **cross-cutting error contract** that applies to ALL existing and future endpoints. No new endpoints are added.

### Error Shape (all error responses)

```json
{
  "code": "<ERROR_CODE>",
  "message": "<human-readable description>"
}
```

**Content-Type**: `application/json`

### Error Code Mapping

| Error Code       | HTTP Status | When Used                              |
|------------------|-------------|----------------------------------------|
| `NOT_FOUND`      | 404         | Requested resource does not exist      |
| `CONFLICT`       | 409         | Operation conflicts with existing data |
| `BUSINESS_RULE`  | 422         | Domain invariant violated              |
| `SERVER_ERROR`   | 500         | Unhandled/unexpected server exception  |

### Important Behaviors

- The `message` field for `SERVER_ERROR` is always the generic string `"An unexpected error occurred."` — actual exception details are logged server-side only.
- The `message` field for domain exceptions (`NOT_FOUND`, `CONFLICT`, `BUSINESS_RULE`) contains the exception's message string.
- Successful responses (2xx/3xx) are not modified by the error middleware.

## Pagination Contract (infrastructure only — no endpoints in this phase)

`PagedList<T>` will produce the following JSON shape when serialized by controllers in Phase 3+:

```json
{
  "items": [ ... ],
  "totalCount": 50,
  "page": 2,
  "pageSize": 10,
  "totalPages": 5,
  "hasNext": true,
  "hasPrevious": true
}
```

This contract is documented here for forward reference but is not exposed by any endpoint in Phase 1.
