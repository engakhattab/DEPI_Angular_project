using HR.Application.Authorization;
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
    IEmployeeAccessService employeeAccessService,
    WorkingDayCalendar workingDayCalendar,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ITripService
{
    private readonly ITripRepository _tripRepository = tripRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IEmployeeAccessService _employeeAccessService = employeeAccessService;
    private readonly WorkingDayCalendar _workingDayCalendar = workingDayCalendar;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<PagedList<TripResponse>>> GetTripsAsync(
        Guid requesterEmployeeId,
        Guid? travelerEmployeeId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var requesterResult = await GetValidatedRequesterAsync(requesterEmployeeId, ct);
        if (!requesterResult.IsSuccess)
        {
            return Result<PagedList<TripResponse>>.Failure(requesterResult.Error!);
        }

        var requester = requesterResult.Value!;
        var allowedIds = await GetAllowedTravelerIdsAsync(requester, ct);
        var hasOrgScope = await _employeeAccessService.HasOrganizationScopeAsync(requester.Id, ct);

        if (travelerEmployeeId.HasValue && !hasOrgScope && !allowedIds.Contains(travelerEmployeeId.Value))
        {
            return Result<PagedList<TripResponse>>.Success(
                new PagedList<TripResponse>(new List<TripResponse>(), 0, page, pageSize));
        }

        var pagedEntities = hasOrgScope
            ? await _tripRepository.GetPageByTravelersAsync(null, travelerEmployeeId, page, pageSize, ct)
            : await _tripRepository.GetPageByTravelersAsync(allowedIds, travelerEmployeeId, page, pageSize, ct);
        var items = pagedEntities.Items.Select(TripResponse.FromEntity).ToList();

        var result = new PagedList<TripResponse>(items, pagedEntities.TotalCount, pagedEntities.Page, pagedEntities.PageSize);
        return Result<PagedList<TripResponse>>.Success(result);
    }

    public async Task<Result<TripResponse>> GetTripByIdAsync(
        Guid requesterEmployeeId,
        Guid id,
        CancellationToken ct)
    {
        var requesterResult = await GetValidatedRequesterAsync(requesterEmployeeId, ct);
        if (!requesterResult.IsSuccess)
        {
            return Result<TripResponse>.Failure(requesterResult.Error!);
        }

        var trip = await _tripRepository.GetByIdAsync(id, ct);
        if (trip is null)
        {
            return Result<TripResponse>.Failure(
                ServiceError.NotFound($"Trip '{id}' was not found.", "NOT_FOUND"));
        }

        var requester = requesterResult.Value!;
        var allowedIds = await GetAllowedTravelerIdsAsync(requester, ct);
        var hasOrgScope = await _employeeAccessService.HasOrganizationScopeAsync(requester.Id, ct);

        if (hasOrgScope)
        {
            return Result<TripResponse>.Success(TripResponse.FromEntity(trip));
        }

        if (trip.RequestedByEmployeeId is null || !allowedIds.Contains(trip.RequestedByEmployeeId.Value))
        {
            return Result<TripResponse>.Failure(
                ServiceError.Forbidden("You do not have permission to access this trip."));
        }

        return Result<TripResponse>.Success(TripResponse.FromEntity(trip));
    }

    public async Task<Result<TripResponse>> CreateTripAsync(
        Guid requesterEmployeeId,
        TripCreateRequest request,
        CancellationToken ct)
    {
        var requesterResult = await GetValidatedRequesterAsync(requesterEmployeeId, ct);
        if (!requesterResult.IsSuccess)
        {
            return Result<TripResponse>.Failure(requesterResult.Error!);
        }

        var requester = requesterResult.Value!;
        var allowedIds = await GetAllowedTravelerIdsAsync(requester, ct);
        var targetTravelerId = request.RequestedByEmployeeId;

        if (!allowedIds.Contains(targetTravelerId))
        {
            return Result<TripResponse>.Failure(
                ServiceError.Forbidden("You do not have permission to create a trip for this employee."));
        }

        var traveler = await _employeeRepository.GetByIdAsync(targetTravelerId, ct);
        if (traveler is null)
        {
            return Result<TripResponse>.Failure(
                ServiceError.NotFound($"Employee '{targetTravelerId}' was not found.", "NOT_FOUND"));
        }

        if (traveler.IsDeleted || traveler.Status != EmployeeStatus.Active)
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
            RequestedByEmployeeId = traveler.Id,
            RequestedBy = traveler,
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

    public async Task<Result> DeleteTripAsync(
        Guid requesterEmployeeId,
        Guid id,
        CancellationToken ct)
    {
        var requesterResult = await GetValidatedRequesterAsync(requesterEmployeeId, ct);
        if (!requesterResult.IsSuccess)
        {
            return Result.Failure(requesterResult.Error!);
        }

        var trip = await _tripRepository.GetTrackedByIdAsync(id, ct);
        if (trip is null)
        {
            return Result.Failure(ServiceError.NotFound($"Trip '{id}' was not found.", "NOT_FOUND"));
        }

        var requester = requesterResult.Value!;
        var allowedIds = await GetAllowedTravelerIdsAsync(requester, ct);
        var hasOrgScope = await _employeeAccessService.HasOrganizationScopeAsync(requester.Id, ct);

        if (!hasOrgScope && (trip.RequestedByEmployeeId is null || !allowedIds.Contains(trip.RequestedByEmployeeId.Value)))
        {
            return Result.Failure(
                ServiceError.Forbidden("You do not have permission to delete this trip."));
        }

        _tripRepository.Remove(trip);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    private async Task<Result<Employee>> GetValidatedRequesterAsync(Guid requesterEmployeeId, CancellationToken ct)
    {
        var requester = await _employeeRepository.GetByIdAsync(requesterEmployeeId, ct);
        if (requester is null)
        {
            return Result<Employee>.Failure(ServiceError.Unauthorized("Invalid session."));
        }

        if (requester.IsDeleted || requester.Status == EmployeeStatus.Terminated)
        {
            return Result<Employee>.Failure(ServiceError.Unauthorized("Invalid session."));
        }

        return Result<Employee>.Success(requester);
    }

    private async Task<IReadOnlySet<Guid>> GetAllowedTravelerIdsAsync(Employee requester, CancellationToken ct)
    {
        return await _employeeAccessService.GetVisibleEmployeeIdsAsync(requester.Id, ct);
    }

    private static string GenerateCode(string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"{prefix}-{suffix}";
    }
}
