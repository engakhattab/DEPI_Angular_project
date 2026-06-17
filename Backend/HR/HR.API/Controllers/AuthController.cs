using System.Security.Claims;
using HR.API.Documentation;
using HR.Application.DTOs.Auth;
using HR.Application.DTOs.Employees;
using HR.Application.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var identifier = request.Identifier.Trim();
        if (string.IsNullOrEmpty(identifier))
        {
            return Unauthorized(new { code = "VALIDATION", message = "Invalid credentials." });
        }

        var result = await _authService.ValidateCredentialsAsync(identifier, request.Password, ct);
        if (!result.IsSuccess)
        {
            return Unauthorized(new
            {
                code = result.Error!.Code,
                message = result.Error.Message
            });
        }

        var employee = result.Value!.Employee;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.Value.UserId),
            new(ClaimTypes.Email, result.Value.UserEmail ?? string.Empty),
            new("employee_id", employee.Id.ToString()),
            new("employee_number", employee.EmployeeNumber),
            new("full_name", employee.FullName),
            new(ClaimTypes.Role, employee.Role.ToString()),
            new("employee_role", employee.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        var response = new LoginResponse { Employee = employee };

        return Ok(response);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me()
    {
        var employeeIdClaim = User.FindFirstValue("employee_id");
        if (!Guid.TryParse(employeeIdClaim, out var employeeId))
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        return Ok(new CurrentUserResponse
        {
            EmployeeId = employeeId,
            FullName = User.FindFirstValue("full_name") ?? string.Empty,
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            Role = Enum.TryParse<HR.Domain.Enums.EmployeeRole>(User.FindFirstValue("employee_role"), out var role)
                ? role
                : HR.Domain.Enums.EmployeeRole.Employee
        });
    }
}
