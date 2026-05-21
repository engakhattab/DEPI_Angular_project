# Data Model: Phase 0 Foundation

**Status**: Structural Only

## Overview

This phase introduces no new entities or schema changes. The data model remains exactly as it currently is in the database. The only change is the physical location of the classes in the solution.

## Entities to Relocate

All domain entities will be moved to `HR.Domain/Entities/`. 

- `Employee`
- `Department`
- `VacationRequest`
- `Trip`
- `ApplicationUser` (Identity user class)

## Enums to Relocate

All domain enums will be moved to `HR.Domain/Enums/`.

- `EmployeeStatus`
- `VacationRequestStatus`

## New Infrastructure Types

These are cross-cutting types rather than database entities, but they are defined in this phase to support future data operations:

- **`Result<T>`**: Wrapper for success/failure states (`HR.Shared`)
- **`ServiceError`**: Standardized error representation (`HR.Shared`)
- **`NotFoundException`**: Domain exception (`HR.Domain`)
- **`ConflictException`**: Domain exception (`HR.Domain`)
- **`BusinessRuleException`**: Domain exception (`HR.Domain`)
