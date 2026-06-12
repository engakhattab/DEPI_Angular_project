# Phase 7 Advanced HR Features Migration Strategy

Migration: `Phase7AdvancedHrFeatures`

## Existing Rows

- Existing `Employees` rows receive `Role = Employee` through the migration default.
- Existing employees are not given compensation profiles, attendance rows, document metadata, salary history, or audit-log backfill.
- Existing trip requester data, vacation data, soft-delete state, and Phase 5 business-rule data remain unchanged.
- Existing rows remain query-safe after the migration because Phase 7 feature tables start empty and use explicit foreign keys for new data only.

## Initial System Administrator Bootstrap

- Initial System Administrator creation is not fake seed data and is not performed inside the migration.
- Startup invokes the idempotent `InitialSystemAdminBootstrapper` before the request pipeline can expose administrative RBAC.
- If an active `SystemAdministrator` already exists, bootstrap does nothing and does not create another admin, overwrite roles, or reset passwords.
- If no active `SystemAdministrator` exists, `InitialAdminBootstrap:Mode = CreateInitialAdmin` creates the initial `ApplicationUser` and linked `Employee` from secure configuration.
- Required creation configuration includes employee number, email, full name, existing department id, temporary password, and force-password-change preference.
- Missing required fields, invalid mode, duplicate employee number, duplicate email, invalid password, or missing department fail clearly and assign no fallback administrator.
- User creation, employee creation, role assignment, and audit writing are transactional; bootstrap must not leave partial user, employee, role, or audit records.
- A successful bootstrap writes an audit entry with actor marker `SYSTEM_BOOTSTRAP`, affected employee id, assigned role `SystemAdministrator`, employee number, email, and UTC timestamp. Temporary passwords, password hashes, tokens, security stamps, and cookies are not audited.

## Future Hardening

Any future non-null, uniqueness, or backfill hardening that depends on production data cleanup must be handled in a separate approved migration.
