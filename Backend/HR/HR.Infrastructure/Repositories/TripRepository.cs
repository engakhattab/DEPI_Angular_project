using HR.Domain.Entities;
using HR.Infrastructure.Data;
using HR.Infrastructure.Data.Pagination;
using HR.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Repositories;

public class TripRepository(ApplicationDbContext context) : ITripRepository
{
    private readonly ApplicationDbContext _context = context;

    public Task<PagedList<Trip>> GetPageByTravelersAsync(
        IReadOnlySet<Guid>? allowedTravelerIds,
        Guid? travelerEmployeeId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        IQueryable<Trip> query = _context.Trips
            .AsNoTracking()
            .Include(t => t.RequestedBy)
            .Include(t => t.Requester);

        if (allowedTravelerIds is not null)
        {
            query = query.Where(t => t.RequestedByEmployeeId != null && allowedTravelerIds.Contains(t.RequestedByEmployeeId.Value));
        }

        if (travelerEmployeeId.HasValue)
        {
            query = query.Where(t => t.RequestedByEmployeeId == travelerEmployeeId.Value);
        }

        return PagedQueryExecutor.ExecuteDescendingAsync(query, t => t.CreatedAt, _context.Database, page, pageSize, ct);
    }

    public Task<Trip?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return _context.Trips
            .AsNoTracking()
            .Include(t => t.RequestedBy)
            .Include(t => t.Requester)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public Task<Trip?> GetTrackedByIdAsync(Guid id, CancellationToken ct)
    {
        return _context.Trips
            .Include(t => t.RequestedBy)
            .Include(t => t.Requester)
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
