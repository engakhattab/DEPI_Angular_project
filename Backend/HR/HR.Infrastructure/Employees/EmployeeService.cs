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

    public async Task<PagedList<EmployeeResponse>> GetEmployeesAsync(
        EmployeeStatus? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var pagedEmployees = await _employeeRepository.GetPageWithDetailsAsync(status, page, pageSize, ct);
        var userIds = pagedEmployees.Items.Select(e => e.ApplicationUserId).Distinct().ToList();
        var userDict = await _identityUserLookup.GetByIdsAsync(userIds, ct);
        var items = pagedEmployees.Items
            .Select(e => MapToResponse(e, userDict.GetValueOrDefault(e.ApplicationUserId)))
            .ToList();

        return new PagedList<EmployeeResponse>(items, pagedEmployees.TotalCount, pagedEmployees.Page, pagedEmployees.PageSize);
    }

    public async Task<EmployeeResponse?> GetEmployeeByIdAsync(Guid id, CancellationToken ct)
    {
        var employee = await _employeeRepository.GetByIdWithDetailsAsync(id, ct);
        if (employee is null)
        {
            return null;
        }

        var user = await _identityUserLookup.GetByIdAsync(employee.ApplicationUserId, ct);
        return MapToResponse(employee, user);
    }

    public async Task<Result<EmployeeCreatedResponse>> CreateEmployeeAsync(EmployeeCreateRequest request, CancellationToken ct)
    {
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

    public async Task<Result<EmployeeResponse>> UpdateEmployeeAsync(Guid id, EmployeeUpdateRequest request, CancellationToken ct)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, ct);
        if (employee is null || employee.IsDeleted)
        {
            return Result<EmployeeResponse>.Failure(
                ServiceError.NotFound($"Employee '{id}' was not found.", "NOT_FOUND"));
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

        if (employee.Status != request.Status && !IsAllowedStatusTransition(employee.Status, request.Status))
        {
            return Result<EmployeeResponse>.Failure(
                ServiceError.BusinessRule(
                    $"Cannot transition an employee from '{employee.Status}' to '{request.Status}'."));
        }

        if (manager is not null && manager.DepartmentId != request.DepartmentId)
        {
            _logger.LogWarning(
                "Employee {EmployeeId} assigned manager {ManagerId} from a different department",
                id,
                manager.Id);
        }

        var shouldTerminate = employee.Status != EmployeeStatus.Terminated && request.Status == EmployeeStatus.Terminated;

        employee.FullName = request.FullName;
        employee.Email = request.Email;
        employee.DepartmentId = request.DepartmentId;
        employee.ManagerId = request.ManagerId;
        employee.BirthDate = request.BirthDate;
        employee.JoinDate = request.JoinDate;
        employee.JobTitle = request.JobTitle;
        employee.PhoneNumber = request.PhoneNumber;
        employee.Notes = request.Notes;
        employee.Status = request.Status;

        if (shouldTerminate)
        {
            employee.TerminatedAt ??= _timeProvider.GetUtcNow();

            var pendingRequests = await _vacationRequestRepository.GetPendingByEmployeeIdAsync(id, ct);
            foreach (var pendingRequest in pendingRequests)
            {
                pendingRequest.Status = VacationRequestStatus.Rejected;
                pendingRequest.UpdatedAt = _timeProvider.GetUtcNow();
            }
        }

        ApplicationUser? user = null;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);

        user = await _userManager.FindByIdAsync(employee.ApplicationUserId);
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

        employee.Department = department;
        employee.Manager = manager;
        user = await _userManager.FindByIdAsync(employee.ApplicationUserId);
        return Result<EmployeeResponse>.Success(MapToResponse(employee, user));
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

    public async Task<Result> DeleteEmployeeAsync(Guid id, CancellationToken ct)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, ct);
        if (employee is null)
        {
            return Result.Failure(ServiceError.NotFound($"Employee '{id}' was not found.", "NOT_FOUND"));
        }

        if (employee.IsDeleted)
        {
            return Result.Success();
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

        employee.IsDeleted = true;
        employee.Status = EmployeeStatus.Terminated;
        employee.TerminatedAt ??= _timeProvider.GetUtcNow();

        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result.Success();
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
