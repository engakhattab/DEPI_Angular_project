using HR.Application.Departments;
using HR.Application.DTOs.Departments;
using HR.Domain.Entities;
using HR.Infrastructure.Data;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Departments;

public class DepartmentService(ApplicationDbContext context) : IDepartmentService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<PagedList<DepartmentResponse>> GetDepartmentsAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name);

        var pagedEntities = await PagedList<Department>.CreateAsync(query, page, pageSize, ct);
        var items = pagedEntities.Items.Select(MapToResponse).ToList();

        return new PagedList<DepartmentResponse>(items, pagedEntities.TotalCount, pagedEntities.Page, pagedEntities.PageSize);
    }

    public async Task<DepartmentResponse?> GetDepartmentByIdAsync(Guid id, CancellationToken ct)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        return department is null ? null : MapToResponse(department);
    }

    public async Task<Result<DepartmentResponse>> CreateDepartmentAsync(DepartmentCreateRequest request, CancellationToken ct)
    {
        if (await _context.Departments.AnyAsync(d => d.Name == request.Name, ct))
        {
            return Result<DepartmentResponse>.Failure(
                ServiceError.Conflict($"Department '{request.Name}' already exists.", "CONFLICT"));
        }

        var department = new Department { Name = request.Name };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync(ct);

        return Result<DepartmentResponse>.Success(MapToResponse(department));
    }

    public async Task<Result<DepartmentResponse>> UpdateDepartmentAsync(Guid id, DepartmentUpdateRequest request, CancellationToken ct)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (department is null)
        {
            return Result<DepartmentResponse>.Failure(
                ServiceError.NotFound($"Department '{id}' was not found.", "NOT_FOUND"));
        }

        if (await _context.Departments.AnyAsync(d => d.Id != id && d.Name == request.Name, ct))
        {
            return Result<DepartmentResponse>.Failure(
                ServiceError.Conflict($"Department '{request.Name}' already exists.", "CONFLICT"));
        }

        department.Name = request.Name;
        await _context.SaveChangesAsync(ct);

        return Result<DepartmentResponse>.Success(MapToResponse(department));
    }

    public async Task<Result> DeleteDepartmentAsync(Guid id, CancellationToken ct)
    {
        var department = await _context.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (department is null)
        {
            return Result.Failure(ServiceError.NotFound($"Department '{id}' was not found.", "NOT_FOUND"));
        }

        if (department.Employees.Any())
        {
            return Result.Failure(
                ServiceError.Conflict("Cannot delete a department that still has employees assigned.", "CONFLICT"));
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
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
