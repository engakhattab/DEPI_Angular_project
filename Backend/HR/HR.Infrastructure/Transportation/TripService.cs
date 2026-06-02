using HR.Application.DTOs.Transportation;
using HR.Application.Transportation;
using HR.Domain.Entities;
using HR.Infrastructure.Repositories;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Infrastructure.Transportation;

public class TripService(
    ITripRepository tripRepository,
    IUnitOfWork unitOfWork) : ITripService
{
    private readonly ITripRepository _tripRepository = tripRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<PagedList<TripResponse>> GetTripsAsync(int page, int pageSize, CancellationToken ct)
    {
        var pagedEntities = await _tripRepository.GetPageAsync(page, pageSize, ct);
        var items = pagedEntities.Items.Select(TripResponse.FromEntity).ToList();

        return new PagedList<TripResponse>(items, pagedEntities.TotalCount, pagedEntities.Page, pagedEntities.PageSize);
    }

    public async Task<TripResponse?> GetTripByIdAsync(Guid id, CancellationToken ct)
    {
        var trip = await _tripRepository.GetByIdAsync(id, ct);

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

        await _tripRepository.AddAsync(trip, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<TripResponse>.Success(TripResponse.FromEntity(trip));
    }

    public async Task<Result> DeleteTripAsync(Guid id, CancellationToken ct)
    {
        var trip = await _tripRepository.GetByIdAsync(id, ct);
        if (trip is null)
        {
            return Result.Failure(ServiceError.NotFound($"Trip '{id}' was not found.", "NOT_FOUND"));
        }

        _tripRepository.Remove(trip);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    private static string GenerateCode(string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"{prefix}-{suffix}";
    }
}
