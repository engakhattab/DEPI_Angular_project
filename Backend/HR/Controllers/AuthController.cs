using HR.Data;
using HR.DTOs.Auth;
using HR.DTOs.Employees;
using HR.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<AuthController> logger) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ILogger<AuthController> _logger = logger;

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var identifier = request.Identifier.Trim();
        if (string.IsNullOrEmpty(identifier))
        {
            return Unauthorized("Invalid credentials.");
        }

        ApplicationUser? user = null;
        Employee? employee = null;

        try
        {
            if (identifier.Contains('@', StringComparison.Ordinal))
            {
                user = await _userManager.FindByEmailAsync(identifier);
                if (user is not null)
                {
                    employee = await _context.Employees
                        .Include(e => e.Department)
                        .Include(e => e.Manager)
                        .Include(e => e.IdentityUser)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id, cancellationToken);
                }
            }

            if (user is null)
            {
                employee = await _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Manager)
                    .Include(e => e.IdentityUser)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmployeeNumber == identifier, cancellationToken);

                user = employee?.IdentityUser;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to look up user with identifier {Identifier}", identifier);
            return StatusCode(StatusCodes.Status500InternalServerError, "Unable to process login right now.");
        }

        if (user is null || employee is null)
        {
            return Unauthorized("Invalid credentials.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return Unauthorized("Invalid credentials.");
        }

        var response = new LoginResponse
        {
            Employee = MapToResponse(employee)
        };

        return Ok(response);
    }

    private static EmployeeResponse MapToResponse(Employee employee)
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
            UserName = employee.IdentityUser?.UserName ?? string.Empty
        };
    }
}
