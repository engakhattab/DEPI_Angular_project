using HR.API.Extensions;
using HR.Application.Departments;
using HR.Application.DTOs.Departments;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DepartmentsController(IDepartmentService departmentService) : ControllerBase
{
    private readonly IDepartmentService _departmentService = departmentService;

    [HttpGet]
    public async Task<ActionResult<PagedList<DepartmentResponse>>> GetDepartments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await _departmentService.GetDepartmentsAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DepartmentResponse>> GetDepartment(Guid id, CancellationToken cancellationToken)
    {
        var department = await _departmentService.GetDepartmentByIdAsync(id, cancellationToken);
        if (department is null)
        {
            return this.ToActionResult(ServiceError.NotFound($"Department '{id}' was not found.", "NOT_FOUND"));
        }

        return Ok(department);
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentResponse>> CreateDepartment(
        [FromBody] DepartmentCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _departmentService.CreateDepartmentAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return CreatedAtAction(nameof(GetDepartment), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DepartmentResponse>> UpdateDepartment(
        Guid id,
        [FromBody] DepartmentUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _departmentService.UpdateDepartmentAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDepartment(Guid id, CancellationToken cancellationToken)
    {
        var result = await _departmentService.DeleteDepartmentAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return NoContent();
    }
}
