using HR.Application.DTOs.Transportation;
using HR.Application.Transportation;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.BusinessRules;
using HR.Infrastructure.Repositories;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Infrastructure.Transportation;

public class TripService(
    ITripRepository tripRepository,
    IEmployeeRepository employeeRepository,
    WorkingDayCalendar workingDayCalendar,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ITripService
{
    private readonly ITripRepository _tripRepository = tripRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly WorkingDayCalendar _workingDayCalendar = workingDayCalendar;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _timeProvider = timeProvider;

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
        var requester = await _employeeRepository.GetByIdAsync(request.RequestedByEmployeeId, ct);
        if (requester is null)
        {
            return Result<TripResponse>.Failure(
                ServiceError.NotFound($"Employee '{request.RequestedByEmployeeId}' was not found.", "NOT_FOUND"));
        }

        if (requester.IsDeleted || requester.Status != EmployeeStatus.Active)
        {
            return Result<TripResponse>.Failure(
                ServiceError.BusinessRule("Trips may only be requested for active, non-deleted employees."));
        }

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        if (request.TripDate < today)
        {
            return Result<TripResponse>.Failure(
                ServiceError.BusinessRule("Trips cannot be scheduled in the past."));
        }

        if (!_workingDayCalendar.IsWorkingDay(request.TripDate))
        {
            return Result<TripResponse>.Failure(
                ServiceError.BusinessRule("Trips must be scheduled on a working day."));
        }

        var trip = new Trip
        {
            RequestedByEmployeeId = requester.Id,
            RequestedBy = requester,
            ReferenceName = request.ReferenceName,
            Project = request.Project,
            Route = request.Route,
            TripType = request.TripType,
            TripDate = request.TripDate,
            TripCode = GenerateCode("TRIP"),
            RequestCode = GenerateCode("REQ"),
            CreatedAt = _timeProvider.GetUtcNow()
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
