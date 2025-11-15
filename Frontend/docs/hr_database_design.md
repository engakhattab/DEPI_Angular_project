# Human Resources Management System Database Design

## 1. Overview
This document defines a relational database design for the first phase of the company management platform: the Human Resources (HR) module. The schema targets a Microsoft SQL Server implementation to align with the .NET back-end, but the structure is portable to any relational database that supports ANSI SQL and role-based security.

### 1.1 Objectives
- Capture the full employee lifecycle from recruitment to separation.
- Support core HR workflows: organization management, attendance, leave, payroll, benefits, performance, and compliance tracking.
- Provide a scalable foundation for future modules (finance, procurement, inventory) without disruptive schema changes.
- Expose clean, normalized entities for API consumption by the Angular 17 front-end.

### 1.2 Guiding Principles
1. **Normalization with pragmatic denormalization** – tables are at least in third normal form; small derived tables exist where they simplify reporting (for example, `vwEmployeeDirectory`).
2. **Auditing & traceability** – every transactional table includes `CreatedBy`, `CreatedAt`, `ModifiedBy`, and `ModifiedAt` fields; key business events use history tables.
3. **Soft deletes** – `IsActive` or `EndDate` flags retain historical data while hiding inactive records from day-to-day queries.
4. **Configurable master data** – lookups (job titles, leave types, benefit plans) live in dedicated tables that business users can extend without code changes.
5. **Security by design** – user accounts and permissions are stored centrally to support role-based API authorization.

## 2. High-Level Domain Model
- **Core HR**: Employees, organizational structure (company, departments, positions), contracts, and documents.
- **Time & Attendance**: Work schedules, attendance logs, overtime, leave management, and holidays.
- **Compensation & Benefits**: Payroll cycles, salary structures, allowances, deductions, benefits enrollment.
- **Performance & Development**: Goals, reviews, skills, training.
- **Security & Compliance**: User accounts, roles, permissions, audit logs.

Future modules can reuse `Employee`, `Department`, `UserAccount`, and `Document` tables to minimize duplication.

## 3. Schema Breakdown
### 3.1 Reference & Configuration Tables
| Table | Purpose | Key Fields |
|-------|---------|------------|
| `Company` | Stores company legal entities; useful for multi-company environments. | `CompanyId`, `Name`, `TaxNumber`, `CurrencyCode`, `IsActive` |
| `Department` | Organizational units. Hierarchy handled via `ParentDepartmentId`. | `DepartmentId`, `CompanyId`, `Name`, `ParentDepartmentId`, `ManagerEmployeeId`, `IsActive` |
| `Position` | Defines job titles/roles within departments. | `PositionId`, `DepartmentId`, `Title`, `JobLevel`, `FLSAStatus`, `IsActive` |
| `EmploymentType` | Full-time, part-time, contractor, intern, etc. | `EmploymentTypeId`, `Name`, `Description`, `IsActive` |
| `LeaveType` | Configurable leave categories. | `LeaveTypeId`, `Name`, `RequiresApproval`, `MaxBalance`, `IsPaid` |
| `BenefitPlan` | Catalog of benefits (health insurance, retirement). | `BenefitPlanId`, `Name`, `Provider`, `CoverageType`, `EmployeeContributionPercent`, `EmployerContributionPercent`, `IsActive` |
| `DeductionType` | Pre-tax/post-tax deduction definitions. | `DeductionTypeId`, `Name`, `Description`, `CalculationMethod`, `IsActive` |
| `AllowanceType` | Allowances such as housing or transportation. | `AllowanceTypeId`, `Name`, `Description`, `CalculationMethod`, `IsTaxable` |
| `PerformanceMetric` | Metrics used in evaluations (productivity, teamwork). | `PerformanceMetricId`, `Name`, `Description`, `WeightDefault` |
| `Skill` | Skill taxonomy for employee competency tracking. | `SkillId`, `Name`, `Category`, `Description` |

