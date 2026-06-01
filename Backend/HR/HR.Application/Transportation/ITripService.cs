using HR.Application.DTOs.Transportation;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Application.Transportation;

public interface ITripService
{
    Task<PagedList<TripResponse>> GetTripsAsync(int page, int pageSize, CancellationToken ct);
    Task<TripResponse?> GetTripByIdAsync(Guid id, CancellationToken ct);
    Task<Result<TripResponse>> CreateTripAsync(TripCreateRequest request, CancellationToken ct);
    Task<Result> DeleteTripAsync(Guid id, CancellationToken ct);
}
