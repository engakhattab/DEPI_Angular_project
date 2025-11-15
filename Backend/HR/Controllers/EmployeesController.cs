using HR.Data;
using HR.DTOs.Employees;
using HR.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<EmployeesController> logger) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ILogger<EmployeesController> _logger = logger;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeResponse>>> GetEmployees([FromQuery] EmployeeStatus? status = null)
    {
        IQueryable<Employee> query = _context.Employees.AsNoTracking();

        query = query
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .Include(e => e.IdentityUser);

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        var employees = await query
            .OrderBy(e => e.EmployeeNumber)
            .ToListAsync();
        return employees.Select(MapToResponse).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeResponse>> GetEmployee(Guid id)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .Include(e => e.IdentityUser)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null)
        {
            return NotFound();
        }

        return MapToResponse(employee);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeCreatedResponse>> CreateEmployee([FromBody] EmployeeCreateRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (await _context.Employees.AnyAsync(e => e.EmployeeNumber == request.EmployeeNumber, cancellationToken))
        {
            return Conflict($"Employee number '{request.EmployeeNumber}' already exists.");
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.DepartmentId, cancellationToken);

        if (department is null)
        {
            return NotFound($"Department '{request.DepartmentId}' was not found.");
        }

        Employee? manager = null;
        if (request.ManagerId.HasValue)
        {
            manager = await _context.Employees
                .Include(e => e.IdentityUser)
                .FirstOrDefaultAsync(e => e.Id == request.ManagerId.Value, cancellationToken);

            if (manager is null)
            {
                return NotFound($"Manager '{request.ManagerId}' was not found.");
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
        EmployeeResponse? response = null;

        try
        {
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                var identityResult = await _userManager.CreateAsync(user, password);
                if (!identityResult.Succeeded)
                {
                    foreach (var error in identityResult.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }

                    await transaction.RollbackAsync(cancellationToken);
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
                    Manager = manager,
                    IdentityUser = user
                };

                user.Employee = employee;

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                response = MapToResponse(employee);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create employee {EmployeeNumber}", request.EmployeeNumber);
            return StatusCode(StatusCodes.Status500InternalServerError, "Unable to create the employee at this time.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (response is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Employee creation did not complete successfully.");
        }

        var createdResponse = new EmployeeCreatedResponse
        {
            Employee = response,
            TemporaryPassword = passwordWasGenerated ? password : null
        };

        return CreatedAtAction(nameof(GetEmployee), new { id = createdResponse.Employee.Id }, createdResponse);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmployeeResponse>> UpdateEmployee(Guid id, [FromBody] EmployeeUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var employee = await _context.Employees
            .Include(e => e.IdentityUser)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee is null)
        {
            return NotFound();
        }

        if (request.ManagerId.HasValue && request.ManagerId.Value == id)
        {
            return BadRequest("An employee cannot be their own manager.");
        }

        var departmentExists = await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId, cancellationToken);
        if (!departmentExists)
        {
            return NotFound($"Department '{request.DepartmentId}' was not found.");
        }

        if (request.ManagerId.HasValue && !await _context.Employees.AnyAsync(e => e.Id == request.ManagerId.Value, cancellationToken))
        {
            return NotFound($"Manager '{request.ManagerId}' was not found.");
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

        if (employee.IdentityUser is not null)
        {
            var user = employee.IdentityUser;

            if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = request.Email;
                user.UserName = request.Email;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }

                    return ValidationProblem(ModelState);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _context.Entry(employee).Reference(e => e.Department).LoadAsync(cancellationToken);
        if (employee.ManagerId.HasValue)
        {
            await _context.Entry(employee).Reference(e => e.Manager).LoadAsync(cancellationToken);
        }

        await _context.Entry(employee).Reference(e => e.IdentityUser).LoadAsync(cancellationToken);

        return MapToResponse(employee);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteEmployee(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .Include(e => e.IdentityUser)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee is null)
        {
            return NotFound();
        }

        var directReports = await _context.Employees
            .Where(e => e.ManagerId == id)
            .ToListAsync(cancellationToken);

        var vacationRequests = await _context.VacationRequests
            .Where(v => v.EmployeeId == id)
            .ToListAsync(cancellationToken);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (directReports.Count > 0)
            {
                foreach (var report in directReports)
                {
                    report.ManagerId = null;
                }
            }

            if (vacationRequests.Count > 0)
            {
                _context.VacationRequests.RemoveRange(vacationRequests);
            }

            if (employee.IdentityUser is not null)
            {
                var deleteUserResult = await _userManager.DeleteAsync(employee.IdentityUser);
                if (!deleteUserResult.Succeeded)
                {
                    foreach (var error in deleteUserResult.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }

                    await transaction.RollbackAsync(cancellationToken);
                    return ValidationProblem(ModelState);
                }
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete employee {EmployeeId}", id);
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(StatusCodes.Status500InternalServerError, "Unable to delete the employee at this time.");
        }

        return NoContent();
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

    private static string GenerateTemporaryPassword()
    {
        var randomNumber = Random.Shared.Next(100_000, 999_999);
        return $"Emp!{randomNumber}A";
    }
}
