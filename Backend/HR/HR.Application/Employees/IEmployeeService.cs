using HR.Application.DTOs.Employees;
using HR.Domain.Enums;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Application.Employees;

public interface IEmployeeService
{
    Task<PagedList<EmployeeResponse>> GetEmployeesAsync(EmployeeStatus? status, int page, int pageSize, CancellationToken ct);
    Task<EmployeeResponse?> GetEmployeeByIdAsync(Guid id, CancellationToken ct);
    Task<Result<EmployeeCreatedResponse>> CreateEmployeeAsync(EmployeeCreateRequest request, CancellationToken ct);
    Task<Result<EmployeeResponse>> UpdateEmployeeAsync(Guid id, EmployeeUpdateRequest request, CancellationToken ct);
    Task<Result> DeleteEmployeeAsync(Guid id, CancellationToken ct);
}
