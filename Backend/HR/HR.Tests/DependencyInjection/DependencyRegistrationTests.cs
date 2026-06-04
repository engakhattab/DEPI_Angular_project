using HR.Application;
using HR.Application.Auth;
using HR.Application.Departments;
using HR.Application.Employees;
using HR.Application.Transportation;
using HR.Application.VacationRequests;
using HR.Infrastructure;
using HR.Infrastructure.Auth;
using HR.Infrastructure.BusinessRules;
using HR.Infrastructure.Data;
using HR.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HR.Tests.DependencyInjection;

public class DependencyRegistrationTests
{
    [Fact]
    public void AddApplication_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var returned = services.AddApplication();

        Assert.Same(services, returned);
    }

    [Fact]
    public void AddInfrastructure_RegistersRepresentativeInfrastructureServices()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(CreateConfiguration());
        services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider;

        Assert.NotNull(scoped.GetRequiredService<ApplicationDbContext>());
        Assert.NotNull(scoped.GetRequiredService<UserManager<Infrastructure.Identity.ApplicationUser>>());
        Assert.NotNull(scoped.GetRequiredService<IAuthService>());
        Assert.NotNull(scoped.GetRequiredService<IEmployeeService>());
        Assert.NotNull(scoped.GetRequiredService<IDepartmentService>());
        Assert.NotNull(scoped.GetRequiredService<IVacationRequestService>());
        Assert.NotNull(scoped.GetRequiredService<ITripService>());
        Assert.NotNull(scoped.GetRequiredService<IEmployeeSessionValidator>());
        Assert.NotNull(scoped.GetRequiredService<IDepartmentRepository>());
        Assert.NotNull(scoped.GetRequiredService<IEmployeeRepository>());
        Assert.NotNull(scoped.GetRequiredService<IVacationRequestRepository>());
        Assert.NotNull(scoped.GetRequiredService<ITripRepository>());
        Assert.NotNull(scoped.GetRequiredService<IIdentityUserLookup>());
        Assert.NotNull(scoped.GetRequiredService<IUnitOfWork>());
        Assert.NotNull(scoped.GetRequiredService<TimeProvider>());
        Assert.NotNull(scoped.GetRequiredService<WorkingDayCalendar>());
    }

    [Fact]
    public void AddInfrastructure_RequiresDefaultConnection()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddInfrastructure(configuration));

        Assert.Equal("Connection string 'DefaultConnection' not found.", exception.Message);
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=HR.Tests;Trusted_Connection=True;"
            })
            .Build();
    }
}
