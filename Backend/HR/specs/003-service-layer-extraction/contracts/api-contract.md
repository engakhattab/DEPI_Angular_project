# API Contract: Phase 3 - Service Layer Extraction

## Scope

All existing routes, HTTP verbs, authentication requirements, single-resource DTOs, and write-operation DTOs remain unchanged. List endpoints intentionally change from raw arrays to paginated result envelopes.

## Authentication

- All listed endpoints require the existing secure session cookie.
- Unauthenticated requests return JSON `401`.
- Forbidden requests return JSON `403`.
- `POST /api/auth/login`, `POST /api/auth/logout`, and `GET /api/auth/me` remain unchanged.

## Pagination Envelope

```json
{
  "items": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 25,
  "totalPages": 0,
  "hasNext": false,
  "hasPrevious": false
}
```

Pagination query parameters:

| Parameter | Default | Normalization |
|-----------|---------|---------------|
| `page` | `1` | Values less than or equal to `0` become `1` |
| `pageSize` | `25` | Values less than or equal to `0` become `25`; values greater than `100` become `100` |

## List Endpoints

### Departments

`GET /api/departments?page=1&pageSize=25`

- Returns `PagedList<DepartmentResponse>`.
- Items remain ordered by department name ascending.

### Vacation Requests

`GET /api/vacationrequests?status=&employeeId=&page=1&pageSize=25`

- Returns `PagedList<VacationRequestResponse>`.
- Existing optional `status` and `employeeId` filters remain supported.
- Items remain ordered by creation time descending.

### Trips

`GET /api/trips?page=1&pageSize=25`

- Returns `PagedList<TripResponse>`.
- Items remain ordered by creation time descending.

### Employees

`GET /api/employees?status=&page=1&pageSize=25`

- Returns `PagedList<EmployeeResponse>`.
- Existing optional `status` filter remains supported.
- Items remain ordered by employee number ascending.

## Existing Non-List Endpoints

The following routes keep their existing request and success-response shapes:

```text
GET    /api/departments/{id}
POST   /api/departments
PUT    /api/departments/{id}
DELETE /api/departments/{id}

GET    /api/vacationrequests/{id}
POST   /api/vacationrequests
PUT    /api/vacationrequests/{id}/status
DELETE /api/vacationrequests/{id}

GET    /api/trips/{id}
POST   /api/trips
DELETE /api/trips/{id}

GET    /api/employees/{id}
POST   /api/employees
PUT    /api/employees/{id}
DELETE /api/employees/{id}
```

## Structured Error Envelope

Expected service failures return:

```json
{
  "code": "NOT_FOUND",
  "message": "Human-readable explanation."
}
```

Status mapping:

| Error Type | HTTP Status |
|------------|-------------|
| `NotFound` | `404` |
| `Conflict` | `409` |
| `Validation` | `400` |
| `BusinessRule` | `422` |
| `Unauthorized` | `401` |
| `Forbidden` | `403` |
| `Internal` | `500` |

Unexpected exceptions continue to use the existing middleware contract:

```json
{
  "code": "SERVER_ERROR",
  "message": "An unexpected error occurred."
}
```
