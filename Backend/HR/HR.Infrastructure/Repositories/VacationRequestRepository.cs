using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Data;
using HR.Infrastructure.Data.Pagination;
using HR.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Repositories;

public class VacationRequestRepository(ApplicationDbContext context) : IVacationRequestRepository
{
    private readonly ApplicationDbContext _context = context;

    public Task<PagedList<VacationRequest>> GetPageWithEmployeeAsync(
        VacationRequestStatus? status,
        Guid? employeeId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        IQueryable<VacationRequest> query = _context.VacationRequests
            .AsNoTracking()
            .Include(v => v.Employee);

        if (status.HasValue)
        {
            query = query.Where(v => v.Status == status.Value);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(v => v.EmployeeId == employeeId.Value);
        }

        return PagedQueryExecutor.ExecuteDescendingAsync(query, v => v.CreatedAt, _context.Database, page, pageSize, ct);
    }

    public Task<VacationRequest?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return _context.VacationRequests.FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public Task<VacationRequest?> GetByIdWithEmployeeAsync(Guid id, CancellationToken ct)
    {
        return _context.VacationRequests
            .AsNoTracking()
            .Include(v => v.Employee)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<IReadOnlyList<VacationRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken ct)
    {
        return await _context.VacationRequests
            .Where(v => v.EmployeeId == employeeId)
            .ToListAsync(ct);
    }

    public Task AddAsync(VacationRequest request, CancellationToken ct)
    {
        return _context.VacationRequests.AddAsync(request, ct).AsTask();
    }

    public void Remove(VacationRequest request)
    {
        _context.VacationRequests.Remove(request);
    }

    public void RemoveRange(IEnumerable<VacationRequest> requests)
    {
        _context.VacationRequests.RemoveRange(requests);
    }
}
