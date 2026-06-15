using HR.Application.DTOs.Employees;
using HR.Application.Employees;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Audit;
using HR.Infrastructure.Identity;
using HR.Infrastructure.Repositories;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace HR.Infrastructure.Employees;

public class EmployeeService(
    IEmployeeRepository employeeRepository,
    IDepartmentRepository departmentRepository,
    IVacationRequestRepository vacationRequestRepository,
    IIdentityUserLookup identityUserLookup,
    IUnitOfWork unitOfWork,
    UserManager<ApplicationUser> userManager,
    ILogger<EmployeeService> logger,
    TimeProvider timeProvider,
    IAuditWriter? auditWriter = null) : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;
    private readonly IVacationRequestRepository _vacationRequestRepository = vacationRequestRepository;
    private readonly IIdentityUserLookup _identityUserLookup = identityUserLookup;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ILogger<EmployeeService> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IAuditWriter? _auditWriter = auditWriter;

    public async Task<Result<PagedList<EmployeeResponse>>> GetEmployeesAsync(
        Guid requesterEmployeeId,
        EmployeeStatus? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var requester = await _employeeRepository.GetByIdAsync(requesterEmployeeId, ct);
        var authError = ValidateRequester(requester);
        if (authError is not null)
        {
            return Result<PagedList<EmployeeResponse>>.Failure(authError);
        }

        PagedList<Employee> pagedEmployees;
        switch (requester!.Role)
        {
            case EmployeeRole.Employee:
                return Result<PagedList<EmployeeResponse>>.Failure(ServiceError.Forbidden());

            case EmployeeRole.Manager:
                var teamIds = await _employeeRepository.GetDirectAndIndirectReportIdsAsync(requester.Id, ct);
                pagedEmployees = await _employeeRepository.GetScopedPageAsync(teamIds, status, page, pageSize, ct);
                break;

            case EmployeeRole.HRAdministrator:
            case EmployeeRole.SystemAdministrator:
                pagedEmployees = await _employeeRepository.GetOrganizationWidePageAsync(status, page, pageSize, ct);
                break;

            default:
                return Result<PagedList<EmployeeResponse>>.Failure(ServiceError.Forbidden());
        }

        var userIds = pagedEmployees.Items.Select(e => e.ApplicationUserId).Distinct().ToList();
        var userDict = await _identityUserLookup.GetByIdsAsync(userIds, ct);
        var items = pagedEmployees.Items
            .Select(e => MapToResponse(e, userDict.GetValueOrDefault(e.ApplicationUserId)))
            .ToList();

        return Result<PagedList<EmployeeResponse>>.Success(
            new PagedList<EmployeeResponse>(items, pagedEmployees.TotalCount, pagedEmployees.Page, pagedEmployees.PageSize));
    }

    public async Task<Result<EmployeeResponse>> GetEmployeeByIdAsync(
        Guid requesterEmployeeId,
        Guid id,
        CancellationToken ct)
    {
        var requester = await _employeeRepository.GetByIdAsync(requesterEmployeeId, ct);
        var authError = ValidateRequester(requester);
        if (authError is not null)
        {
            return Result<EmployeeResponse>.Failure(authError);
        }

        var targetExists = await _employeeRepository.ExistsIncludingSoftDeletedAsync(id, ct);
        if (!targetExists)
        {
            return Result<EmployeeResponse>.Failure(
                ServiceError.NotFound($"Employee '{id}' was not found.", "NOT_FOUND"));
        }

        switch (requester!.Role)
        {
            case EmployeeRole.Employee:
                if (requester.Id != id)
                {
                    return Result<EmployeeResponse>.Failure(ServiceError.Forbidden());
                }
                break;

            case EmployeeRole.Manager:
                if (requester.Id != id)
                {
                    var teamIds = await _employeeRepository.GetDirectAndIndirectReportIdsAsync(requester.Id, ct);
                    if (!teamIds.Contains(id))
                    {
                        return Result<EmployeeResponse>.Failure(ServiceError.Forbidden());
                    }
                }
                break;

            case EmployeeRole.HRAdministrator:
            case EmployeeRole.SystemAdministrator:
                break;

            default:
                return Result<EmployeeResponse>.Failure(ServiceError.Forbidden());
        }

        Employee? employee;
        if (requester.Role is EmployeeRole.HRAdministrator or EmployeeRole.SystemAdministrator)
        {
            employee = await _employeeRepository.GetByIdWithDetailsIncludingSoftDeletedAsync(id, ct);
        }
        else
        {
            employee = await _employeeRepository.GetByIdWithDetailsAsync(id, ct);
        }

        if (employee is null)
        {
            return Result<EmployeeResponse>.Failure(
                ServiceError.NotFound($"Employee '{id}' was not found.", "NOT_FOUND"));
        }

        var user = await _identityUserLookup.GetByIdAsync(employee.ApplicationUserId, ct);
        return Result<EmployeeResponse>.Success(MapToResponse(employee, user));
    }

    public async Task<Result<EmployeeCreatedResponse>> CreateEmployeeAsync(
        Guid requesterEmployeeId,
        EmployeeCreateRequest request,
        CancellationToken ct)
    {
        var authError = await CheckWriteAuthorizationAsync(requesterEmployeeId, ct);
        if (authError is not null)
        {
            return Result<EmployeeCreatedResponse>.Failure(authError);
        }

        if (await _employeeRepository.ExistsByNumberAsync(request.EmployeeNumber, ct))
        {
            return Result<EmployeeCreatedResponse>.Failure(
                ServiceError.Conflict($"Employee number '{request.EmployeeNumber}' already exists.", "CONFLICT"));
        }

        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, ct);
        if (department is null)
        {
            return Result<EmployeeCreatedResponse>.Failure(
                ServiceError.NotFound($"Department '{request.DepartmentId}' was not found.", "NOT_FOUND"));
        }

        Employee? manager = null;
        if (request.ManagerId.HasValue)
        {
            manager = await _employeeRepository.GetByIdAsync(request.ManagerId.Value, ct);
            if (manager is null || manager.IsDeleted)
            {
                return Result<EmployeeCreatedResponse>.Failure(
                    ServiceError.NotFound($"Manager '{request.ManagerId}' was not found.", "NOT_FOUND"));
            }
        }

        if (request.Status == EmployeeStatus.Active
            && await _employeeRepository.ExistsActiveWithEmailAsync(request.Email, null, ct))
        {
            return Result<EmployeeCreatedResponse>.Failure(
                ServiceError.Conflict("An active employee with this email already exists.", "CONFLICT"));
        }

        if (manager is not null && manager.DepartmentId != request.DepartmentId)
        {
            _logger.LogWarning(
                "Employee {EmployeeNumber} assigned manager {ManagerId} from a different department",
                request.EmployeeNumber,
                manager.Id);
        }

        var password = string.IsNullOrWhiteSpace(request.InitialPassword)
            ? GenerateTemporaryPassword()
            : request.InitialPassword!;
        var passwordWasGenerated = string.IsNullOrWhiteSpace(request.InitialPassword);

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        ServiceError? failure = null;
        EmployeeResponse? employeeResponse = null;

        await _unitOfWork.ExecuteWithStrategyAsync(async innerCt =>
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync(innerCt);

            var identityResult = await _userManager.CreateAsync(user, password);
            if (!identityResult.Succeeded)
            {
                failure = ServiceError.Validation(BuildIdentityErrorMessage(identityResult.Errors), "VALIDATION");
                await transaction.RollbackAsync(innerCt);
                return;
            }

            var employee = new Employee
            {
                EmployeeNumber = request.EmployeeNumber,
                FullName = request.FullName,
                Email = request.Email,
                DepartmentId = request.DepartmentId,
                ManagerId = request.ManagerId,
                BirthDate = request.BirthDate,
                JoinDate = request.JoinDate,
                JobTitle = request.JobTitle,
                PhoneNumber = request.PhoneNumber,
                Notes = request.Notes,
                Status = request.Status,
                VacationBalanceDays = 21,
                TerminatedAt = request.Status == EmployeeStatus.Terminated ? _timeProvider.GetUtcNow() : null,
                ApplicationUserId = user.Id,
                Department = department,
                Manager = manager
            };

            await _employeeRepository.AddAsync(employee, innerCt);
            await _unitOfWork.SaveChangesAsync(innerCt);
            await transaction.CommitAsync(innerCt);

            employeeResponse = MapToResponse(employee, user);
        }, ct);

        if (failure is not null)
        {
            return Result<EmployeeCreatedResponse>.Failure(failure);
        }

        if (employeeResponse is null)
        {
            throw new InvalidOperationException("Employee creation did not complete successfully.");
        }

        return Result<EmployeeCreatedResponse>.Success(new EmployeeCreatedResponse
        {
            Employee = employeeResponse,
            TemporaryPassword = passwordWasGenerated ? password : null
        });
    }

    public async Task<Result<EmployeeResponse>> UpdateEmployeeAsync(
        Guid requesterEmployeeId,
        Guid id,
        EmployeeUpdateRequest request,
        CancellationToken ct)
    {
        var roleAuthError = await CheckWriteAuthorizationAsync(requesterEmployeeId, ct);
        if (roleAuthError is not null)
        {
            return Result<EmployeeResponse>.Failure(roleAuthError);
        }

        var target = await _employeeRepository.GetByIdAsync(id, ct);
        if (target is null)
        {
            return Result<EmployeeResponse>.Failure(
                ServiceError.NotFound($"Employee '{id}' was not found.", "NOT_FOUND"));
        }

        var authError = await CheckWriteAuthorizationAsync(requesterEmployeeId, id, target, ct);
        if (authError is not null)
        {
            return Result<EmployeeResponse>.Failure(authError);
        }

        if (target.IsDeleted)
        {
            return Result<EmployeeResponse>.Failure(
                ServiceError.NotFound($"Employee '{id}' was not found.", "NOT_FOUND"));
        }

        if (IsLastAdminRemoval(target) && target.Status != request.Status)
        {
            var lastAdminError = await CheckLastAdminRemovalAsync(ct);
            if (lastAdminError is not null)
            {
                return Result<EmployeeResponse>.Failure(lastAdminError);
            }
        }

        if (request.ManagerId.HasValue && request.ManagerId.Value == id)
        {
            return Result<EmployeeResponse>.Failure(
                ServiceError.BusinessRule("An employee cannot be their own manager."));
        }

        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, ct);
        if (department is null)
        {
            return Result<EmployeeResponse>.Failure(
                ServiceError.NotFound($"Department '{request.DepartmentId}' was not found.", "NOT_FOUND"));
        }

        Employee? manager = null;
        if (request.ManagerId.HasValue)
        {
            manager = await _employeeRepository.GetByIdAsync(request.ManagerId.Value, ct);
            if (manager is null || manager.IsDeleted)
            {
                return Result<EmployeeResponse>.Failure(
                    ServiceError.NotFound($"Manager '{request.ManagerId}' was not found.", "NOT_FOUND"));
            }

            if (await WouldCreateCycleAsync(id, request.ManagerId.Value, ct))
            {
                return Result<EmployeeResponse>.Failure(
                    ServiceError.BusinessRule("The requested manager assignment would create a circular reporting chain."));
            }
        }

        if (request.Status == EmployeeStatus.Active
            && await _employeeRepository.ExistsActiveWithEmailAsync(request.Email, id, ct))
        {
            return Result<EmployeeResponse>.Failure(
                ServiceError.Conflict("An active employee with this email already exists.", "CONFLICT"));
        }

        if (target.Status != request.Status && !IsAllowedStatusTransition(target.Status, request.Status))
        {
            return Result<EmployeeResponse>.Failure(
                ServiceError.BusinessRule(
                    $"Cannot transition an employee from '{target.Status}' to '{request.Status}'."));
        }

        if (manager is not null && manager.DepartmentId != request.DepartmentId)
        {
            _logger.LogWarning(
                "Employee {EmployeeId} assigned manager {ManagerId} from a different department",
                id,
                manager.Id);
        }

        var shouldTerminate = target.Status != EmployeeStatus.Terminated && request.Status == EmployeeStatus.Terminated;

        target.FullName = request.FullName;
        target.Email = request.Email;
        target.DepartmentId = request.DepartmentId;
        target.ManagerId = request.ManagerId;
        target.BirthDate = request.BirthDate;
        target.JoinDate = request.JoinDate;
        target.JobTitle = request.JobTitle;
        target.PhoneNumber = request.PhoneNumber;
        target.Notes = request.Notes;
        target.Status = request.Status;

        if (shouldTerminate)
        {
            target.TerminatedAt ??= _timeProvider.GetUtcNow();

            var pendingRequests = await _vacationRequestRepository.GetPendingByEmployeeIdAsync(id, ct);
            foreach (var pendingRequest in pendingRequests)
            {
                pendingRequest.Status = VacationRequestStatus.Rejected;
                pendingRequest.UpdatedAt = _timeProvider.GetUtcNow();
            }
        }

        ApplicationUser? user = null;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);

        user = await _userManager.FindByIdAsync(target.ApplicationUserId);
        if (user is not null && !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = request.Email;
            user.UserName = request.Email;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                await transaction.RollbackAsync(ct);
                return Result<EmployeeResponse>.Failure(
                    ServiceError.Validation(BuildIdentityErrorMessage(updateResult.Errors), "VALIDATION"));
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        target.Department = department;
        target.Manager = manager;
        user = await _userManager.FindByIdAsync(target.ApplicationUserId);
        return Result<EmployeeResponse>.Success(MapToResponse(target, user));
    }

    public async Task<Result<EmployeeRoleResponse>> UpdateRoleAsync(
        Guid requesterEmployeeId,
        Guid id,
        EmployeeRoleUpdateRequest request,
        CancellationToken ct)
    {
        var requester = await _employeeRepository.GetByIdAsync(requesterEmployeeId, ct);
        if (requester is null || requester.IsDeleted || requester.Status == EmployeeStatus.Terminated || requester.Role != EmployeeRole.SystemAdministrator)
        {
            return Result<EmployeeRoleResponse>.Failure(ServiceError.Forbidden());
        }

        var employee = await _employeeRepository.GetByIdAsync(id, ct);
        if (employee is null || employee.IsDeleted)
        {
            return Result<EmployeeRoleResponse>.Failure(ServiceError.NotFound($"Employee '{id}' was not found.", "NOT_FOUND"));
        }

        if (employee.Status == EmployeeStatus.Terminated)
        {
            return Result<EmployeeRoleResponse>.Failure(ServiceError.BusinessRule("Terminated employees cannot be assigned roles."));
        }

        var now = _timeProvider.GetUtcNow();
        var previous = employee.Role;
        if (previous == request.Role)
        {
            return Result<EmployeeRoleResponse>.Success(new EmployeeRoleResponse
            {
                EmployeeId = employee.Id,
                Role = employee.Role,
                UpdatedAt = now
            });
        }

        if (previous == EmployeeRole.SystemAdministrator && request.Role != EmployeeRole.SystemAdministrator)
        {
            var activeAdminCount = await _employeeRepository.GetActiveSystemAdministratorCountAsync(ct);
            if (activeAdminCount <= 1)
            {
                return Result<EmployeeRoleResponse>.Failure(
                    ServiceError.BusinessRule("Cannot demote the last active SystemAdministrator."));
            }
        }

        employee.Role = request.Role;
        if (_auditWriter is not null)
        {
            await _auditWriter.WriteAsync(
                "Employee",
                employee.Id,
                AuditActionType.RoleChanged,
                requesterEmployeeId,
                null,
                ["Role"],
                new { role = previous.ToString() },
                new { role = request.Role.ToString() },
                null,
                ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<EmployeeRoleResponse>.Success(new EmployeeRoleResponse
        {
            EmployeeId = employee.Id,
            Role = employee.Role,
            UpdatedAt = now
        });
    }

    public async Task<Result> DeleteEmployeeAsync(
        Guid requesterEmployeeId,
        Guid id,
        CancellationToken ct)
    {
        var roleAuthError = await CheckWriteAuthorizationAsync(requesterEmployeeId, ct);
        if (roleAuthError is not null)
        {
            return Result.Failure(roleAuthError);
        }

        var target = await _employeeRepository.GetByIdAsync(id, ct);
        if (target is null)
        {
            return Result.Failure(ServiceError.NotFound($"Employee '{id}' was not found.", "NOT_FOUND"));
        }

        var authError = await CheckWriteAuthorizationAsync(requesterEmployeeId, id, target, ct);
        if (authError is not null)
        {
            return Result.Failure(authError);
        }

        if (target.IsDeleted)
        {
            return Result.Success();
        }

        if (IsLastAdminRemoval(target))
        {
            var lastAdminError = await CheckLastAdminRemovalAsync(ct);
            if (lastAdminError is not null)
            {
                return Result.Failure(lastAdminError);
            }
        }

        var directReports = await _employeeRepository.GetDirectReportsAsync(id, ct);
        var pendingVacationRequests = await _vacationRequestRepository.GetPendingByEmployeeIdAsync(id, ct);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        foreach (var report in directReports)
        {
            report.ManagerId = null;
        }

        foreach (var request in pendingVacationRequests)
        {
            request.Status = VacationRequestStatus.Rejected;
            request.UpdatedAt = _timeProvider.GetUtcNow();
        }

        target.IsDeleted = true;
        target.Status = EmployeeStatus.Terminated;
        target.TerminatedAt ??= _timeProvider.GetUtcNow();

        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result.Success();
    }

    private static ServiceError? ValidateRequester(Employee? requester)
    {
        if (requester is null || requester.IsDeleted)
        {
            return ServiceError.Unauthorized("Invalid session.");
        }

        if (requester.Status == EmployeeStatus.Terminated)
        {
            return ServiceError.Unauthorized("Invalid session.");
        }

        return null;
    }

    private async Task<ServiceError?> CheckWriteAuthorizationAsync(
        Guid requesterEmployeeId,
        CancellationToken ct)
    {
        var requester = await _employeeRepository.GetByIdAsync(requesterEmployeeId, ct);
        var authError = ValidateRequester(requester);
        if (authError is not null)
        {
            return authError;
        }

        if (requester!.Role is not (EmployeeRole.HRAdministrator or EmployeeRole.SystemAdministrator))
        {
            return ServiceError.Forbidden();
        }

        return null;
    }

    private async Task<ServiceError?> CheckWriteAuthorizationAsync(
        Guid requesterEmployeeId,
        Guid targetId,
        Employee target,
        CancellationToken ct)
    {
        var requester = await _employeeRepository.GetByIdAsync(requesterEmployeeId, ct);
        var authError = ValidateRequester(requester);
        if (authError is not null)
        {
            return authError;
        }

        if (requester!.Role is not (EmployeeRole.HRAdministrator or EmployeeRole.SystemAdministrator))
        {
            return ServiceError.Forbidden();
        }

        if (requester.Role == EmployeeRole.HRAdministrator && target.Role == EmployeeRole.SystemAdministrator)
        {
            return ServiceError.Forbidden();
        }

        return null;
    }

    private async Task<ServiceError?> CheckLastAdminRemovalAsync(CancellationToken ct)
    {
        var activeAdminCount = await _employeeRepository.GetActiveSystemAdministratorCountAsync(ct);
        if (activeAdminCount <= 1)
        {
            return ServiceError.BusinessRule("Cannot remove the last active SystemAdministrator.");
        }

        return null;
    }

    private static bool IsLastAdminRemoval(Employee target)
    {
        return target.Role == EmployeeRole.SystemAdministrator
            && target.Status == EmployeeStatus.Active
            && !target.IsDeleted;
    }

    private EmployeeResponse MapToResponse(Employee employee, ApplicationUser? user)
    {
        return new EmployeeResponse
        {
            Id = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            FullName = employee.FullName,
            Email = employee.Email ?? string.Empty,
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name ?? string.Empty,
            ManagerId = employee.ManagerId,
            ManagerName = employee.Manager?.FullName,
            BirthDate = employee.BirthDate,
            JoinDate = employee.JoinDate,
            JobTitle = employee.JobTitle,
            PhoneNumber = employee.PhoneNumber,
            Notes = employee.Notes,
            Status = employee.Status,
            Role = employee.Role,
            VacationBalanceDays = employee.VacationBalanceDays,
            IsDeleted = employee.IsDeleted,
            TerminatedAt = employee.TerminatedAt,
            IdentityUserId = employee.ApplicationUserId,
            UserName = user?.UserName ?? string.Empty
        };
    }

    private static bool IsAllowedStatusTransition(EmployeeStatus currentStatus, EmployeeStatus requestedStatus)
    {
        return currentStatus switch
        {
            EmployeeStatus.Active => requestedStatus is EmployeeStatus.Suspended or EmployeeStatus.Terminated,
            EmployeeStatus.Suspended => requestedStatus is EmployeeStatus.Active or EmployeeStatus.Terminated,
            EmployeeStatus.Terminated => false,
            _ => false
        };
    }

    private async Task<bool> WouldCreateCycleAsync(Guid employeeId, Guid proposedManagerId, CancellationToken ct)
    {
        var current = proposedManagerId;
        for (var depth = 0; depth < 64; depth++)
        {
            if (current == employeeId)
            {
                return true;
            }

            var managerId = await _employeeRepository.GetManagerIdAsync(current, ct);
            if (!managerId.HasValue)
            {
                return false;
            }

            current = managerId.Value;
        }

        return true;
    }

    private static string GenerateTemporaryPassword()
    {
        var randomNumber = Random.Shared.Next(100_000, 999_999);
        return $"Emp!{randomNumber}A";
    }

    private static string BuildIdentityErrorMessage(IEnumerable<IdentityError> errors)
    {
        var messages = errors.Select(e => e.Description).Where(m => !string.IsNullOrWhiteSpace(m)).ToArray();
        return messages.Length == 0 ? "Identity operation failed." : string.Join(" ", messages);
    }
}
