using HR.Domain.Entities;
using HR.Shared.Pagination;

namespace HR.Infrastructure.Repositories;

public interface ITripRepository
{
    Task<PagedList<Trip>> GetPageAsync(int page, int pageSize, CancellationToken ct);
    Task<Trip?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Trip trip, CancellationToken ct);
    void Remove(Trip trip);
}
