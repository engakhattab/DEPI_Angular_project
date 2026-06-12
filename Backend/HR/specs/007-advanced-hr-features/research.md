# Research: Phase 7 - Advanced HR Features

## Decision 1: Use Single Employee Role Value

**Decision**: Add one current role per employee: `Employee`, `Manager`, `HRAdministrator`, or `SystemAdministrator`.

**Rationale**: The specification's access matrix is hierarchical enough for the current HR system and explicitly excludes multiple simultaneous roles, temporary elevation, and permission union behavior. A single role keeps authorization rules testable and prevents role-combination ambiguity during the first RBAC phase.

**Alternatives considered**:

- Multiple role assignments per employee: rejected because it creates permission-union edge cases outside Phase 7 scope.
- Temporary role grants: rejected because no time-bound delegation workflow is specified.
- ASP.NET Identity roles only: rejected as the primary business role source because the existing employee profile is the HR authorization unit and must remain visible to services.

## Decision 2: Create Initial System Administrator From Secure Configuration

**Decision**: When no active `SystemAdministrator` exists, create the initial administrator from secure `InitialAdminBootstrap` configuration using the primary `CreateInitialAdmin` mode. If an active System administrator already exists, bootstrap is a no-op.

**Rationale**: This prevents all-admin defaults, avoids public setup endpoints, and avoids locking out administration on first deployment. The flow is testable, idempotent, and can fail clearly before role enforcement blocks privileged workflows. Sensitive values such as the temporary password can be supplied through environment variables or user secrets per client.

**Alternatives considered**:

- First active employee becomes admin: rejected because it is order-dependent and risky.
- All active employees become HR administrators: rejected because it grants broad privilege.
- Promoting an existing configured employee by email or employee number: allowed only as a secondary future mode if separately approved; rejected as the Phase 7 default because the approved first-run path is to create the initial administrator from secure configuration.
- Manual database update only: rejected because it is error-prone and not reviewable enough for repeatable deployment.

## Decision 3: Keep Cookie Auth and Add Role Context Additively

**Decision**: Preserve cookie-based session authentication and existing claims, adding role context only as an additive authorization detail.

**Rationale**: The constitution forbids JWT for this project, and Phase 7 requires RBAC without replacing the auth mechanism. Existing clients must not lose current login/current-user data.

**Alternatives considered**:

- JWT or refresh tokens: rejected by constitution.
- Replacing claims with role-only identity: rejected because existing claims are part of compatibility.
- Performing all authorization only in controllers: rejected because manager-chain and employee-state decisions need service/data access.

## Decision 4: Derive Attendance Date from Configured Business Timezone

**Decision**: Store actual attendance timestamps in UTC and derive `AttendanceDate` from a configured named business timezone, using `Africa/Cairo` as a development default only.

**Rationale**: Attendance is a business-day concept and must work across deployments in Egypt, Europe, or the Gulf. A named configured timezone avoids trusting client dates or unnamed server local time. Startup validation prevents silent timezone drift.

**Alternatives considered**:

- UTC-only attendance date: rejected because it misclassifies local days near midnight.
- Client-provided local date: rejected because it can be tampered with or inconsistent.
- Server local time: rejected because deployment host settings are not a stable business contract.

## Decision 5: Dedicated Compensation Profile and Salary History

**Decision**: Store current compensation in a dedicated employee compensation profile and store value-changing updates in salary history.

**Rationale**: Compensation is sensitive and must not appear in normal employee responses. A dedicated profile keeps query shape and DTO exposure explicit while supporting authorized history review.

**Alternatives considered**:

- Add salary fields directly to `Employee`: rejected because it increases accidental exposure risk in normal employee projections.
- Store only salary history and infer current value: rejected because current compensation reads become unnecessarily complex.
- Defer salary history: rejected because the spec requires compensation changes to be logged.

## Decision 6: Backend-Managed Local Document Storage

**Decision**: Store document binary files in backend-managed local storage outside public static folders and store metadata in SQL Server.

**Rationale**: This satisfies the user's clarification, avoids large binary content in business records, and keeps Phase 7 self-contained without external services. Authorized download endpoints can enforce role and employee-scope checks.

**Alternatives considered**:

- Store binaries in SQL Server: rejected by explicit clarification and because it bloats business records.
- Store only external links: rejected because Phase 7 should support real upload/download without external dependency.
- Public static folder storage: rejected because documents are sensitive and must require authorization.

## Decision 7: Redacted General Audit Details

**Decision**: General audit logs store changed field names and non-sensitive before/after values. Sensitive values are redacted or summarized.

**Rationale**: Audit logs remain useful for review without duplicating salary values, document content, or protected storage details in a broad audit surface.

**Alternatives considered**:

- Store full before/after values for all fields: rejected because it duplicates sensitive data.
- Store only actor/action/time/entity: rejected because it is too weak for review.
- Defer sensitive audit details: rejected because Phase 7 requires traceability for sensitive features.

## Decision 8: Derived Dashboard Summary

**Decision**: Build dashboard summary metrics from current HR records at request time, scoped by role.

**Rationale**: The current data scale and feature scope do not justify cached summary tables or background jobs. Derived queries keep Phase 7 simpler and avoid synchronization bugs.

**Alternatives considered**:

- Dashboard summary table: rejected as premature unless later performance data proves it necessary.
- Background aggregation job: rejected because it adds operational complexity outside current scope.
- Client-side aggregation from multiple endpoints: rejected because it leaks more data and increases frontend complexity.

## Decision 9: One Phase 7 Migration

**Decision**: Create one approved EF Core migration for Phase 7 schema changes and do not modify existing migrations.

**Rationale**: Phase 7 adds new persisted concepts and role backfill defaults. A single migration keeps review focused while preserving migration history.

**Alternatives considered**:

- Multiple migrations in one phase: rejected unless implementation uncovers a sequencing need.
- Manual SQL-only schema changes: rejected because the project uses EF Core migration discipline.
- Editing Phase 5 migration: rejected by constitution and migration discipline.
