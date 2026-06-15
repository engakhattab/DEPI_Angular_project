# Internal Contract: Authorization Scope Foundation

Phase 8 has no new public HTTP API contract. This file documents the internal application service contract that later phases must use.

## Contract Owner

- Interface: `HR.Application.Authorization.IEmployeeAccessService`
- Implementation: `HR.Infrastructure.Authorization.EmployeeAccessService`
- Data access: `HR.Infrastructure.Repositories.IEmployeeRepository`

`HR.Application` must not reference `HR.Infrastructure`.

## Required Service Decisions

### Get Current Employee Context

```csharp
Task<Result<EmployeeAccessContext>> GetCurrentAsync(Guid employeeId, CancellationToken ct);
```

Success result includes:

- `EmployeeId`
- `Role`
- `IsActive`
- `IsDeleted`
- `IsTerminated`

Failure behavior:

- Missing employee record returns an unauthorized-compatible failure.
- The method does not expose unrelated employee data.

### Is Self

```csharp
bool IsSelf(Guid requesterEmployeeId, Guid targetEmployeeId);
```

Returns true only when the two IDs are equal.

### Is Manager Of Target

```csharp
Task<bool> IsManagerOfAsync(Guid requesterEmployeeId, Guid targetEmployeeId, CancellationToken ct);
```

Returns true only when:

- requester exists
- requester is not deleted
- requester is not terminated
- requester role is `Manager`
- target is an active, non-deleted direct or indirect report

### Can Access Employee

```csharp
Task<bool> CanAccessEmployeeAsync(Guid requesterEmployeeId, Guid targetEmployeeId, CancellationToken ct);
```

Returns true when any of these are true:

- target is self
- requester is a manager and target is an active, non-deleted direct or indirect report
- requester has organization scope

Returns false for missing, deleted, or terminated requester.

### Can Access Team Data

```csharp
Task<bool> CanAccessTeamDataAsync(Guid requesterEmployeeId, Guid targetEmployeeId, CancellationToken ct);
```

Returns true when:

- requester has organization scope, or
- requester is a manager and target is requester self or active, non-deleted direct/indirect report

Normal employees do not have team scope beyond self access through `CanAccessEmployeeAsync`.

### Role Checks

```csharp
Task<bool> HasAnyRoleAsync(Guid employeeId, CancellationToken ct, params EmployeeRole[] roles);
Task<bool> IsHRAdministratorAsync(Guid employeeId, CancellationToken ct);
Task<bool> IsSystemAdministratorAsync(Guid employeeId, CancellationToken ct);
Task<bool> HasOrganizationScopeAsync(Guid employeeId, CancellationToken ct);
```

Role checks return false for missing, deleted, or terminated employees.

Suspended employees remain role/scope eligible in Phase 8 unless a later endpoint-specific phase forbids a particular action.

### Visible Employee IDs

```csharp
Task<IReadOnlySet<Guid>> GetVisibleEmployeeIdsAsync(Guid requesterEmployeeId, CancellationToken ct);
```

Expected sets:

- Employee: requester ID only.
- Manager: requester ID plus active, non-deleted direct and indirect reports.
- HRAdministrator: active organization employee IDs.
- SystemAdministrator: active organization employee IDs.
- Missing/deleted/terminated requester: empty set.

## Compatibility Constraints

This internal contract must not change:

- HTTP routes
- response JSON shapes
- cookie names/settings
- claim names or values
- public status codes
- public error codes
- database schema

## Later Phase Usage

- Phase 9 must use this contract for employee endpoint visibility and permissions.
- Phase 10 must use this contract for vacation request ownership, team review, and organization-scope access.
- Phase 11 must use this contract for trip ownership, team creation, requester/traveler rules, and organization-scope access.

