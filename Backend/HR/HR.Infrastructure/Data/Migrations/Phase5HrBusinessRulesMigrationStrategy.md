Phase 5 trip requester migration strategy

- Existing trip rows must not break the migration.
- Do not invent fake requester data for existing trips.
- Add RequestedByEmployeeId as nullable for existing rows unless a reliable historical requester source is available.
- Require RequestedByEmployeeId for new trip creation through the API and service layer after Phase 5.
- Keep existing rows with null requester safe in queries, DTO mapping, compatibility checks, and regression coverage.
- Introduce any future non-null database constraint only in a separate approved migration after reliable backfill is possible.
