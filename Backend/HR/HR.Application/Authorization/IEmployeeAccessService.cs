using HR.Domain.Enums;
using HR.Shared.Results;

namespace HR.Application.Authorization;

public interface IEmployeeAccessService
{
    Task<Result<EmployeeAccessContext>> GetCurrentAsync(Guid employeeId, CancellationToken ct);
    Task<bool> HasAnyRoleAsync(Guid employeeId, CancellationToken ct, params EmployeeRole[] roles);
    Task<bool> CanAccessEmployeeAsync(Guid requesterEmployeeId, Guid targetEmployeeId, CancellationToken ct);
    Task<IReadOnlySet<Guid>> GetVisibleEmployeeIdsAsync(Guid requesterEmployeeId, CancellationToken ct);
}

public sealed record EmployeeAccessContext(
    Guid EmployeeId,
    EmployeeRole Role,
    bool IsActive,
    bool IsDeleted,
    bool IsTerminated);
