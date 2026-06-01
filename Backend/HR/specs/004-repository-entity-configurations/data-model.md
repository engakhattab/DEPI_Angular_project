# Data Model: Phase 4 - Repository Pattern and Entity Configurations

**Status**: No Persistent Schema Changes

## Overview

Phase 4 reorganizes persistence access and mapping declarations without changing stored data. Existing entities, fields, indexes, relationships, delete behavior, migrations, and DTOs remain unchanged.

## Existing Persistent Entities

### Department

| Field | Rule |
|-------|------|
| `Id` | Unique identifier |
| `Name` | Required, maximum 100 characters, unique |
| `Employees` | Collection of assigned employees |

### Employee

| Field | Rule |
|-------|------|
| `Id` | Unique identifier |
| `EmployeeNumber` | Required, maximum 20 characters, unique |
| `FullName` | Required, maximum 200 characters |
| `JobTitle` | Optional, maximum 150 characters |
| `Email` | Optional, maximum 256 characters |
| `PhoneNumber` | Optional, maximum 30 characters |
| `Status` | Stored as text, maximum 32 characters |
| `DepartmentId` | Required department relationship |
| `ManagerId` | Optional self-referencing manager relationship |
| `ApplicationUserId` | Required one-to-one Identity-user relationship |

### VacationRequest

| Field | Rule |
|-------|------|
| `Id` | Unique identifier |
| `EmployeeId` | Required employee relationship |
| `StartDate`, `EndDate` | Existing request date range |
| `Reason` | Required, maximum 500 characters |
| `Status` | Stored as text, maximum 32 characters |
| `CreatedAt` | Required |
| `UpdatedAt` | Optional |

### Trip

| Field | Rule |
|-------|------|
| `Id` | Unique identifier |
| `ReferenceName` | Required, maximum 200 characters |
| `Project` | Required, maximum 150 characters |
| `Route` | Required, maximum 150 characters |
| `TripType` | Required, maximum 50 characters |
| `TripDate` | Existing trip date |
| `TripCode` | Required, maximum 32 characters, unique |
| `RequestCode` | Required, maximum 32 characters, unique |
| `CreatedAt` | Required |

### ApplicationUser

`ApplicationUser` remains the existing Identity credential record. Identity's built-in mappings continue to come from the base context. Phase 4 preserves the existing one-to-one employee relationship and adds no custom Identity-user fields.

## Existing Relationships

| Relationship | Delete Behavior |
|--------------|-----------------|
| Department has many employees | Restrict department deletion while employees remain assigned |
| Employee belongs to one department | Restrict |
| Employee optionally has a manager and direct reports | Restrict |
| Employee belongs one-to-one to an Identity user | Cascade from Identity user to employee |
| Vacation request belongs to one employee | Cascade from employee to vacation requests |
| Trip | Standalone record |

## Mapping Ownership After Phase 4

| Entity | Configuration Declaration |
|--------|---------------------------|
| Department | `HR.Infrastructure/Data/Configurations/DepartmentConfiguration.cs` |
| Employee and its Identity-user relationship | `HR.Infrastructure/Data/Configurations/EmployeeConfiguration.cs` |
| Vacation request | `HR.Infrastructure/Data/Configurations/VacationRequestConfiguration.cs` |
| Trip | `HR.Infrastructure/Data/Configurations/TripConfiguration.cs` |
| Identity built-in schema | `IdentityDbContext<ApplicationUser>` base configuration |

## Persistence Boundaries

| Boundary | Responsibility |
|----------|----------------|
| Department repository | Department pages, lookup, uniqueness checks, assigned-employee detail loading, add, and remove |
| Employee repository | Employee pages, detail lookup, authentication profile lookup, existence checks, direct reports, add, and remove |
| Vacation-request repository | Filtered pages, detail lookup, employee-related lookup, add, remove, and range remove |
| Trip repository | Trip pages, lookup, add, and remove |
| Identity-user lookup | Read-only single and batch Identity-user mapping reads |
| Unit of work | Save, execution strategy, transaction begin, commit, and rollback coordination |

## State Transitions

No new state transitions are introduced. Existing unrestricted vacation-status updates and existing employee-status updates remain unchanged until Phase 5.

## Explicitly Deferred Model Changes

- Soft-delete fields.
- Vacation balance fields.
- Vacation review audit fields.
- Trip-to-employee relationships.
- New entities.
- New migrations.
