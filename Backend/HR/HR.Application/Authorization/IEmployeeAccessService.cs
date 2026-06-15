using HR.Domain.Enums;
using HR.Shared.Results;

namespace HR.Application.Authorization;

public interface IEmployeeAccessService
{
    Task<Result<EmployeeAccessContext>> GetCurrentAsync(Guid employeeId, CancellationToken ct);
    bool IsSelf(Guid requesterEmployeeId, Guid targetEmployeeId);
    Task<bool> IsManagerOfAsync(Guid requesterEmployeeId, Guid targetEmployeeId, CancellationToken ct);
    Task<bool> CanAccessEmployeeAsync(Guid requesterEmployeeId, Guid targetEmployeeId, CancellationToken ct);
    Task<bool> CanAccessTeamDataAsync(Guid requesterEmployeeId, Guid targetEmployeeId, CancellationToken ct);
    Task<bool> HasAnyRoleAsync(Guid employeeId, CancellationToken ct, params EmployeeRole[] roles);
    Task<bool> IsHRAdministratorAsync(Guid employeeId, CancellationToken ct);
    Task<bool> IsSystemAdministratorAsync(Guid employeeId, CancellationToken ct);
    Task<bool> HasOrganizationScopeAsync(Guid employeeId, CancellationToken ct);
    Task<IReadOnlySet<Guid>> GetVisibleEmployeeIdsAsync(Guid requesterEmployeeId, CancellationToken ct);
}

public sealed record EmployeeAccessContext(
    Guid EmployeeId,
    EmployeeRole Role,
    bool IsActive,
    bool IsDeleted,
    bool IsTerminated);
