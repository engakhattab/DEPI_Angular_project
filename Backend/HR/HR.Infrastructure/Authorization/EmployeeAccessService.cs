using HR.Application.Authorization;
using HR.Domain.Enums;
using HR.Infrastructure.Repositories;
using HR.Shared.Results;

namespace HR.Infrastructure.Authorization;

public class EmployeeAccessService(IEmployeeRepository employeeRepository) : IEmployeeAccessService
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;

    public bool IsSelf(Guid requesterEmployeeId, Guid targetEmployeeId)
    {
        return requesterEmployeeId == targetEmployeeId;
    }

    public async Task<bool> IsManagerOfAsync(Guid requesterEmployeeId, Guid targetEmployeeId, CancellationToken ct)
    {
        var requester = await _employeeRepository.GetByIdAsync(requesterEmployeeId, ct);
        if (requester is null || requester.IsDeleted || requester.Status == EmployeeStatus.Terminated)
        {
            return false;
        }

        if (requester.Role != EmployeeRole.Manager)
        {
            return false;
        }

        var reports = await _employeeRepository.GetDirectAndIndirectReportIdsAsync(requesterEmployeeId, ct);
        return reports.Contains(targetEmployeeId);
    }

    public async Task<bool> CanAccessTeamDataAsync(Guid requesterEmployeeId, Guid targetEmployeeId, CancellationToken ct)
    {
        var requester = await _employeeRepository.GetByIdAsync(requesterEmployeeId, ct);
        if (requester is null || requester.IsDeleted || requester.Status == EmployeeStatus.Terminated)
        {
            return false;
        }

        if (requester.Role is EmployeeRole.HRAdministrator or EmployeeRole.SystemAdministrator)
        {
            return true;
        }

        if (requester.Role != EmployeeRole.Manager)
        {
            return false;
        }

        if (requester.Id == targetEmployeeId)
        {
            return true;
        }

        var reports = await _employeeRepository.GetDirectAndIndirectReportIdsAsync(requesterEmployeeId, ct);
        return reports.Contains(targetEmployeeId);
    }

    public async Task<bool> IsHRAdministratorAsync(Guid employeeId, CancellationToken ct)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct);
        return employee is not null
            && !employee.IsDeleted
            && employee.Status != EmployeeStatus.Terminated
            && employee.Role == EmployeeRole.HRAdministrator;
    }

    public async Task<bool> IsSystemAdministratorAsync(Guid employeeId, CancellationToken ct)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct);
        return employee is not null
            && !employee.IsDeleted
            && employee.Status != EmployeeStatus.Terminated
            && employee.Role == EmployeeRole.SystemAdministrator;
    }

    public async Task<bool> HasOrganizationScopeAsync(Guid employeeId, CancellationToken ct)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct);
        return employee is not null
            && !employee.IsDeleted
            && employee.Status != EmployeeStatus.Terminated
            && employee.Role is EmployeeRole.HRAdministrator or EmployeeRole.SystemAdministrator;
    }

    public async Task<Result<EmployeeAccessContext>> GetCurrentAsync(Guid employeeId, CancellationToken ct)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct);
        if (employee is null)
        {
            return Result<EmployeeAccessContext>.Failure(ServiceError.Unauthorized("Invalid session."));
        }

        return Result<EmployeeAccessContext>.Success(
            new EmployeeAccessContext(
                employee.Id,
                employee.Role,
                employee.Status == EmployeeStatus.Active,
                employee.IsDeleted,
                employee.Status == EmployeeStatus.Terminated));
    }

    public async Task<bool> HasAnyRoleAsync(Guid employeeId, CancellationToken ct, params EmployeeRole[] roles)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct);
        return employee is not null
            && !employee.IsDeleted
            && employee.Status != EmployeeStatus.Terminated
            && roles.Contains(employee.Role);
    }

    public async Task<bool> CanAccessEmployeeAsync(Guid requesterEmployeeId, Guid targetEmployeeId, CancellationToken ct)
    {
        var requester = await _employeeRepository.GetByIdAsync(requesterEmployeeId, ct);
        if (requester is null || requester.IsDeleted || requester.Status == EmployeeStatus.Terminated)
        {
            return false;
        }

        if (IsSelf(requesterEmployeeId, targetEmployeeId))
        {
            return true;
        }

        if (requester.Role is EmployeeRole.HRAdministrator or EmployeeRole.SystemAdministrator)
        {
            return true;
        }

        return await IsManagerOfAsync(requesterEmployeeId, targetEmployeeId, ct);
    }

    public async Task<IReadOnlySet<Guid>> GetVisibleEmployeeIdsAsync(Guid requesterEmployeeId, CancellationToken ct)
    {
        var requester = await _employeeRepository.GetByIdAsync(requesterEmployeeId, ct);
        if (requester is null || requester.IsDeleted || requester.Status == EmployeeStatus.Terminated)
        {
            return new HashSet<Guid>();
        }

        if (requester.Role is EmployeeRole.HRAdministrator or EmployeeRole.SystemAdministrator)
        {
            return new HashSet<Guid>((await _employeeRepository.GetAllActiveAsync(ct)).Select(e => e.Id));
        }

        if (requester.Role == EmployeeRole.Manager)
        {
            var reports = await _employeeRepository.GetDirectAndIndirectReportIdsAsync(requesterEmployeeId, ct);
            var visible = new HashSet<Guid>(reports) { requesterEmployeeId };
            return visible;
        }

        return new HashSet<Guid> { requesterEmployeeId };
    }
}
