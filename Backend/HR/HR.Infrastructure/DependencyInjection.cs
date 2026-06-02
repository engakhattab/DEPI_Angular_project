using HR.Application.Auth;
using HR.Application.Departments;
using HR.Application.Employees;
using HR.Application.Transportation;
using HR.Application.VacationRequests;
using HR.Infrastructure.Auth;
using HR.Infrastructure.Departments;
using HR.Infrastructure.Employees;
using HR.Infrastructure.Repositories;
using HR.Infrastructure.Transportation;
using HR.Infrastructure.VacationRequests;
using Microsoft.Extensions.DependencyInjection;

namespace HR.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IVacationRequestService, VacationRequestService>();
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IVacationRequestRepository, VacationRequestRepository>();
        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IIdentityUserLookup, IdentityUserLookup>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
