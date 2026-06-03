using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure;
using HR.Infrastructure.Data;
using HR.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HR.Tests.TestInfrastructure;

public sealed class SqliteTestEnvironment : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    private SqliteTestEnvironment(
        SqliteConnection connection,
        ServiceProvider provider,
        IServiceScope scope,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        Department? defaultDepartment)
    {
        _connection = connection;
        _provider = provider;
        _scope = scope;
        Context = context;
        UserManager = userManager;
        DefaultDepartment = defaultDepartment;
    }

    public ApplicationDbContext Context { get; }

    public UserManager<ApplicationUser> UserManager { get; }

    public Department? DefaultDepartment { get; }

    public static async Task<SqliteTestEnvironment> CreateAsync(
        bool seedDefaultDepartment = false,
        TimeProvider? timeProvider = null)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(timeProvider ?? TimeProvider.System);
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddInfrastructure();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Department? department = null;
        if (seedDefaultDepartment)
        {
            department = new Department { Name = "Engineering" };
            context.Departments.Add(department);
            await context.SaveChangesAsync();
        }

        return new SqliteTestEnvironment(connection, provider, scope, context, userManager, department);
    }

    public T GetRequiredService<T>() where T : notnull
    {
        return _scope.ServiceProvider.GetRequiredService<T>();
    }

    public async Task<Department> AddDepartmentAsync(string name)
    {
        var department = new Department { Name = name };
        Context.Departments.Add(department);
        await Context.SaveChangesAsync();
        return department;
    }

    public async Task<ApplicationUser> AddUserAsync(string email, string password = "ValidPass1!")
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var result = await UserManager.CreateAsync(user, password);
        Assert.True(result.Succeeded, string.Join(" ", result.Errors.Select(e => e.Description)));
        return user;
    }

    public async Task<Employee> AddEmployeeAsync(
        string employeeNumber,
        string email,
        Guid departmentId,
        Guid? managerId = null,
        EmployeeStatus status = EmployeeStatus.Active,
        int vacationBalanceDays = 21,
        bool isDeleted = false,
        DateTimeOffset? terminatedAt = null)
    {
        var user = await AddUserAsync(email);
        var employee = new Employee
        {
            EmployeeNumber = employeeNumber,
            FullName = employeeNumber,
            Email = email,
            DepartmentId = departmentId,
            ManagerId = managerId,
            Status = status,
            VacationBalanceDays = vacationBalanceDays,
            IsDeleted = isDeleted,
            TerminatedAt = terminatedAt,
            ApplicationUserId = user.Id
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();
        return employee;
    }

    public async Task<VacationRequest> AddVacationRequestAsync(
        Guid employeeId,
        VacationRequestStatus status,
        DateTimeOffset createdAt,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string reason = "Test request",
        int workingDayCount = 2)
    {
        var request = new VacationRequest
        {
            EmployeeId = employeeId,
            StartDate = startDate ?? new DateOnly(2026, 6, 1),
            EndDate = endDate ?? new DateOnly(2026, 6, 2),
            Reason = reason,
            Status = status,
            WorkingDayCount = workingDayCount,
            CreatedAt = createdAt
        };

        Context.VacationRequests.Add(request);
        await Context.SaveChangesAsync();
        return request;
    }

    public async Task<Trip> AddTripAsync(
        string referenceName,
        DateTimeOffset createdAt,
        Guid? requestedByEmployeeId = null)
    {
        var trip = new Trip
        {
            ReferenceName = referenceName,
            Project = "Project",
            Route = "Route",
            TripType = "Bus",
            TripDate = new DateOnly(2026, 6, 10),
            TripCode = $"TRIP-{Guid.NewGuid():N}"[..11].ToUpperInvariant(),
            RequestCode = $"REQ-{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            CreatedAt = createdAt,
            RequestedByEmployeeId = requestedByEmployeeId
        };

        Context.Trips.Add(trip);
        await Context.SaveChangesAsync();
        return trip;
    }

    public async ValueTask DisposeAsync()
    {
        _scope.Dispose();
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
