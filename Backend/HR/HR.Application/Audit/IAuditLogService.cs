using HR.Application.DTOs.Audit;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Application.Audit;

public interface IAuditLogService
{
    Task<Result<PagedList<AuditLogEntryResponse>>> SearchAsync(Guid requesterEmployeeId, AuditLogQueryRequest request, CancellationToken ct);
}
