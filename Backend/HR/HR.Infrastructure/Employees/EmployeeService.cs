using HR.Application.DTOs.Employees;
using HR.Application.Employees;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Data;
using HR.Infrastructure.Identity;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Employees;

public class EmployeeService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager) : IEmployeeService
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<PagedList<EmployeeResponse>> GetEmployeesAsync(
        EmployeeStatus? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        IQueryable<Employee> query = _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Manager);

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        query = query.OrderBy(e => e.EmployeeNumber);

        var pagedEmployees = await PagedList<Employee>.CreateAsync(query, page, pageSize, ct);
        var userIds = pagedEmployees.Items.Select(e => e.ApplicationUserId).Distinct().ToList();
        var users = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(ct);

        var userDict = users.ToDictionary(u => u.Id);
        var items = pagedEmployees.Items
            .Select(e => MapToResponse(e, userDict.GetValueOrDefault(e.ApplicationUserId)))
            .ToList();

        return new PagedList<EmployeeResponse>(items, pagedEmployees.TotalCount, pagedEmployees.Page, pagedEmployees.PageSize);
    }

    public async Task<EmployeeResponse?> GetEmployeeByIdAsync(Guid id, CancellationToken ct)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (employee is null)
        {
            return null;
        }

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == employee.ApplicationUserId, ct);
        return MapToResponse(employee, user);
    }

    public async Task<Result<EmployeeCreatedResponse>> CreateEmployeeAsync(EmployeeCreateRequest request, CancellationToken ct)
    {
        if (await _context.Employees.AnyAsync(e => e.EmployeeNumber == request.EmployeeNumber, ct))
        {
            return Result<EmployeeCreatedResponse>.Failure(
                ServiceError.Conflict($"Employee number '{request.EmployeeNumber}' already exists.", "CONFLICT"));
        }

        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId, ct);
        if (department is null)
        {
            return Result<EmployeeCreatedResponse>.Failure(
                ServiceError.NotFound($"Department '{request.DepartmentId}' was not found.", "NOT_FOUND"));
        }

        Employee? manager = null;
        if (request.ManagerId.HasValue)
        {
            manager = await _context.Employees.FirstOrDefaultAsync(e => e.Id == request.ManagerId.Value, ct);
            if (manager is null)
            {
                return Result<EmployeeCreatedResponse>.Failure(
                    ServiceError.NotFound($"Manager '{request.ManagerId}' was not found.", "NOT_FOUND"));
            }
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

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        ServiceError? failure = null;
        EmployeeResponse? employeeResponse = null;

        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            var identityResult = await _userManager.CreateAsync(user, password);
            if (!identityResult.Succeeded)
            {
                failure = ServiceError.Validation(BuildIdentityErrorMessage(identityResult.Errors), "VALIDATION");
                await transaction.RollbackAsync(ct);
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
                ApplicationUserId = user.Id,
                Department = department,
                Manager = manager
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            employeeResponse = MapToResponse(employee, user);
        });

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
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (employee is null)
        {
            return Result<EmployeeResponse>.Failure(
                ServiceError.NotFound($"Employee '{id}' was not found.", "NOT_FOUND"));
        }

        if (request.ManagerId.HasValue && request.ManagerId.Value == id)
        {
            return Result<EmployeeResponse>.Failure(
                ServiceError.Validation("An employee cannot be their own manager.", "VALIDATION"));
        }

        var departmentExists = await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId, ct);
        if (!departmentExists)
        {
            return Result<EmployeeResponse>.Failure(
                ServiceError.NotFound($"Department '{request.DepartmentId}' was not found.", "NOT_FOUND"));
        }

        if (request.ManagerId.HasValue)
        {
            var managerExists = await _context.Employees.AnyAsync(e => e.Id == request.ManagerId.Value, ct);
            if (!managerExists)
            {
                return Result<EmployeeResponse>.Failure(
                    ServiceError.NotFound($"Manager '{request.ManagerId}' was not found.", "NOT_FOUND"));
            }
        }

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

        var user = await _userManager.FindByIdAsync(employee.ApplicationUserId);
        if (user is not null && !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = request.Email;
            user.UserName = request.Email;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return Result<EmployeeResponse>.Failure(
                    ServiceError.Validation(BuildIdentityErrorMessage(updateResult.Errors), "VALIDATION"));
            }
        }

        await _context.SaveChangesAsync(ct);
        await _context.Entry(employee).Reference(e => e.Department).LoadAsync(ct);
        if (employee.ManagerId.HasValue)
        {
            await _context.Entry(employee).Reference(e => e.Manager).LoadAsync(ct);
        }

        user = await _userManager.FindByIdAsync(employee.ApplicationUserId);
        return Result<EmployeeResponse>.Success(MapToResponse(employee, user));
    }

    public async Task<Result> DeleteEmployeeAsync(Guid id, CancellationToken ct)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (employee is null)
        {
            return Result.Failure(ServiceError.NotFound($"Employee '{id}' was not found.", "NOT_FOUND"));
        }

        var directReports = await _context.Employees
            .Where(e => e.ManagerId == id)
            .ToListAsync(ct);

        var vacationRequests = await _context.VacationRequests
            .Where(v => v.EmployeeId == id)
            .ToListAsync(ct);

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        foreach (var report in directReports)
        {
            report.ManagerId = null;
        }

        if (vacationRequests.Count > 0)
        {
            _context.VacationRequests.RemoveRange(vacationRequests);
        }

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync(ct);

        var user = await _userManager.FindByIdAsync(employee.ApplicationUserId);
        if (user is not null)
        {
            var deleteUserResult = await _userManager.DeleteAsync(user);
            if (!deleteUserResult.Succeeded)
            {
                await transaction.RollbackAsync(ct);
                return Result.Failure(
                    ServiceError.Validation(BuildIdentityErrorMessage(deleteUserResult.Errors), "VALIDATION"));
            }
        }

        await transaction.CommitAsync(ct);
        return Result.Success();
    }

    private static EmployeeResponse MapToResponse(Employee employee, ApplicationUser? user)
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
            IdentityUserId = employee.ApplicationUserId,
            UserName = user?.UserName ?? string.Empty
        };
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
