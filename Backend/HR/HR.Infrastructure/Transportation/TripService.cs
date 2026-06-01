using HR.Application.DTOs.Transportation;
using HR.Application.Transportation;
using HR.Domain.Entities;
using HR.Infrastructure.Data;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Transportation;

public class TripService(ApplicationDbContext context) : ITripService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<PagedList<TripResponse>> GetTripsAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = _context.Trips
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt);

        var pagedEntities = await PagedList<Trip>.CreateAsync(query, page, pageSize, ct);
        var items = pagedEntities.Items.Select(TripResponse.FromEntity).ToList();

        return new PagedList<TripResponse>(items, pagedEntities.TotalCount, pagedEntities.Page, pagedEntities.PageSize);
    }

    public async Task<TripResponse?> GetTripByIdAsync(Guid id, CancellationToken ct)
    {
        var trip = await _context.Trips
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        return trip is null ? null : TripResponse.FromEntity(trip);
    }

    public async Task<Result<TripResponse>> CreateTripAsync(TripCreateRequest request, CancellationToken ct)
    {
        var trip = new Trip
        {
            ReferenceName = request.ReferenceName,
            Project = request.Project,
            Route = request.Route,
            TripType = request.TripType,
            TripDate = request.TripDate,
            TripCode = GenerateCode("TRIP"),
            RequestCode = GenerateCode("REQ"),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Trips.Add(trip);
        await _context.SaveChangesAsync(ct);

        return Result<TripResponse>.Success(TripResponse.FromEntity(trip));
    }

    public async Task<Result> DeleteTripAsync(Guid id, CancellationToken ct)
    {
        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (trip is null)
        {
            return Result.Failure(ServiceError.NotFound($"Trip '{id}' was not found.", "NOT_FOUND"));
        }

        _context.Trips.Remove(trip);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    private static string GenerateCode(string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"{prefix}-{suffix}";
    }
}
