using HR.Application.Auth;
using HR.Domain.Entities;
using HR.Infrastructure.Data;
using HR.Infrastructure.Identity;
using HR.Shared.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Auth;

public class AuthService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager) : IAuthService
{
    private readonly ApplicationDbContext _context = context;
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
                employee = await _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Manager)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id, ct);
            }
        }

        if (user is null)
        {
            employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeNumber == identifier, ct);

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

        var valid = await _userManager.CheckPasswordAsync(user, password);
        if (!valid)
        {
            return Result<AuthenticatedEmployee>.Failure(
                ServiceError.Validation("Invalid credentials.", "VALIDATION"));
        }

        return Result<AuthenticatedEmployee>.Success(
            new AuthenticatedEmployee(
                employee,
                user.Id,
                user.UserName,
                user.Email));
    }
}
