using HR.Application.DTOs.VacationRequests;
using HR.Application.VacationRequests;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Repositories;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Infrastructure.VacationRequests;

public class VacationRequestService(
    IVacationRequestRepository vacationRequestRepository,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork) : IVacationRequestService
{
    private readonly IVacationRequestRepository _vacationRequestRepository = vacationRequestRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
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
                ServiceError.Validation("End date must be on or after the start date.", "VALIDATION"));
        }

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, ct);

        if (employee is null)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.NotFound($"Employee '{request.EmployeeId}' was not found.", "NOT_FOUND"));
        }

        var vacationRequest = new VacationRequest
        {
            EmployeeId = request.EmployeeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            Status = VacationRequestStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _vacationRequestRepository.AddAsync(vacationRequest, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        vacationRequest.Employee = employee;
        return Result<VacationRequestResponse>.Success(VacationRequestResponse.FromEntity(vacationRequest));
    }

    public async Task<Result<VacationRequestResponse>> UpdateVacationStatusAsync(
        Guid id,
        VacationRequestStatusUpdateRequest request,
        CancellationToken ct)
    {
        var vacationRequest = await _vacationRequestRepository.GetByIdAsync(id, ct);

        if (vacationRequest is null)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.NotFound($"Vacation request '{id}' was not found.", "NOT_FOUND"));
        }

        vacationRequest.Status = request.Status;
        vacationRequest.UpdatedAt = DateTimeOffset.UtcNow;
        vacationRequest.Employee = await _employeeRepository.GetByIdAsync(vacationRequest.EmployeeId, ct);

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

        _vacationRequestRepository.Remove(vacationRequest);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
