using HR.Application.DTOs.VacationRequests;
using HR.Application.VacationRequests;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.BusinessRules;
using HR.Infrastructure.Repositories;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Infrastructure.VacationRequests;

public class VacationRequestService(
    IVacationRequestRepository vacationRequestRepository,
    IEmployeeRepository employeeRepository,
    WorkingDayCalendar workingDayCalendar,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IVacationRequestService
{
    private readonly IVacationRequestRepository _vacationRequestRepository = vacationRequestRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly WorkingDayCalendar _workingDayCalendar = workingDayCalendar;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<PagedList<VacationRequestResponse>> GetVacationRequestsAsync(
        VacationRequestStatus? status,
        Guid? employeeId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var pagedEntities = await _vacationRequestRepository.GetPageWithEmployeeAsync(status, employeeId, page, pageSize, ct);
        var items = pagedEntities.Items.Select(VacationRequestResponse.FromEntity).ToList();

        return new PagedList<VacationRequestResponse>(items, pagedEntities.TotalCount, pagedEntities.Page, pagedEntities.PageSize);
    }

    public async Task<VacationRequestResponse?> GetVacationRequestByIdAsync(Guid id, CancellationToken ct)
    {
        var request = await _vacationRequestRepository.GetByIdWithEmployeeAsync(id, ct);

        return request is null ? null : VacationRequestResponse.FromEntity(request);
    }

    public async Task<Result<VacationRequestResponse>> CreateVacationRequestAsync(VacationRequestCreateRequest request, CancellationToken ct)
    {
        if (request.StartDate > request.EndDate)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.BusinessRule("End date must be on or after the start date."));
        }

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, ct);

        if (employee is null)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.NotFound($"Employee '{request.EmployeeId}' was not found.", "NOT_FOUND"));
        }

        if (employee.IsDeleted || employee.Status != EmployeeStatus.Active)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.BusinessRule("Only active employees can submit vacation requests."));
        }

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        if (request.StartDate < today)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.BusinessRule("Vacation start date cannot be in the past."));
        }

        var noticeDays = _workingDayCalendar.CountFullWorkingDaysBetween(today, request.StartDate);
        if (noticeDays < 3)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.BusinessRule("Vacation requests must be submitted at least three full working days in advance."));
        }

        var workingDayCount = _workingDayCalendar.CountInclusiveWorkingDays(request.StartDate, request.EndDate);
        if (workingDayCount > employee.VacationBalanceDays)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.BusinessRule("Requested vacation exceeds the employee's available balance."));
        }

        var hasOverlap = await _vacationRequestRepository.HasOverlappingPendingOrApprovedAsync(
            request.EmployeeId,
            request.StartDate,
            request.EndDate,
            ct);
        if (hasOverlap)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.BusinessRule("Employee already has a pending or approved vacation request that overlaps the requested dates."));
        }

        var vacationRequest = new VacationRequest
        {
            EmployeeId = request.EmployeeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            Status = VacationRequestStatus.Pending,
            WorkingDayCount = workingDayCount,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _vacationRequestRepository.AddAsync(vacationRequest, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        vacationRequest.Employee = employee;
        return Result<VacationRequestResponse>.Success(VacationRequestResponse.FromEntity(vacationRequest));
    }

    public async Task<Result<VacationRequestResponse>> UpdateVacationStatusAsync(
        Guid id,
        Guid reviewerEmployeeId,
        VacationRequestStatusUpdateRequest request,
        CancellationToken ct)
    {
        var vacationRequest = await _vacationRequestRepository.GetTrackedByIdWithEmployeeAndReviewerAsync(id, ct);

        if (vacationRequest is null)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.NotFound($"Vacation request '{id}' was not found.", "NOT_FOUND"));
        }

        if (vacationRequest.EmployeeId == reviewerEmployeeId)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.BusinessRule("Employees cannot approve or reject their own vacation requests."));
        }

        if (vacationRequest.Status == request.Status)
        {
            return Result<VacationRequestResponse>.Success(VacationRequestResponse.FromEntity(vacationRequest));
        }

        if (!IsAllowedTransition(vacationRequest.Status, request.Status))
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.BusinessRule(
                    $"Cannot transition a vacation request from '{vacationRequest.Status}' to '{request.Status}'."));
        }

        var reviewer = await _employeeRepository.GetByIdAsync(reviewerEmployeeId, ct);
        if (reviewer is null || reviewer.IsDeleted || reviewer.Status == EmployeeStatus.Terminated)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.Unauthorized("Authenticated employee context is invalid."));
        }

        var employee = vacationRequest.Employee ?? await _employeeRepository.GetByIdAsync(vacationRequest.EmployeeId, ct);
        if (employee is null)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.NotFound($"Employee '{vacationRequest.EmployeeId}' was not found.", "NOT_FOUND"));
        }

        if (vacationRequest.Status == VacationRequestStatus.Pending
            && request.Status == VacationRequestStatus.Approved
            && employee.VacationBalanceDays < vacationRequest.WorkingDayCount)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.BusinessRule("Requested vacation exceeds the employee's available balance."));
        }

        if (vacationRequest.Status == VacationRequestStatus.Pending
            && request.Status == VacationRequestStatus.Approved)
        {
            employee.VacationBalanceDays -= vacationRequest.WorkingDayCount;
        }
        else if (vacationRequest.Status == VacationRequestStatus.Approved
                 && request.Status == VacationRequestStatus.Rejected)
        {
            employee.VacationBalanceDays += vacationRequest.WorkingDayCount;
        }

        vacationRequest.Status = request.Status;
        vacationRequest.Employee = employee;
        vacationRequest.ReviewedByEmployeeId = reviewerEmployeeId;
        vacationRequest.ReviewedBy = reviewer;
        vacationRequest.ReviewedAt = _timeProvider.GetUtcNow();
        vacationRequest.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<VacationRequestResponse>.Success(VacationRequestResponse.FromEntity(vacationRequest));
    }

    public async Task<Result> DeleteVacationRequestAsync(Guid id, CancellationToken ct)
    {
        var vacationRequest = await _vacationRequestRepository.GetByIdAsync(id, ct);
        if (vacationRequest is null)
        {
            return Result.Failure(ServiceError.NotFound($"Vacation request '{id}' was not found.", "NOT_FOUND"));
        }

        if (vacationRequest.Status != VacationRequestStatus.Pending)
        {
            return Result.Failure(ServiceError.BusinessRule("Only pending vacation requests may be deleted."));
        }

        _vacationRequestRepository.Remove(vacationRequest);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    private static bool IsAllowedTransition(VacationRequestStatus currentStatus, VacationRequestStatus requestedStatus)
    {
        return currentStatus switch
        {
            VacationRequestStatus.Pending => requestedStatus is VacationRequestStatus.Approved or VacationRequestStatus.Rejected,
            VacationRequestStatus.Approved => requestedStatus == VacationRequestStatus.Rejected,
            VacationRequestStatus.Rejected => false,
            _ => false
        };
    }
}
