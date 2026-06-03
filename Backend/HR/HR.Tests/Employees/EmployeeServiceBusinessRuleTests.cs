using HR.Application.DTOs.Employees;
using HR.Application.Employees;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Employees;
using HR.Infrastructure.Identity;
using HR.Infrastructure.Repositories;
using HR.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace HR.Tests.Employees;

public class EmployeeServiceBusinessRuleTests
{
    [Fact]
    public async Task UpdateEmployeeAsync_RejectsDuplicateActiveEmail()
    {
        await using var fixture = await EmployeeFixture.CreateAsync();
        await fixture.AddEmployeeAsync("EMP-801", "first@example.com");
        var employee = await fixture.AddEmployeeAsync("EMP-802", "second@example.com");

        var result = await fixture.Service.UpdateEmployeeAsync(
            employee.Id,
            fixture.BuildUpdateRequest(employee, email: "first@example.com"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_RejectsCircularManagerChain()
    {
        await using var fixture = await EmployeeFixture.CreateAsync();
        var manager = await fixture.AddEmployeeAsync("EMP-803", "manager803@example.com");
        var report = await fixture.AddEmployeeAsync("EMP-804", "report804@example.com", managerId: manager.Id);

        var result = await fixture.Service.UpdateEmployeeAsync(
            manager.Id,
            fixture.BuildUpdateRequest(manager, managerId: report.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_AllowsCrossDepartmentManagerAndSameStatusNoOpWithoutTerminationSideEffects()
    {
        await using var fixture = await EmployeeFixture.CreateAsync();
        var otherDepartment = await fixture.Environment.AddDepartmentAsync("Support");
        var manager = await fixture.Environment.AddEmployeeAsync("EMP-805", "manager805@example.com", otherDepartment.Id);
        var employee = await fixture.AddEmployeeAsync("EMP-806", "employee806@example.com");

        var result = await fixture.Service.UpdateEmployeeAsync(
            employee.Id,
            fixture.BuildUpdateRequest(employee, managerId: manager.Id, status: EmployeeStatus.Active),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await fixture.ReloadEmployeeAsync(employee.Id);
        Assert.Equal(manager.Id, reloaded.ManagerId);
        Assert.Null(reloaded.TerminatedAt);
    }

    [Fact]
    public async Task CreateEmployeeAsync_LogsWarningForCrossDepartmentManagerAssignmentAndAppliesDefaultVacationBalance()
    {
        await using var fixture = await EmployeeFixture.CreateAsync();
        var logger = new CapturingLogger<EmployeeService>();
        var otherDepartment = await fixture.Environment.AddDepartmentAsync("Support");
        var manager = await fixture.Environment.AddEmployeeAsync("EMP-814", "manager814@example.com", otherDepartment.Id);
        var service = fixture.CreateService(logger);

        var result = await service.CreateEmployeeAsync(
            new EmployeeCreateRequest
            {
                EmployeeNumber = "EMP-815",
                FullName = "Employee 815",
                Email = "employee815@example.com",
                DepartmentId = fixture.Environment.DefaultDepartment!.Id,
                ManagerId = manager.Id,
                Status = EmployeeStatus.Active,
                InitialPassword = "ValidPass1!"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(21, result.Value!.Employee.VacationBalanceDays);
        Assert.Contains(logger.Entries, entry => entry.LogLevel == LogLevel.Warning
            && entry.Message.Contains("different department", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateEmployeeAsync_TerminationRejectsPendingVacationAndPreservesEmployeeNumber()
    {
        await using var fixture = await EmployeeFixture.CreateAsync();
        var employee = await fixture.AddEmployeeAsync("EMP-807", "employee807@example.com");
        await fixture.Environment.AddVacationRequestAsync(employee.Id, VacationRequestStatus.Pending, fixture.UtcNow);
        var originalNumber = employee.EmployeeNumber;

        var result = await fixture.Service.UpdateEmployeeAsync(
            employee.Id,
            fixture.BuildUpdateRequest(employee, status: EmployeeStatus.Terminated),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await fixture.ReloadEmployeeAsync(employee.Id);
        var request = await fixture.Environment.Context.VacationRequests.SingleAsync(v => v.EmployeeId == employee.Id);
        Assert.Equal(originalNumber, reloaded.EmployeeNumber);
        Assert.NotNull(reloaded.TerminatedAt);
        Assert.Equal(EmployeeStatus.Terminated, reloaded.Status);
        Assert.Equal(VacationRequestStatus.Rejected, request.Status);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_RejectsFurtherStatusTransitionFromTerminated()
    {
        await using var fixture = await EmployeeFixture.CreateAsync();
        var employee = await fixture.AddEmployeeAsync(
            "EMP-816",
            "employee816@example.com",
            status: EmployeeStatus.Terminated,
            terminatedAt: fixture.UtcNow.AddDays(-1));

        var result = await fixture.Service.UpdateEmployeeAsync(
            employee.Id,
            fixture.BuildUpdateRequest(employee, status: EmployeeStatus.Active),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_SameTerminatedStatusDoesNotRewriteTerminationTimestamp()
    {
        await using var fixture = await EmployeeFixture.CreateAsync();
        var employee = await fixture.AddEmployeeAsync(
            "EMP-808",
            "employee808@example.com",
            status: EmployeeStatus.Terminated,
            terminatedAt: fixture.UtcNow.AddDays(-1));

        var result = await fixture.Service.UpdateEmployeeAsync(
            employee.Id,
            fixture.BuildUpdateRequest(employee, status: EmployeeStatus.Terminated),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.UtcNow.AddDays(-1), (await fixture.ReloadEmployeeAsync(employee.Id)).TerminatedAt);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_KeepsEmployeeNumberImmutableAcrossOtherFieldChanges()
    {
        await using var fixture = await EmployeeFixture.CreateAsync();
        var employee = await fixture.AddEmployeeAsync("EMP-817", "employee817@example.com");

        var result = await fixture.Service.UpdateEmployeeAsync(
            employee.Id,
            fixture.BuildUpdateRequest(employee, email: "employee817-updated@example.com"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("EMP-817", result.Value!.EmployeeNumber);
        Assert.Equal("EMP-817", (await fixture.ReloadEmployeeAsync(employee.Id)).EmployeeNumber);
    }

    [Fact]
    public async Task DeleteEmployeeAsync_SoftDeletesRetainsIdentityAndClearsDirectReports()
    {
        await using var fixture = await EmployeeFixture.CreateAsync();
        var manager = await fixture.AddEmployeeAsync("EMP-809", "manager809@example.com");
        var report = await fixture.AddEmployeeAsync("EMP-810", "report810@example.com", managerId: manager.Id);
        await fixture.Environment.AddVacationRequestAsync(manager.Id, VacationRequestStatus.Pending, fixture.UtcNow);

        var result = await fixture.Service.DeleteEmployeeAsync(manager.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        fixture.Environment.Context.ChangeTracker.Clear();
        var deletedEmployee = await fixture.Environment.Context.Employees.SingleAsync(e => e.Id == manager.Id);
        var directReport = await fixture.Environment.Context.Employees.SingleAsync(e => e.Id == report.Id);
        var userExists = await fixture.Environment.Context.Users.AnyAsync(u => u.Id == manager.ApplicationUserId);
        var request = await fixture.Environment.Context.VacationRequests.SingleAsync(v => v.EmployeeId == manager.Id);

        Assert.True(deletedEmployee.IsDeleted);
        Assert.Equal(EmployeeStatus.Terminated, deletedEmployee.Status);
        Assert.NotNull(deletedEmployee.TerminatedAt);
        Assert.True(userExists);
        Assert.Null(directReport.ManagerId);
        Assert.Equal(VacationRequestStatus.Rejected, request.Status);
    }

    [Fact]
    public async Task GetEmployeesAsync_ExcludesSoftDeletedButRetainsTerminatedVisibleProfiles()
    {
        await using var fixture = await EmployeeFixture.CreateAsync();
        await fixture.AddEmployeeAsync("EMP-811", "active811@example.com");
        await fixture.AddEmployeeAsync("EMP-812", "terminated812@example.com", status: EmployeeStatus.Terminated, terminatedAt: fixture.UtcNow);
        await fixture.AddEmployeeAsync("EMP-813", "deleted813@example.com", isDeleted: true, status: EmployeeStatus.Terminated, terminatedAt: fixture.UtcNow);

        var result = await fixture.Service.GetEmployeesAsync(null, 1, 25, CancellationToken.None);

        Assert.Contains(result.Items, e => e.EmployeeNumber == "EMP-811");
        Assert.Contains(result.Items, e => e.EmployeeNumber == "EMP-812");
        Assert.DoesNotContain(result.Items, e => e.EmployeeNumber == "EMP-813");
    }

    private sealed class EmployeeFixture : IAsyncDisposable
    {
        private readonly SqliteTestEnvironment _environment;

        private EmployeeFixture(SqliteTestEnvironment environment, DateTimeOffset utcNow)
        {
            _environment = environment;
            UtcNow = utcNow;
            Service = environment.GetRequiredService<IEmployeeService>();
        }

        public SqliteTestEnvironment Environment => _environment;

        public IEmployeeService Service { get; }

        public DateTimeOffset UtcNow { get; }

        public EmployeeService CreateService(ILogger<EmployeeService> logger)
        {
            return new EmployeeService(
                _environment.GetRequiredService<IEmployeeRepository>(),
                _environment.GetRequiredService<IDepartmentRepository>(),
                _environment.GetRequiredService<IVacationRequestRepository>(),
                _environment.GetRequiredService<IIdentityUserLookup>(),
                _environment.GetRequiredService<IUnitOfWork>(),
                _environment.GetRequiredService<UserManager<ApplicationUser>>(),
                logger,
                _environment.GetRequiredService<TimeProvider>());
        }

        public static async Task<EmployeeFixture> CreateAsync()
        {
            var utcNow = new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);
            var environment = await SqliteTestEnvironment.CreateAsync(
                seedDefaultDepartment: true,
                timeProvider: new TestTimeProvider(utcNow));
            return new EmployeeFixture(environment, utcNow);
        }

        public Task<Employee> AddEmployeeAsync(
            string employeeNumber,
            string email,
            Guid? managerId = null,
            EmployeeStatus status = EmployeeStatus.Active,
            int vacationBalanceDays = 21,
            bool isDeleted = false,
            DateTimeOffset? terminatedAt = null)
        {
            return _environment.AddEmployeeAsync(
                employeeNumber,
                email,
                _environment.DefaultDepartment!.Id,
                managerId,
                status,
                vacationBalanceDays,
                isDeleted,
                terminatedAt);
        }

        public EmployeeUpdateRequest BuildUpdateRequest(
            Employee employee,
            string? email = null,
            Guid? departmentId = null,
            Guid? managerId = null,
            EmployeeStatus? status = null)
        {
            return new EmployeeUpdateRequest
            {
                FullName = employee.FullName,
                Email = email ?? employee.Email ?? $"{employee.EmployeeNumber}@example.com",
                DepartmentId = departmentId ?? employee.DepartmentId,
                ManagerId = managerId,
                BirthDate = employee.BirthDate,
                JoinDate = employee.JoinDate,
                JobTitle = employee.JobTitle,
                PhoneNumber = employee.PhoneNumber,
                Notes = employee.Notes,
                Status = status ?? employee.Status
            };
        }

        public Task<Employee> ReloadEmployeeAsync(Guid employeeId)
        {
            return _environment.Context.Employees.SingleAsync(e => e.Id == employeeId);
        }

        public ValueTask DisposeAsync()
        {
            return _environment.DisposeAsync();
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(LogLevel LogLevel, string Message);
}
