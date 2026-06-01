using HR.Application.DTOs.Departments;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Application.Departments;

public interface IDepartmentService
{
    Task<PagedList<DepartmentResponse>> GetDepartmentsAsync(int page, int pageSize, CancellationToken ct);
    Task<DepartmentResponse?> GetDepartmentByIdAsync(Guid id, CancellationToken ct);
    Task<Result<DepartmentResponse>> CreateDepartmentAsync(DepartmentCreateRequest request, CancellationToken ct);
    Task<Result<DepartmentResponse>> UpdateDepartmentAsync(Guid id, DepartmentUpdateRequest request, CancellationToken ct);
    Task<Result> DeleteDepartmentAsync(Guid id, CancellationToken ct);
}
