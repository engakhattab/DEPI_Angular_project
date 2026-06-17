# Phase 13 Data Model: Response Documentation Artifacts

Phase 13 does not introduce persisted application data. The entities below describe documentation artifacts and verification records used to plan, implement, and validate Swagger/OpenAPI response documentation.

## Endpoint Group

Represents a controller-level API area that must be reviewed.

| Field | Description | Validation |
|-------|-------------|------------|
| `name` | User-facing endpoint group name | Must be one of Auth, Employees, Departments, Attendance, Vacation Requests, Trips, Compensation, Employee Documents, Dashboard, Audit Logs |
| `controller` | Current controller source file | Must map to an existing controller in `HR.API/Controllers` |
| `routePrefix` | Current route prefix shown in Swagger | Must not be changed by Phase 13 |
| `securityPolicy` | Existing auth or role policy, if any | Must reflect current attributes and global authorization behavior |

## Endpoint Operation

Represents one documented route/action in an endpoint group.

| Field | Description | Validation |
|-------|-------------|------------|
| `method` | HTTP method | Must match the existing controller action |
| `route` | Route template | Must match the existing route; no rename or move allowed |
| `actionName` | Controller action method | Must map to current source |
| `successResponses` | Expected successful response entries | Must include every current `200`, `201`, or `204` outcome |
| `errorResponses` | Expected error response entries | Must include only statuses currently supported by the action/pipeline |
| `responsePayload` | Existing success DTO, paged list, empty response, or file response | Must not imply a runtime shape change |

## Response Documentation Entry

Represents one status code documented for one endpoint operation.

| Field | Description | Validation |
|-------|-------------|------------|
| `statusCode` | HTTP response status | Must be a current API outcome |
| `category` | Success, client error, auth error, business-rule error, or server error | Must be consistent with current behavior |
| `payloadShape` | DTO, `PagedList<T>`, structured error, file, or no body | Must match runtime behavior |
| `description` | Human-readable Swagger/OpenAPI description | Must not promise unsupported behavior |
| `source` | Controller/service/middleware behavior that justifies the entry | Must be traceable during implementation review |

## Structured Error Payload

Represents the existing JSON error body shape.

| Field | Description | Validation |
|-------|-------------|------------|
| `code` | Machine-readable error code | Must preserve current compatibility codes |
| `message` | Human-readable error message | Must remain a string |

## File Response

Represents an operation that returns downloadable file content.

| Field | Description | Validation |
|-------|-------------|------------|
| `content` | Binary file content | Must be documented as a file/binary response, not normal JSON |
| `contentType` | Runtime file media type | Must be sourced from current download behavior |
| `fileName` | Download filename | Must be sourced from current download behavior |

## Swagger Verification Result

Represents Phase 13 completion evidence.

| Field | Description | Validation |
|-------|-------------|------------|
| `endpointGroup` | Reviewed group | Must be one of the required groups |
| `routesPresent` | Whether expected routes still appear | Must be true for completion |
| `successStatusesDocumented` | Whether expected success statuses appear documented | Must be true for completion |
| `errorStatusesDocumented` | Whether expected error statuses appear documented | Must be true for applicable errors |
| `followUpFindings` | Runtime or contract mismatches outside Phase 13 | Must not be fixed in Phase 13 without separate approval |

## Relationships

- One Endpoint Group has many Endpoint Operations.
- One Endpoint Operation has many Response Documentation Entries.
- A Response Documentation Entry references one payload shape: DTO, `PagedList<T>`, Structured Error Payload, File Response, or no body.
- One Swagger Verification Result belongs to one Endpoint Group.

## State Transitions

Response documentation entries move through these planning/implementation states:

1. `Identified`: Current endpoint outcome is found in controller/service behavior.
2. `Documented`: Swagger/OpenAPI metadata has been added for the outcome.
3. `Verified`: Swagger UI or OpenAPI JSON shows the outcome as documented.
4. `Follow-up`: A behavior mismatch is found and recorded outside Phase 13 scope.
