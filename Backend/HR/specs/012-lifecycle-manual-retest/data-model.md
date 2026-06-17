# Data Model: Phase 12 - Lifecycle Documentation and Manual Retest

Phase 12 does not add or change database tables, application entities, DTOs, or migrations. The following are documentation and validation records used to structure manual retest work.

## ManualRetestEnvironment

Represents the local validation environment used for Phase 12 evidence.

| Field | Description | Validation |
|-------|-------------|------------|
| `databaseName` | Local SQL Server database used for retest | Must be `HrSystemDb_Phase12LifecycleTest` |
| `connectionSource` | How the connection string was supplied | Must be environment variable, user secret, or local uncommitted config |
| `migrationState` | Applied migration list/status | Must include approved migrations through Phase 11 |
| `apiBaseUrl` | Local API URL used for manual requests | Must point to the running local API |
| `testTool` | Swagger/Postman/HTTP client used | Must preserve auth cookies |
| `runDate` | Date/time of the manual retest | Required in implementation summary |

## TestActor

Represents one local-only employee identity used during the retest.

| Field | Description | Validation |
|-------|-------------|------------|
| `employeeNumber` | Stable manual test employee number | One of `EMP001`, `EMP002`, `EMP003`, `EMP004` |
| `email` | Local-only login email | Must use `@test.com` placeholder values from spec |
| `role` | Application role | One of `SystemAdministrator`, `HRAdministrator`, `Manager`, `Employee` |
| `passwordLabel` | Local-only placeholder password reference | Must not be a production/customer credential |
| `managerRelationship` | Relationship used for team scope | Manager actor must have at least EMP004 in scope |
| `outsideScopeTarget` | Employee outside actor scope for negative tests | Required for Employee/Manager negative checks |

## ScopeValidationScenario

Represents a single manual authorization or business-rule validation step.

| Field | Description | Validation |
|-------|-------------|------------|
| `id` | Stable checklist identifier | Unique within Phase 12 checklist |
| `module` | Functional area | Auth, Employees, Vacations, Trips, Attendance, Compensation, Documents, Dashboard, or Audit |
| `actor` | Role executing the request | References a `TestActor` |
| `endpointOrAction` | Endpoint and method or manual action | Required |
| `setupDependency` | Data prerequisite | Required when the scenario depends on a created record |
| `expectedOutcome` | Expected status/result | References `ExpectedHttpOutcome` or documented success result |
| `actualOutcome` | Observed status/result | Required after execution |
| `result` | Pass/fail/blocked | Required after execution |
| `notes` | Failure detail or follow-up | Required for failed/blocked scenarios |

## ExpectedHttpOutcome

Represents the expected response behavior for an HTTP scenario.

| Field | Description | Validation |
|-------|-------------|------------|
| `statusCode` | Expected HTTP status | Must use current behavior, especially 401, 403, 404, 409, 422 where relevant |
| `responseShape` | Expected response format | Error responses must preserve `{ code, message }` compatibility |
| `dataVisibility` | Allowed data exposure | Must not leak out-of-scope employee/vacation/trip data |
| `paginationShape` | List response envelope expectation | Scoped empty lists must preserve normal list/page shape |
| `compatibilityNote` | Existing compatibility caveat | Required if field names are compatibility aliases |

## GuideSectionUpdate

Represents one documentation section that must be reviewed and possibly updated.

| Field | Description | Validation |
|-------|-------------|------------|
| `documentPath` | Target guide path | `API_LIFECYCLE_TESTING_GUIDE.md` or `CLIENT_INSTALLATION_GUIDE.md` |
| `sectionName` | Section heading or topic | Required |
| `stalenessRisk` | Reason the section may need changes | Required |
| `requiredUpdate` | Required documentation change | Required unless no change is needed |
| `verification` | How the update is verified | Must link to checklist/evidence scenario where applicable |

## RetestEvidence

Represents final Phase 12 execution evidence.

| Field | Description | Validation |
|-------|-------------|------------|
| `environment` | `ManualRetestEnvironment` details | Required |
| `commandsRun` | Restore/build/test/EF/manual setup commands | Required |
| `scenarios` | Completed `ScopeValidationScenario` entries | Must cover all required role scenarios |
| `failures` | Failed or blocked results | Required, can be empty |
| `followUpDefects` | Defects needing separate approval | Required if failures indicate runtime behavior mismatch |
| `sourceChangeConfirmation` | Confirmation no source changes were made | Required |
| `migrationConfirmation` | Confirmation no new migrations were created | Required |

## State Transitions

Manual retest scenarios move through:

```text
Planned -> Ready -> Executed -> Passed
                         `-> Failed -> Follow-up defect recorded
                         `-> Blocked -> Blocked reason recorded
```

Runtime defect remediation is not a Phase 12 state transition. It requires separate approval and a separate implementation scope.
