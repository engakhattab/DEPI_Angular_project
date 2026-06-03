using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Shared.Pagination;

namespace HR.Infrastructure.Repositories;

public interface IVacationRequestRepository
{
    Task<PagedList<VacationRequest>> GetPageWithEmployeeAsync(
        VacationRequestStatus? status,
        Guid? employeeId,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<VacationRequest?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<VacationRequest?> GetByIdWithEmployeeAsync(Guid id, CancellationToken ct);
    Task<VacationRequest?> GetTrackedByIdWithEmployeeAndReviewerAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<VacationRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken ct);
    Task<IReadOnlyList<VacationRequest>> GetPendingByEmployeeIdAsync(Guid employeeId, CancellationToken ct);
    Task<bool> HasOverlappingPendingOrApprovedAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct);
    Task AddAsync(VacationRequest request, CancellationToken ct);
    void Remove(VacationRequest request);
    void RemoveRange(IEnumerable<VacationRequest> requests);
}
