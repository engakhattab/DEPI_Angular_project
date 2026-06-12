using HR.Application.DTOs.Compensation;
using HR.Shared.Results;

namespace HR.Application.Compensation;

public interface ICompensationService
{
    Task<Result<CompensationResponse>> GetAsync(Guid requesterEmployeeId, Guid employeeId, CancellationToken ct);
    Task<Result<CompensationResponse>> UpdateAsync(Guid requesterEmployeeId, Guid employeeId, CompensationUpdateRequest request, CancellationToken ct);
}
