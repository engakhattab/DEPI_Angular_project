using HR.Application.DTOs.VacationRequests;
using HR.Domain.Enums;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Application.VacationRequests;

public interface IVacationRequestService
{
    Task<Result<PagedList<VacationRequestResponse>>> GetVacationRequestsAsync(
        Guid requesterEmployeeId,
        VacationRequestStatus? status,
        Guid? employeeId,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<Result<VacationRequestResponse>> GetVacationRequestByIdAsync(
        Guid requesterEmployeeId,
        Guid id,
        CancellationToken ct);

    Task<Result<VacationRequestResponse>> CreateVacationRequestAsync(
        Guid requesterEmployeeId,
        VacationRequestCreateRequest request,
        CancellationToken ct);

    Task<Result<VacationRequestResponse>> UpdateVacationStatusAsync(
        Guid id,
        Guid reviewerEmployeeId,
        VacationRequestStatusUpdateRequest request,
        CancellationToken ct);

    Task<Result> DeleteVacationRequestAsync(
        Guid requesterEmployeeId,
        Guid id,
        CancellationToken ct);
}