### 3.2 Core HR Tables
| Table | Purpose | Key Fields |
|-------|---------|------------|
| `Employee` | Master record for each worker. | `EmployeeId`, `CompanyId`, `EmployeeNumber`, `FirstName`, `LastName`, `PreferredName`, `Gender`, `DateOfBirth`, `NationalIdNumber`, `Email`, `Phone`, `HireDate`, `EmploymentTypeId`, `Status`, `ManagerEmployeeId`, `PhotoDocumentId`, `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy` |
| `EmployeeAddress` | Current and historical addresses. | `EmployeeAddressId`, `EmployeeId`, `AddressType`, `Line1`, `Line2`, `City`, `State`, `PostalCode`, `Country`, `StartDate`, `EndDate`, `IsPrimary` |
| `EmployeeContact` | Emergency contacts and dependents. | `EmployeeContactId`, `EmployeeId`, `ContactName`, `Relationship`, `Phone`, `Email`, `IsEmergencyContact`, `IsDependent` |
| `EmployeeDocument` | Uploaded documents (contracts, IDs). | `EmployeeDocumentId`, `EmployeeId`, `DocumentType`, `FileName`, `StoragePath`, `IssuedDate`, `ExpiryDate`, `IsSensitive` |
| `EmploymentContract` | Employment agreements (salary, probation). | `EmploymentContractId`, `EmployeeId`, `PositionId`, `StartDate`, `EndDate`, `ProbationEndDate`, `BaseSalary`, `CurrencyCode`, `WorkHoursPerWeek`, `PayFrequency`, `IsCurrent` |
| `EmployeePositionHistory` | Tracks position changes over time. | `EmployeePositionHistoryId`, `EmployeeId`, `PositionId`, `EffectiveStartDate`, `EffectiveEndDate`, `Reason`, `ApprovedBy` |
| `EmployeeSkill` | Employee skills with proficiency levels. | `EmployeeSkillId`, `EmployeeId`, `SkillId`, `ProficiencyLevel` (1-5), `YearsExperience`, `LastUsedDate` |

### 3.3 Time & Attendance Tables
| Table | Purpose | Key Fields |
|-------|---------|------------|
| `WorkSchedule` | Weekly schedule templates. | `WorkScheduleId`, `Name`, `Description`, `HoursPerWeek`, `IsDefault` |
| `WorkScheduleDetail` | Daily patterns for a schedule. | `WorkScheduleDetailId`, `WorkScheduleId`, `DayOfWeek`, `StartTime`, `EndTime`, `BreakMinutes` |
| `EmployeeScheduleAssignment` | Assigns schedules to employees. | `EmployeeScheduleAssignmentId`, `EmployeeId`, `WorkScheduleId`, `EffectiveStartDate`, `EffectiveEndDate` |
| `AttendanceLog` | Clock-in/out data. | `AttendanceLogId`, `EmployeeId`, `LogDate`, `CheckInTime`, `CheckOutTime`, `HoursWorked`, `Source` (web, kiosk, mobile), `ApprovedBy`, `Status` |
| `OvertimeRequest` | Employee-submitted overtime. | `OvertimeRequestId`, `EmployeeId`, `RequestDate`, `HoursRequested`, `Reason`, `Status`, `ApprovedBy`, `ApprovedAt` |
| `LeaveBalance` | Accrued leave per employee & type. | `LeaveBalanceId`, `EmployeeId`, `LeaveTypeId`, `BalanceHours`, `CarryOverHours`, `LastUpdated` |
| `LeaveRequest` | Leave applications. | `LeaveRequestId`, `EmployeeId`, `LeaveTypeId`, `StartDate`, `EndDate`, `TotalHours`, `Reason`, `Status`, `ApproverId`, `RequestedAt`, `ReviewedAt`, `AttachmentDocumentId` |
| `Holiday` | Company-wide holidays by location. | `HolidayId`, `CompanyId`, `Name`, `HolidayDate`, `Location`, `IsRecurring` |

### 3.4 Compensation & Benefits Tables
| Table | Purpose | Key Fields |
|-------|---------|------------|
| `PayrollCycle` | Defines payroll periods. | `PayrollCycleId`, `CompanyId`, `CycleName`, `PeriodStart`, `PeriodEnd`, `PayDate`, `Status` |
| `SalaryComponent` | Base pay, allowances, deductions. | `SalaryComponentId`, `EmployeeId`, `ComponentType` (Allowance/Deduction/Base), `ReferenceId` (FK to AllowanceType, DeductionType), `Amount`, `IsPercentage`, `EffectiveStartDate`, `EffectiveEndDate` |
| `Timesheet` | Aggregated hours per period. | `TimesheetId`, `EmployeeId`, `PayrollCycleId`, `RegularHours`, `OvertimeHours`, `LeaveHours`, `Status`, `SubmittedAt`, `ApprovedAt`, `ApproverId` |
| `PayrollRun` | Header for generated payroll. | `PayrollRunId`, `PayrollCycleId`, `RunNumber`, `RunDate`, `PreparedBy`, `ApprovedBy`, `Status`, `Notes` |
| `PayrollLine` | Individual employee payroll results. | `PayrollLineId`, `PayrollRunId`, `EmployeeId`, `GrossPay`, `TotalAllowances`, `TotalDeductions`, `NetPay`, `CurrencyCode`, `PaymentMethod`, `BankAccountId` |
| `PayrollEarningDetail` | Breakdown of earnings. | `PayrollEarningDetailId`, `PayrollLineId`, `SalaryComponentId`, `Description`, `Amount` |
| `PayrollDeductionDetail` | Breakdown of deductions. | `PayrollDeductionDetailId`, `PayrollLineId`, `SalaryComponentId`, `Description`, `Amount` |
| `BenefitEnrollment` | Employee enrollment status per plan. | `BenefitEnrollmentId`, `EmployeeId`, `BenefitPlanId`, `EnrollmentDate`, `CoverageLevel`, `EmployeeContribution`, `EmployerContribution`, `Status`, `DependentDocumentId` |
| `BankAccount` | Employee bank accounts for payroll. | `BankAccountId`, `EmployeeId`, `BankName`, `AccountNumber`, `RoutingNumber`, `IBAN`, `IsPrimary` |

