using HR.Application.Auth;
using HR.Application.DTOs.Employees;
using HR.Domain.Entities;
using HR.Infrastructure.Identity;
using HR.Infrastructure.Repositories;
using HR.Shared.Results;
using Microsoft.AspNetCore.Identity;

namespace HR.Infrastructure.Auth;

public class AuthService(
    IEmployeeRepository employeeRepository,
    UserManager<ApplicationUser> userManager) : IAuthService
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<AuthenticatedEmployee>> ValidateCredentialsAsync(
        string identifier,
        string password,
        CancellationToken ct)
    {
        ApplicationUser? user = null;
        Employee? employee = null;

        if (identifier.Contains('@', StringComparison.Ordinal))
        {
            user = await _userManager.FindByEmailAsync(identifier);
            if (user is not null)
            {
                employee = await _employeeRepository.GetByApplicationUserIdWithDetailsAsync(user.Id, ct);
            }
        }

        if (user is null)
        {
            employee = await _employeeRepository.GetByEmployeeNumberWithDetailsAsync(identifier, ct);

            if (employee is not null)
            {
                user = await _userManager.FindByIdAsync(employee.ApplicationUserId);
            }
        }

        if (user is null || employee is null)
        {
            return Result<AuthenticatedEmployee>.Failure(
                ServiceError.Validation("Invalid credentials.", "VALIDATION"));
        }

        if (employee.IsDeleted || employee.Status == HR.Domain.Enums.EmployeeStatus.Terminated)
        {
            return Result<AuthenticatedEmployee>.Failure(
                ServiceError.Unauthorized("This employee account is no longer allowed to sign in."));
        }

        var valid = await _userManager.CheckPasswordAsync(user, password);
        if (!valid)
        {
            return Result<AuthenticatedEmployee>.Failure(
                ServiceError.Validation("Invalid credentials.", "VALIDATION"));
        }

        return Result<AuthenticatedEmployee>.Success(
            new AuthenticatedEmployee(
                MapToResponse(employee, user),
                user.Id,
                user.UserName,
                user.Email));
    }

    private static EmployeeResponse MapToResponse(Employee employee, ApplicationUser user)
    {
        return new EmployeeResponse
        {
            Id = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            FullName = employee.FullName,
            Email = employee.Email ?? user.Email ?? string.Empty,
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
            VacationBalanceDays = employee.VacationBalanceDays,
            IsDeleted = employee.IsDeleted,
            TerminatedAt = employee.TerminatedAt,
            IdentityUserId = user.Id,
            UserName = user.UserName ?? string.Empty
        };
    }
}
