using HR.Application.DTOs.Employees;
using HR.Application.Employees;
using HR.Domain.Entities;
using HR.Infrastructure.Employees;
using HR.Infrastructure.Repositories;
using HR.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
        private readonly SqliteTestEnvironment _environment;

        private EmployeeServiceFixture(
            SqliteTestEnvironment environment,
            Department department)
        {
            _environment = environment;
            Context = environment.Context;
            Department = department;
            Service = environment.GetRequiredService<IEmployeeService>();
        }

        public HR.Infrastructure.Data.ApplicationDbContext Context { get; }

        public Department Department { get; }

        public IEmployeeService Service { get; }

        public static async Task<EmployeeServiceFixture> CreateAsync()
        {
            var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
            return new EmployeeServiceFixture(environment, environment.DefaultDepartment!);
        }

        public async Task<Employee> AddEmployeeAsync(string employeeNumber, string email, Guid? managerId = null)
        {
            return await _environment.AddEmployeeAsync(employeeNumber, email, Department.Id, managerId);
        }

        public async ValueTask DisposeAsync()
        {
            await _environment.DisposeAsync();
        }
    }
}
