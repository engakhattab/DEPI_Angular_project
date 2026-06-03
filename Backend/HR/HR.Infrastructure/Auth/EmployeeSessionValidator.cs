using System.Security.Claims;
using HR.Infrastructure.Repositories;

namespace HR.Infrastructure.Auth;

public class EmployeeSessionValidator(IEmployeeRepository employeeRepository) : IEmployeeSessionValidator
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;

    public async Task<bool> IsValidAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var employeeIdClaim = principal.FindFirstValue("employee_id");
        if (!Guid.TryParse(employeeIdClaim, out var employeeId))
        {
            return false;
        }

        return await _employeeRepository.IsAuthenticationEligibleAsync(employeeId, ct);
    }
}
