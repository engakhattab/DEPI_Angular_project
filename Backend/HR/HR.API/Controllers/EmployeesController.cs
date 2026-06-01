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
public class EmployeesController(IEmployeeService employeeService) : ControllerBase
{
    private readonly IEmployeeService _employeeService = employeeService;

    [HttpGet]
    public async Task<ActionResult<PagedList<EmployeeResponse>>> GetEmployees(
        [FromQuery] EmployeeStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await _employeeService.GetEmployeesAsync(status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeResponse>> GetEmployee(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetEmployeeByIdAsync(id, cancellationToken);
        if (employee is null)
        {
            return this.ToActionResult(ServiceError.NotFound($"Employee '{id}' was not found.", "NOT_FOUND"));
        }

        return Ok(employee);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeCreatedResponse>> CreateEmployee(
        [FromBody] EmployeeCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _employeeService.CreateEmployeeAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return CreatedAtAction(nameof(GetEmployee), new { id = result.Value!.Employee.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmployeeResponse>> UpdateEmployee(
        Guid id,
        [FromBody] EmployeeUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _employeeService.UpdateEmployeeAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteEmployee(Guid id, CancellationToken cancellationToken)
    {
        var result = await _employeeService.DeleteEmployeeAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return NoContent();
    }
}
