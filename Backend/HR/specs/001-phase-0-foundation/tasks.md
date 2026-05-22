---
description: "Task list for Phase 0 — Foundation & Project Restructure"
---

# Tasks: Phase 0 — Foundation & Project Restructure

**Input**: Design documents from `specs/001-phase-0-foundation/`
**Prerequisites**: plan.md, spec.md, data-model.md

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create `HR.Domain` class library project (target .NET 8)
- [x] T002 Create `HR.Application` class library project (target .NET 8)
- [x] T003 Create `HR.Infrastructure` class library project (target .NET 8)
- [x] T004 Create `HR.Shared` class library project (target .NET 8)
- [x] T005 Add project references: `HR.Application` references `HR.Domain` and `HR.Shared`
- [x] T006 Add project references: `HR.Infrastructure` references `HR.Domain` and `HR.Shared`
- [x] T007 Add project references: `HR.API` references `HR.Application`, `HR.Infrastructure`, and `HR.Shared`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

- [x] T008 [P] Remove all NuGet package references from `HR.Domain.csproj` to ensure zero external dependencies
- [x] T009 [P] Add Microsoft.EntityFrameworkCore and Microsoft.AspNetCore.Identity.EntityFrameworkCore to `HR.Infrastructure.csproj`

**Checkpoint**: Foundation ready - project structure is in place.

---

## Phase 3: User Story 1 - Maintain Existing API Functionality (Priority: P1) 🎯 MVP

**Goal**: Relocate existing monolith classes into the layered architecture without breaking the API contract.

**Independent Test**: API compiles successfully and all endpoints return the exact same data format as before.

### Implementation for User Story 1

- [x] T010 [P] [US1] Move `Employee.cs`, `Department.cs`, `VacationRequest.cs`, `Trip.cs`, `ApplicationUser.cs` to `HR.Domain/Entities/` and update namespaces
- [x] T011 [P] [US1] Move `EmployeeStatus.cs`, `VacationRequestStatus.cs` to `HR.Domain/Enums/` and update namespaces
- [x] T012 [US1] Move `ApplicationDbContext.cs` and the entire `Migrations/` folder to `HR.Infrastructure/Data/` and update namespaces
- [x] T013 [P] [US1] Move all request/response DTOs to `HR.Application/` (under feature folders like Employees, Departments, etc.) and update namespaces
- [x] T014 [P] [US1] Move `DateOnlyJsonConverter.cs` and `NullableDateOnlyJsonConverter.cs` to `HR.Shared/` and update namespaces
- [x] T015 [US1] Fix all `using` statements in `HR.API` Controllers and `Program.cs` to reference the new project namespaces
- [x] T016 [US1] Compile the entire solution and resolve any remaining namespace or reference errors

**Checkpoint**: At this point, the API should be fully runnable and behave exactly as it did before the refactor.

---

## Phase 4: User Story 2 - Prepare Common Infrastructure (Priority: P2)

**Goal**: Introduce standard exception and result types to be used by the service layer in subsequent phases.

**Independent Test**: Types can be instantiated correctly in isolated unit tests (or verified visually).

### Implementation for User Story 2

- [x] T017 [P] [US2] Create `NotFoundException` class in `HR.Domain/Exceptions/NotFoundException.cs`
- [x] T018 [P] [US2] Create `ConflictException` class in `HR.Domain/Exceptions/ConflictException.cs`
- [x] T019 [P] [US2] Create `BusinessRuleException` class in `HR.Domain/Exceptions/BusinessRuleException.cs`
- [x] T020 [P] [US2] Create `ServiceError` record with standard error codes in `HR.Shared/Results/ServiceError.cs`
- [x] T021 [P] [US2] Create `Result<T>` wrapper class in `HR.Shared/Results/Result.cs`

**Checkpoint**: Domain exceptions and Result wrapper are ready for Phase 3 service extraction.

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T022 Run `dotnet clean` and `dotnet build` to ensure absolute compilation success
- [x] T023 Run any existing tests (if present) to ensure they still pass after namespace updates

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Must complete first to create the physical folders and `.csproj` files.
- **Foundational (Phase 2)**: Depends on Phase 1. Ensures correct NuGet packages.
- **User Story 1**: Depends on Phase 2. Relocates existing files.
- **User Story 2**: Can run in parallel with User Story 1 once Setup is complete.

### Parallel Opportunities

- All project creation tasks (T001-T004) can be run concurrently by an agent.
- Moving entities (T010), enums (T011), DTOs (T013), and JSON Converters (T014) can be done in parallel.
- All exception classes (T017-T019) and result classes (T020-T021) can be created in parallel.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 & 2.
2. Complete Phase 3 (US1).
3. **STOP and VALIDATE**: Verify the API runs and handles requests. This ensures the physical separation hasn't broken EF Core migrations or JSON serialization.

### Incremental Delivery

1. Complete MVP.
2. Add US2 infrastructure types.
3. Validate compilation again.
4. Phase 0 is complete and ready for code review.
