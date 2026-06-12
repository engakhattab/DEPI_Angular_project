using HR.Application.Auth;
using HR.Application.Attendance;
using HR.Application.Audit;
using HR.Application.Authorization;
using HR.Application.Compensation;
using HR.Application.Departments;
using HR.Application.Documents;
using HR.Application.Dashboard;
using HR.Application.Employees;
using HR.Application.Transportation;
using HR.Application.VacationRequests;
using HR.Infrastructure.Attendance;
using HR.Infrastructure.Audit;
using HR.Infrastructure.Auth;
using HR.Infrastructure.Authorization;
using HR.Infrastructure.Compensation;
using HR.Infrastructure.Configuration;
using HR.Infrastructure.Data;
using HR.Infrastructure.Departments;
using HR.Infrastructure.Documents;
using HR.Infrastructure.Employees;
using HR.Infrastructure.FileStorage;
using HR.Infrastructure.Identity;
using HR.Infrastructure.Repositories;
using HR.Infrastructure.Dashboard;
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
        services
            .AddOptions<BusinessSettings>()
            .Bind(configuration.GetSection(BusinessSettings.SectionName))
            .Validate(s => !string.IsNullOrWhiteSpace(s.TimeZoneId), "BusinessSettings:TimeZoneId is required.")
            .Validate(s =>
            {
                try
                {
                    TimeZoneInfo.FindSystemTimeZoneById(s.TimeZoneId);
                    return true;
                }
                catch
                {
                    return false;
                }
            }, "BusinessSettings:TimeZoneId is invalid.")
            .ValidateOnStart();

        services
            .AddOptions<DocumentStorageOptions>()
            .Bind(configuration.GetSection(DocumentStorageOptions.SectionName))
            .Validate(s => !string.IsNullOrWhiteSpace(s.RootPath), "DocumentStorage:RootPath is required.")
            .Validate(s => s.MaxFileSizeBytes > 0, "DocumentStorage:MaxFileSizeBytes must be positive.")
            .Validate(s => s.AllowedExtensions.Length > 0, "DocumentStorage:AllowedExtensions must not be empty.")
            .ValidateOnStart();

        services.Configure<InitialAdminBootstrapOptions>(
            configuration.GetSection(InitialAdminBootstrapOptions.SectionName));

        services.AddScoped<BusinessRules.WorkingDayCalendar>();
        services.AddScoped<IBusinessTimeProvider, BusinessTimeProvider>();
        services.AddScoped<IEmployeeSessionValidator, EmployeeSessionValidator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IVacationRequestService, VacationRequestService>();
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IEmployeeAccessService, EmployeeAccessService>();
        services.AddScoped<InitialSystemAdminBootstrapper>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<ICompensationService, CompensationService>();
        services.AddScoped<IEmployeeDocumentStorage, LocalEmployeeDocumentStorage>();
        services.AddScoped<IEmployeeDocumentService, EmployeeDocumentService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IVacationRequestRepository, VacationRequestRepository>();
        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<ICompensationRepository, CompensationRepository>();
        services.AddScoped<IEmployeeDocumentRepository, EmployeeDocumentRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IIdentityUserLookup, IdentityUserLookup>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
