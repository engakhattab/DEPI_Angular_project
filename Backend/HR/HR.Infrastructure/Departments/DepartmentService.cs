using HR.Application.Departments;
using HR.Application.DTOs.Departments;
using HR.Domain.Entities;
using HR.Infrastructure.Repositories;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Infrastructure.Departments;

public class DepartmentService(
    IDepartmentRepository departmentRepository,
    IUnitOfWork unitOfWork) : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<PagedList<DepartmentResponse>> GetDepartmentsAsync(int page, int pageSize, CancellationToken ct)
    {
        var pagedEntities = await _departmentRepository.GetPageAsync(page, pageSize, ct);
        var items = pagedEntities.Items.Select(MapToResponse).ToList();

        return new PagedList<DepartmentResponse>(items, pagedEntities.TotalCount, pagedEntities.Page, pagedEntities.PageSize);
    }

    public async Task<DepartmentResponse?> GetDepartmentByIdAsync(Guid id, CancellationToken ct)
    {
        var department = await _departmentRepository.GetByIdAsync(id, ct);
        return department is null ? null : MapToResponse(department);
    }

    public async Task<Result<DepartmentResponse>> CreateDepartmentAsync(DepartmentCreateRequest request, CancellationToken ct)
    {
        if (await _departmentRepository.ExistsByNameAsync(request.Name, null, ct))
        {
            return Result<DepartmentResponse>.Failure(
                ServiceError.Conflict($"Department '{request.Name}' already exists.", "CONFLICT"));
        }

        var department = new Department { Name = request.Name };

        await _departmentRepository.AddAsync(department, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<DepartmentResponse>.Success(MapToResponse(department));
    }

    public async Task<Result<DepartmentResponse>> UpdateDepartmentAsync(Guid id, DepartmentUpdateRequest request, CancellationToken ct)
    {
        var department = await _departmentRepository.GetByIdAsync(id, ct);
        if (department is null)
        {
            return Result<DepartmentResponse>.Failure(
                ServiceError.NotFound($"Department '{id}' was not found.", "NOT_FOUND"));
        }

        if (await _departmentRepository.ExistsByNameAsync(request.Name, id, ct))
        {
            return Result<DepartmentResponse>.Failure(
                ServiceError.Conflict($"Department '{request.Name}' already exists.", "CONFLICT"));
        }

        department.Name = request.Name;
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<DepartmentResponse>.Success(MapToResponse(department));
    }

    public async Task<Result> DeleteDepartmentAsync(Guid id, CancellationToken ct)
    {
        var department = await _departmentRepository.GetByIdWithEmployeesAsync(id, ct);
        if (department is null)
        {
            return Result.Failure(ServiceError.NotFound($"Department '{id}' was not found.", "NOT_FOUND"));
        }

        if (department.Employees.Any())
        {
            return Result.Failure(
                ServiceError.Conflict("Cannot delete a department that still has employees assigned.", "CONFLICT"));
        }

        _departmentRepository.Remove(department);
        await _unitOfWork.SaveChangesAsync(ct);

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
