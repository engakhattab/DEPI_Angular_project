using HR.Domain.Enums;

namespace HR.API.Extensions;

public static class AuthorizationPolicyExtensions
{
    public static IServiceCollection AddHrAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Employee", policy => policy.RequireAuthenticatedUser());
            options.AddPolicy("Manager", policy => policy.RequireRole(EmployeeRole.Manager.ToString(), EmployeeRole.HRAdministrator.ToString(), EmployeeRole.SystemAdministrator.ToString()));
            options.AddPolicy("HRAdministrator", policy => policy.RequireRole(EmployeeRole.HRAdministrator.ToString(), EmployeeRole.SystemAdministrator.ToString()));
            options.AddPolicy("SystemAdministrator", policy => policy.RequireRole(EmployeeRole.SystemAdministrator.ToString()));
        });

        return services;
    }
}
