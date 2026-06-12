using System.Security.Claims;

namespace HR.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetEmployeeId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("employee_id");
        return Guid.TryParse(value, out var employeeId) ? employeeId : null;
    }
}
