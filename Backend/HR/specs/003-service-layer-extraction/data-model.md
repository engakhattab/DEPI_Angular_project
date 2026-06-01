# Data Model: Phase 3 - Service Layer Extraction

**Status**: No Persistent Schema Changes

## Overview

Phase 3 reorganizes application behavior without changing persisted data. Existing entities, relationships, EF Core configuration, and migrations remain unchanged.

## Existing Persistent Entities

### Department

- `Id`: unique department identifier
- `Name`: required unique department name
- `Employees`: employees currently assigned to the department

### Employee

- `Id`: unique employee identifier
- `EmployeeNumber`: required unique employee number
- `FullName`, `Email`, profile fields, and employment `Status`
- `DepartmentId`: required department relationship
- `ManagerId`: optional self-referencing manager relationship
- `ApplicationUserId`: required Identity user relationship

### VacationRequest

- `Id`: unique request identifier
- `EmployeeId`: required employee relationship
- `StartDate`, `EndDate`, `Reason`, `Status`
- `CreatedAt`, optional `UpdatedAt`

### Trip

- `Id`: unique trip identifier
- `ReferenceName`, `Project`, `Route`, `TripType`, `TripDate`
- `TripCode`, `RequestCode`: generated unique identifiers
- `CreatedAt`

## Existing Relationships

- A department has many employees.
- An employee belongs to one department.
- An employee may have one manager and many direct reports.
- An employee maps one-to-one to an Identity user.
- A vacation request belongs to one employee.
- Trips remain standalone records in Phase 3.

## Application Result Shapes

### Paginated Result

List operations return:

| Field | Type | Meaning |
|-------|------|---------|
| `items` | array | Records for the requested page |
| `totalCount` | integer | Matching records across all pages |
| `page` | integer | Normalized one-based page number |
| `pageSize` | integer | Normalized records-per-page value |
| `totalPages` | integer | Number of pages for the matching result set |
| `hasNext` | boolean | Whether a later page exists |
| `hasPrevious` | boolean | Whether an earlier page exists |

Pagination normalization:

- `page <= 0` becomes `1`
- `pageSize <= 0` becomes `25`
- `pageSize > 100` becomes `100`

### Structured Error

Expected service failures return:

| Field | Type | Meaning |
|-------|------|---------|
| `code` | string | Stable machine-readable failure category |
| `message` | string | Human-readable explanation |

## Validation Rules Preserved in Phase 3

- Department names remain unique.
- Departments with assigned employees cannot be deleted.
- Vacation end dates cannot precede start dates.
- Vacation requests require an existing employee.
- Employee numbers remain unique.
- Employee creation and update require an existing department.
- Optional employee managers must exist.
- An employee cannot manage themselves.
- Employee creation and deletion preserve transactional behavior with Identity.

## Explicitly Deferred Model Changes

The following are not part of Phase 3:

- Repository interfaces.
- Per-entity EF configuration classes.
- Soft-delete fields.
- Vacation balance fields.
- Vacation review audit fields.
- Trip-to-employee relationships.
- New entities or migrations.
