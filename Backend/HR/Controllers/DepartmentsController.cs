using HR.Data;
using HR.DTOs.Departments;
using HR.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController(ApplicationDbContext context) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DepartmentResponse>>> GetDepartments(CancellationToken cancellationToken)
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return departments.Select(MapToResponse).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DepartmentResponse>> GetDepartment(Guid id, CancellationToken cancellationToken)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (department is null)
        {
            return NotFound();
        }

        return MapToResponse(department);
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentResponse>> CreateDepartment([FromBody] DepartmentCreateRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (await _context.Departments.AnyAsync(d => d.Name == request.Name, cancellationToken))
        {
            return Conflict($"Department '{request.Name}' already exists.");
        }

        var department = new Department { Name = request.Name };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, MapToResponse(department));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DepartmentResponse>> UpdateDepartment(Guid id, [FromBody] DepartmentUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (department is null)
        {
            return NotFound();
        }

        if (await _context.Departments.AnyAsync(d => d.Id != id && d.Name == request.Name, cancellationToken))
        {
            return Conflict($"Department '{request.Name}' already exists.");
        }

        department.Name = request.Name;
        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(department);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDepartment(Guid id, CancellationToken cancellationToken)
    {
        var department = await _context.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (department is null)
        {
            return NotFound();
        }

        if (department.Employees.Any())
        {
            return Conflict("Cannot delete a department that still has employees assigned.");
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static DepartmentResponse MapToResponse(Department department)
    {
        return new DepartmentResponse
        {
            Id = department.Id,
            Name = department.Name
        };
    }
}
