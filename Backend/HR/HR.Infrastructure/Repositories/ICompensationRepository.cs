using HR.Domain.Entities;

namespace HR.Infrastructure.Repositories;

public interface ICompensationRepository
{
    Task<EmployeeCompensation?> GetByEmployeeIdAsync(Guid employeeId, CancellationToken ct);
    Task<IReadOnlyList<SalaryHistoryEntry>> GetHistoryAsync(Guid employeeId, CancellationToken ct);
    Task AddCompensationAsync(EmployeeCompensation compensation, CancellationToken ct);
    Task AddHistoryAsync(SalaryHistoryEntry historyEntry, CancellationToken ct);
}
