using HR.Application.DTOs.Employees;
using HR.Domain.Enums;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Application.Employees;

public interface IEmployeeService
{
    Task<Result<PagedList<EmployeeResponse>>> GetEmployeesAsync(Guid requesterEmployeeId, EmployeeStatus? status, int page, int pageSize, CancellationToken ct);
    Task<Result<EmployeeResponse>> GetEmployeeByIdAsync(Guid requesterEmployeeId, Guid id, CancellationToken ct);
    Task<Result<EmployeeCreatedResponse>> CreateEmployeeAsync(Guid requesterEmployeeId, EmployeeCreateRequest request, CancellationToken ct);
    Task<Result<EmployeeResponse>> UpdateEmployeeAsync(Guid requesterEmployeeId, Guid id, EmployeeUpdateRequest request, CancellationToken ct);
    Task<Result<EmployeeRoleResponse>> UpdateRoleAsync(Guid requesterEmployeeId, Guid id, EmployeeRoleUpdateRequest request, CancellationToken ct);
    Task<Result> DeleteEmployeeAsync(Guid requesterEmployeeId, Guid id, CancellationToken ct);
}
