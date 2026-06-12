using HR.Application.DTOs.Dashboard;
using HR.Shared.Results;

namespace HR.Application.Dashboard;

public interface IDashboardService
{
    Task<Result<DashboardSummaryResponse>> GetSummaryAsync(Guid requesterEmployeeId, CancellationToken ct);
}
