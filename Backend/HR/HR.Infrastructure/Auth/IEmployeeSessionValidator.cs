using System.Security.Claims;

namespace HR.Infrastructure.Auth;

public interface IEmployeeSessionValidator
{
    Task<bool> IsValidAsync(ClaimsPrincipal principal, CancellationToken ct);
}
