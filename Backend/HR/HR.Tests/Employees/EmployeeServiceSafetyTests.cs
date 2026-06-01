using HR.Application.DTOs.Employees;
using HR.Domain.Entities;
using HR.Infrastructure.Data;
using HR.Infrastructure.Employees;
using HR.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HR.Tests.Employees;

public class EmployeeServiceSafetyTests
{
    [Fact]
    public async Task DeleteEmployeeAsync_RemovesRelatedDataAndClearsDirectReports()
    {
        await using var fixture = await EmployeeServiceFixture.CreateAsync();
        var manager = await fixture.AddEmployeeAsync("EMP-001", "manager@example.com");
        var report = await fixture.AddEmployeeAsync("EMP-002", "report@example.com", manager.Id);
        var vacationRequest = new VacationRequest
        {
            EmployeeId = manager.Id,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 2),
            Reason = "Regression test"
        };
        fixture.Context.VacationRequests.Add(vacationRequest);
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.DeleteEmployeeAsync(manager.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        fixture.Context.ChangeTracker.Clear();
        Assert.False(await fixture.Context.Employees.AnyAsync(e => e.Id == manager.Id));
        Assert.False(await fixture.Context.Users.AnyAsync(u => u.Id == manager.ApplicationUserId));
        Assert.False(await fixture.Context.VacationRequests.AnyAsync(v => v.Id == vacationRequest.Id));
        Assert.Null((await fixture.Context.Employees.SingleAsync(e => e.Id == report.Id)).ManagerId);
    }

    [Fact]
    public async Task DeleteEmployeeAsync_WhenIdentityDeleteFails_RollsBackAllCleanup()
    {
        await using var fixture = await EmployeeServiceFixture.CreateAsync();
        var manager = await fixture.AddEmployeeAsync("EMP-003", "rollback-manager@example.com");
        var report = await fixture.AddEmployeeAsync("EMP-004", "rollback-report@example.com", manager.Id);
        var vacationRequest = new VacationRequest
        {
            EmployeeId = manager.Id,
            StartDate = new DateOnly(2026, 6, 3),
            EndDate = new DateOnly(2026, 6, 4),
            Reason = "Rollback regression test"
        };
        fixture.Context.VacationRequests.Add(vacationRequest);
        await fixture.Context.SaveChangesAsync();
        await fixture.Context.Database.ExecuteSqlRawAsync(
            """
            CREATE TRIGGER RejectIdentityUserDelete
            BEFORE DELETE ON AspNetUsers
            BEGIN
                SELECT RAISE(ABORT, 'Identity user deletion rejected.');
            END;
            """);

        await Assert.ThrowsAnyAsync<Exception>(
            () => fixture.Service.DeleteEmployeeAsync(manager.Id, CancellationToken.None));

        fixture.Context.ChangeTracker.Clear();
        Assert.True(await fixture.Context.Employees.AnyAsync(e => e.Id == manager.Id));
        Assert.True(await fixture.Context.Users.AnyAsync(u => u.Id == manager.ApplicationUserId));
        Assert.True(await fixture.Context.VacationRequests.AnyAsync(v => v.Id == vacationRequest.Id));
        Assert.Equal(manager.Id, (await fixture.Context.Employees.SingleAsync(e => e.Id == report.Id)).ManagerId);
    }

    [Fact]
    public async Task CreateEmployeeAsync_WhenEmployeeInsertFails_RollsBackIdentityUser()
    {
        await using var fixture = await EmployeeServiceFixture.CreateAsync();
        await fixture.Context.Database.ExecuteSqlRawAsync(
            """
            CREATE TRIGGER RejectEmployeeInsert
            BEFORE INSERT ON Employees
            BEGIN
                SELECT RAISE(ABORT, 'Employee insertion rejected.');
            END;
            """);

        var request = new EmployeeCreateRequest
        {
            EmployeeNumber = "EMP-005",
            FullName = "Rollback Employee",
            Email = "rollback-create@example.com",
            DepartmentId = fixture.Department.Id,
            InitialPassword = "ValidPass1!"
        };

        await Assert.ThrowsAnyAsync<Exception>(
            () => fixture.Service.CreateEmployeeAsync(request, CancellationToken.None));

        fixture.Context.ChangeTracker.Clear();
        Assert.False(await fixture.Context.Employees.AnyAsync(e => e.EmployeeNumber == request.EmployeeNumber));
        Assert.False(await fixture.Context.Users.AnyAsync(u => u.Email == request.Email));
    }

    private sealed class EmployeeServiceFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;
        private readonly UserManager<ApplicationUser> _userManager;

        private EmployeeServiceFixture(
            SqliteConnection connection,
            ServiceProvider provider,
            IServiceScope scope,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            Department department)
        {
            _connection = connection;
            _provider = provider;
            _scope = scope;
            Context = context;
            _userManager = userManager;
            Department = department;
            Service = new EmployeeService(context, userManager);
        }

        public ApplicationDbContext Context { get; }

        public Department Department { get; }

        public EmployeeService Service { get; }

        public static async Task<EmployeeServiceFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
            services
                .AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            var provider = services.BuildServiceProvider();
            var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var department = new Department { Name = "Engineering" };
            context.Departments.Add(department);
            await context.SaveChangesAsync();

            return new EmployeeServiceFixture(connection, provider, scope, context, userManager, department);
        }

        public async Task<Employee> AddEmployeeAsync(string employeeNumber, string email, Guid? managerId = null)
        {
            var user = new ApplicationUser { UserName = email, Email = email };
            var identityResult = await _userManager.CreateAsync(user, "ValidPass1!");
            Assert.True(identityResult.Succeeded, string.Join(" ", identityResult.Errors.Select(e => e.Description)));

            var employee = new Employee
            {
                EmployeeNumber = employeeNumber,
                FullName = employeeNumber,
                Email = email,
                DepartmentId = Department.Id,
                ManagerId = managerId,
                ApplicationUserId = user.Id
            };
            Context.Employees.Add(employee);
            await Context.SaveChangesAsync();
            return employee;
        }

        public async ValueTask DisposeAsync()
        {
            _scope.Dispose();
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
