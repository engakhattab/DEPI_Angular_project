# Data Model: Phase 6 - DI Registration Cleanup

## Summary

Phase 6 introduces no domain data, persistence model, schema, migration, seed data, or database constraint changes.

## Entity Impact

No entities are added, removed, or modified.

Existing Phase 5 entities remain unchanged:

- `Employee`
- `Department`
- `VacationRequest`
- `Trip`
- `ApplicationUser`

## Database Impact

No Phase 6 database changes are permitted.

The local database used for manual validation must already include the approved Phase 5 migration:

```text
20260603014628_Phase5HrBusinessRules
```

Phase 6 validation may read and write normal test data through existing endpoints, but those data mutations are manual validation activity, not Phase 6 schema work.

## Migration Policy

- Do not create a Phase 6 EF migration.
- Do not edit existing migrations.
- Do not edit `ApplicationDbContextModelSnapshot`.
- Do not add tables, columns, indexes, foreign keys, constraints, or seed data.

## State Transitions

No new state transitions are introduced. Existing Phase 5 employee and vacation request state machines remain active.
