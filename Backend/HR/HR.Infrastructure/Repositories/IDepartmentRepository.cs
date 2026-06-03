using HR.Domain.Entities;
using HR.Shared.Pagination;

namespace HR.Infrastructure.Repositories;

public interface IDepartmentRepository
{
    Task<PagedList<Department>> GetPageAsync(int page, int pageSize, CancellationToken ct);
    Task<PagedList<Department>> GetPageWithEmployeeCountsAsync(int page, int pageSize, CancellationToken ct);
    Task<Department?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Department?> GetByIdWithEmployeeCountAsync(Guid id, CancellationToken ct);
    Task<Department?> GetByIdWithEmployeesAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string name, Guid? excludingId, CancellationToken ct);
    Task AddAsync(Department department, CancellationToken ct);
    void Remove(Department department);
}
