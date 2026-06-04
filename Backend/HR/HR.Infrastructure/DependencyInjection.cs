using HR.Application.Auth;
using HR.Application.Departments;
using HR.Application.Employees;
using HR.Application.Transportation;
using HR.Application.VacationRequests;
using HR.Infrastructure.Auth;
using HR.Infrastructure.Data;
using HR.Infrastructure.Departments;
using HR.Infrastructure.Employees;
using HR.Infrastructure.Identity;
using HR.Infrastructure.Repositories;
using HR.Infrastructure.Transportation;
using HR.Infrastructure.VacationRequests;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HR.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<BusinessRules.WorkingDayCalendar>();
        services.AddScoped<IEmployeeSessionValidator, EmployeeSessionValidator>();
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
