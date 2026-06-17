using HR.API.Documentation;
using HR.API.Extensions;
using HR.Application.DTOs.Employees;
using HR.Application.Employees;
using HR.Domain.Enums;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EmployeesController(IEmployeeService employeeService) : ControllerBase
{
    private readonly IEmployeeService _employeeService = employeeService;

    [HttpGet]
    [ProducesResponseType(typeof(PagedList<EmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<EmployeeResponse>>> GetEmployees(
        [FromQuery] EmployeeStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var requesterId = User.GetEmployeeId();
        if (!requesterId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        var result = await _employeeService.GetEmployeesAsync(requesterId.Value, status, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponse>> GetEmployee(Guid id, CancellationToken cancellationToken)
    {
        var requesterId = User.GetEmployeeId();
        if (!requesterId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        var result = await _employeeService.GetEmployeeByIdAsync(requesterId.Value, id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EmployeeCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EmployeeCreatedResponse>> CreateEmployee(
        [FromBody] EmployeeCreateRequest request,
        CancellationToken cancellationToken)
    {
        var requesterId = User.GetEmployeeId();
        if (!requesterId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _employeeService.CreateEmployeeAsync(requesterId.Value, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return CreatedAtAction(nameof(GetEmployee), new { id = result.Value!.Employee.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EmployeeResponse>> UpdateEmployee(
        Guid id,
        [FromBody] EmployeeUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var requesterId = User.GetEmployeeId();
        if (!requesterId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _employeeService.UpdateEmployeeAsync(requesterId.Value, id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteEmployee(Guid id, CancellationToken cancellationToken)
    {
        var requesterId = User.GetEmployeeId();
        if (!requesterId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        var result = await _employeeService.DeleteEmployeeAsync(requesterId.Value, id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return NoContent();
    }

    [HttpPut("{id:guid}/role")]
    [Authorize(Policy = "SystemAdministrator")]
    [ProducesResponseType(typeof(EmployeeRoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EmployeeRoleResponse>> UpdateRole(
        Guid id,
        [FromBody] EmployeeRoleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var requesterId = User.GetEmployeeId();
        if (!requesterId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        var result = await _employeeService.UpdateRoleAsync(requesterId.Value, id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Value);
    }
}
