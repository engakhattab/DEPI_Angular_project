using HR.Domain.Entities;
using HR.Infrastructure.Data;
using HR.Infrastructure.Data.Pagination;
using HR.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Repositories;

public class TripRepository(ApplicationDbContext context) : ITripRepository
{
    private readonly ApplicationDbContext _context = context;

    public Task<PagedList<Trip>> GetPageAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = _context.Trips
            .AsNoTracking();

        return PagedQueryExecutor.ExecuteDescendingAsync(query, t => t.CreatedAt, _context.Database, page, pageSize, ct);
    }

    public Task<Trip?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return _context.Trips
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public Task AddAsync(Trip trip, CancellationToken ct)
    {
        return _context.Trips.AddAsync(trip, ct).AsTask();
    }

    public void Remove(Trip trip)
    {
        _context.Trips.Remove(trip);
    }
}
