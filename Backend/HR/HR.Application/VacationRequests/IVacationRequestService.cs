using HR.Application.DTOs.VacationRequests;
using HR.Domain.Enums;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Application.VacationRequests;

public interface IVacationRequestService
{
    Task<PagedList<VacationRequestResponse>> GetVacationRequestsAsync(
        VacationRequestStatus? status,
        Guid? employeeId,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<VacationRequestResponse?> GetVacationRequestByIdAsync(Guid id, CancellationToken ct);
    Task<Result<VacationRequestResponse>> CreateVacationRequestAsync(VacationRequestCreateRequest request, CancellationToken ct);
    Task<Result<VacationRequestResponse>> UpdateVacationStatusAsync(Guid id, VacationRequestStatusUpdateRequest request, CancellationToken ct);
    Task<Result> DeleteVacationRequestAsync(Guid id, CancellationToken ct);
}