### 3.5 Performance & Development Tables
| Table | Purpose | Key Fields |
|-------|---------|------------|
| `PerformanceCycle` | Defines review periods. | `PerformanceCycleId`, `Name`, `StartDate`, `EndDate`, `Status` |
| `PerformanceGoal` | Goal records per employee. | `PerformanceGoalId`, `EmployeeId`, `CycleId`, `Title`, `Description`, `Weight`, `TargetValue`, `CurrentValue`, `DueDate`, `Status` |
| `PerformanceReview` | Manager review header. | `PerformanceReviewId`, `EmployeeId`, `CycleId`, `ReviewerId`, `ReviewDate`, `OverallRating`, `Comments`, `Status` |
| `PerformanceReviewMetric` | Individual metric ratings. | `PerformanceReviewMetricId`, `PerformanceReviewId`, `PerformanceMetricId`, `Rating`, `Comments` |
| `TrainingCourse` | Training catalog. | `TrainingCourseId`, `Title`, `Provider`, `DeliveryMethod`, `DurationHours`, `Cost`, `IsMandatory` |
| `TrainingEnrollment` | Employee participation in courses. | `TrainingEnrollmentId`, `EmployeeId`, `TrainingCourseId`, `EnrollmentDate`, `CompletionDate`, `Result`, `Score`, `CertificateDocumentId` |

### 3.6 Security & Audit Tables
| Table | Purpose | Key Fields |
|-------|---------|------------|
| `UserAccount` | System login credentials mapped to employees. | `UserAccountId`, `EmployeeId`, `Username`, `Email`, `PasswordHash`, `PasswordSalt`, `IsLocked`, `LastLoginAt`, `MustResetPassword`, `CreatedAt` |
| `Role` | Security roles (HR Admin, Manager, Employee). | `RoleId`, `Name`, `Description`, `IsSystemRole` |
| `Permission` | API or UI permissions. | `PermissionId`, `Code`, `Description`, `Module`, `IsActive` |
| `RolePermission` | Many-to-many between roles and permissions. | `RoleId`, `PermissionId`, `GrantedBy`, `GrantedAt` |
| `UserRole` | Assigns roles to users. | `UserRoleId`, `UserAccountId`, `RoleId`, `AssignedBy`, `AssignedAt`, `IsPrimaryRole` |
| `ApiAuditLog` | Records API calls for compliance. | `ApiAuditLogId`, `UserAccountId`, `Endpoint`, `HttpMethod`, `RequestPayload`, `ResponseCode`, `Timestamp`, `DurationMs`, `ClientIp` |
| `SystemNotification` | Alerts for actions (leave approvals, expiring documents). | `SystemNotificationId`, `RecipientUserId`, `Title`, `Message`, `Link`, `IsRead`, `CreatedAt`, `ReadAt` |

## 4. Relationship Diagram (Textual)
- `Company` 1—* `Department` 1—* `Position`.
- `Department` self-references `ParentDepartmentId` for hierarchy.
- `Employee` belongs to `Company`, `EmploymentType`, and optionally references a `Manager` (self-reference).
- `EmploymentContract` links an `Employee` to their current `Position` and compensation terms.
- `Employee` 1—* `EmployeeAddress`, `EmployeeContact`, `EmployeeDocument`, `EmployeeSkill`, `BenefitEnrollment`, `BankAccount`.
- `LeaveRequest` references `LeaveType`, `Employee`, and optionally a supporting `EmployeeDocument`.
- `EmployeeScheduleAssignment` and `AttendanceLog` allow accurate timesheet calculations feeding `Timesheet`, `PayrollLine`, and `PayrollEarningDetail` tables.
- `PerformanceReview` references `Employee`, `PerformanceCycle`, and `Reviewer` (also an employee).
- `UserAccount` optionally maps to `Employee` to secure HR endpoints; `UserRole` + `RolePermission` enforce access.

