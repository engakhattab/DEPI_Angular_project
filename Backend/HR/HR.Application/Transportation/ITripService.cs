using HR.Application.DTOs.Transportation;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Application.Transportation;

public interface ITripService
{
    Task<Result<PagedList<TripResponse>>> GetTripsAsync(
        Guid requesterEmployeeId,
        Guid? travelerEmployeeId,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<Result<TripResponse>> GetTripByIdAsync(
        Guid requesterEmployeeId,
        Guid id,
        CancellationToken ct);

    Task<Result<TripResponse>> CreateTripAsync(
        Guid requesterEmployeeId,
        TripCreateRequest request,
        CancellationToken ct);

    Task<Result> DeleteTripAsync(
        Guid requesterEmployeeId,
        Guid id,
        CancellationToken ct);
}
