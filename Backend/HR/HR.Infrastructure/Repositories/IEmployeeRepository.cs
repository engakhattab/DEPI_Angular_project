using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Shared.Pagination;

namespace HR.Infrastructure.Repositories;

public interface IEmployeeRepository
{
    Task<PagedList<Employee>> GetPageWithDetailsAsync(
        EmployeeStatus? status,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Employee?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct);
    Task<Employee?> GetByApplicationUserIdWithDetailsAsync(string applicationUserId, CancellationToken ct);
    Task<Employee?> GetByEmployeeNumberWithDetailsAsync(string employeeNumber, CancellationToken ct);
    Task<IReadOnlyList<Employee>> FindByEmailOrEmployeeNumberAsync(string identifier, CancellationToken ct);
    Task<IReadOnlyList<Employee>> GetDirectReportsAsync(Guid managerId, CancellationToken ct);
    Task<IReadOnlyList<Employee>> GetAllActiveAsync(CancellationToken ct);
    Task<IReadOnlySet<Guid>> GetDirectAndIndirectReportIdsAsync(Guid managerId, CancellationToken ct);
    Task<bool> AnyActiveSystemAdministratorAsync(CancellationToken ct);
    Task<bool> ExistsWithEmailAsync(string email, CancellationToken ct);
    Task<bool> ExistsActiveWithEmailAsync(string email, Guid? excludingEmployeeId, CancellationToken ct);
    Task<Guid?> GetManagerIdAsync(Guid employeeId, CancellationToken ct);
    Task<bool> IsAuthenticationEligibleAsync(Guid employeeId, CancellationToken ct);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByNumberAsync(string employeeNumber, CancellationToken ct);
    Task AddAsync(Employee employee, CancellationToken ct);
    void Remove(Employee employee);
}
