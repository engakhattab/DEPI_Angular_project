using HR.Domain.Entities;
using HR.Shared.Pagination;

namespace HR.Infrastructure.Repositories;

public interface IEmployeeDocumentRepository
{
    Task<EmployeeDocument?> GetCurrentByIdAsync(Guid employeeId, Guid documentId, CancellationToken ct);
    Task<PagedList<EmployeeDocument>> GetCurrentPageAsync(Guid employeeId, int page, int pageSize, CancellationToken ct);
    Task AddAsync(EmployeeDocument document, CancellationToken ct);
}