## 5. Data Flow & API Considerations
1. **Onboarding Workflow**
   - Create `Employee`, `EmploymentContract`, `UserAccount`, initial `SalaryComponent` entries, and assign default `WorkSchedule`.
   - Front-end can call onboarding API endpoints sequentially; transactions ensure consistency.

2. **Time & Attendance**
   - Daily attendance captured in `AttendanceLog` via kiosk/mobile API.
   - Nightly job aggregates to `Timesheet` by `PayrollCycle`.
   - Managers approve `Timesheet` and `OvertimeRequest` records; approvals trigger notifications.

3. **Leave Management**
   - Employee submits `LeaveRequest`; system checks `LeaveBalance` for available hours.
   - Manager approval updates `LeaveRequest.Status` and decrements `LeaveBalance`.
   - Optional attachments stored as `EmployeeDocument` entries referenced by the request.

4. **Payroll Processing**
   - HR creates a `PayrollRun` for a given `PayrollCycle`.
   - Stored procedures consolidate `SalaryComponent`, `Timesheet`, and `BenefitEnrollment` data into `PayrollLine`/detail tables.
   - Results surface to Angular via payroll summary/detail endpoints.

5. **Performance Reviews**
   - Goals stored in `PerformanceGoal`; status updates from employees or managers through API.
   - At cycle close, managers submit `PerformanceReview` and related `PerformanceReviewMetric` ratings.

6. **Security & Auditing**
   - API gateway writes entries to `ApiAuditLog` for every request.
   - Role-based access ensures HR staff can manage sensitive data while managers/employees access limited views.

## 6. Implementation Notes for the .NET Back-End
- Use Entity Framework Core code-first migrations; define each table as an entity with fluent API for relationships.
- Implement `BaseAuditableEntity` abstract class (`CreatedBy`, `CreatedAt`, etc.) for reuse.
- Configure `RowVersion` (timestamp) columns on transactional tables to support optimistic concurrency.
- Seed essential lookups (`EmploymentType`, `LeaveType`, `Role`, `Permission`) in initial migrations.
- Enable soft delete filters in EF Core (`IsActive`, `EndDate`) so APIs return only relevant records by default.
- Create views or stored procedures for complex payroll calculations; expose them via repositories or stored procedure calls.
- Implement background jobs (using Hangfire or Quartz.NET) for leave balance accrual, payroll generation, notification dispatch, and document expiry alerts.

## 7. Sample API Endpoints
| Module | Endpoint | Method | Description |
|--------|----------|--------|-------------|
| Core HR | `/api/employees` | GET/POST | Manage employee directory. |
| Core HR | `/api/employees/{id}/contracts` | GET/POST | Retrieve or create employment contracts. |
| Time & Attendance | `/api/attendance/logs` | POST | Submit attendance events from kiosk/mobile. |
| Time & Attendance | `/api/leave/requests/{id}/approve` | POST | Manager approval workflow. |
| Payroll | `/api/payroll/runs` | POST | Initiate payroll calculation for a cycle. |
| Payroll | `/api/payroll/runs/{id}/lines` | GET | Retrieve payroll details for the front-end. |
| Performance | `/api/performance/goals` | GET/POST | Manage employee goals. |
| Security | `/api/admin/users/{id}/roles` | PUT | Assign roles to users. |

## 8. Scalability & Future Expansion
- **Integration readiness**: External systems (finance, ERP) can connect via integration tables or message queues using the unique IDs defined here.
- **Future modules**: Finance can reuse `PayrollLine` data; asset management can link to `Employee`. The security module is intentionally generic to support broader access control.
- **Analytics**: Create star schemas in a separate data warehouse by sourcing from the normalized tables (dimensional modeling for reports).

## 9. Next Steps
1. Validate schema with stakeholders and adjust to local labor regulations.
2. Define user stories and APIs per module, ensuring coverage of CRUD, approval flows, and reporting.
3. Implement EF Core entities and DbContext based on this schema; generate initial migration.
4. Coordinate with the front-end developer to map endpoints to Angular services and components.
5. Plan automated tests (unit + integration) around onboarding, leave accrual, and payroll calculations to protect business rules.

