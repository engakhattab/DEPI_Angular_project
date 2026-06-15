using HR.Application.Auth;
using HR.Application.DTOs.Employees;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Auth;
using HR.Infrastructure.Repositories;
using HR.Shared.Pagination;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Auth;

public class AuthServiceTests
{
    [Fact]
    public async Task ValidateCredentialsAsync_WithEmailLookup_ReturnsMappedEmployeeResponse()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync();
        var user = await environment.AddUserAsync("auth-email@example.com");
        var repository = new FakeEmployeeRepository
        {
            ApplicationUserEmployee = CreateEmployee(user.Id, "EMP-401", "Auth Email")
        };
        var service = new AuthService(repository, environment.UserManager);

        var result = await service.ValidateCredentialsAsync(user.Email!, "ValidPass1!", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("EMP-401", result.Value!.Employee.EmployeeNumber);
        Assert.Equal(user.Id, result.Value.UserId);
        Assert.Equal("Engineering", result.Value.Employee.DepartmentName);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithEmployeeNumberLookup_ReturnsMappedEmployeeResponse()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync();
        var user = await environment.AddUserAsync("auth-number@example.com");
        var repository = new FakeEmployeeRepository
        {
            EmployeeNumberEmployee = CreateEmployee(user.Id, "EMP-402", "Auth Number")
        };
        var service = new AuthService(repository, environment.UserManager);

        var result = await service.ValidateCredentialsAsync("EMP-402", "ValidPass1!", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Auth Number", result.Value!.Employee.FullName);
        Assert.Equal(user.Email, result.Value.UserEmail);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WhenMatchingIdentityIsMissing_ReturnsValidationFailure()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync();
        var repository = new FakeEmployeeRepository
        {
            EmployeeNumberEmployee = CreateEmployee("missing-user", "EMP-403", "Missing User")
        };
        var service = new AuthService(repository, environment.UserManager);

        var result = await service.ValidateCredentialsAsync("EMP-403", "ValidPass1!", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION", result.Error!.Code);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WhenPasswordIsInvalid_ReturnsValidationFailure()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync();
        var user = await environment.AddUserAsync("wrong-pass@example.com");
        var repository = new FakeEmployeeRepository
        {
            EmployeeNumberEmployee = CreateEmployee(user.Id, "EMP-404", "Wrong Password")
        };
        var service = new AuthService(repository, environment.UserManager);

        var result = await service.ValidateCredentialsAsync("EMP-404", "WrongPass1!", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION", result.Error!.Code);
    }

    private static Employee CreateEmployee(string applicationUserId, string employeeNumber, string fullName)
    {
        return new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = employeeNumber,
            FullName = fullName,
            Email = $"{employeeNumber}@example.com",
            DepartmentId = Guid.NewGuid(),
            Department = new Department { Id = Guid.NewGuid(), Name = "Engineering" },
            ApplicationUserId = applicationUserId
        };
    }

    private sealed class FakeEmployeeRepository : IEmployeeRepository
    {
        public Employee? ApplicationUserEmployee { get; init; }
        public Employee? EmployeeNumberEmployee { get; init; }

        public Task<PagedList<Employee>> GetPageWithDetailsAsync(HR.Domain.Enums.EmployeeStatus? status, int page, int pageSize, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<PagedList<Employee>> GetScopedPageAsync(IReadOnlySet<Guid> allowedIds, EmployeeStatus? status, int page, int pageSize, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<PagedList<Employee>> GetOrganizationWidePageAsync(EmployeeStatus? status, int page, int pageSize, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Employee?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Employee?> GetByIdWithDetailsIncludingSoftDeletedAsync(Guid id, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Employee?> GetByApplicationUserIdWithDetailsAsync(string applicationUserId, CancellationToken ct)
            => Task.FromResult(ApplicationUserEmployee);

        public Task<Employee?> GetByEmployeeNumberWithDetailsAsync(string employeeNumber, CancellationToken ct)
            => Task.FromResult(EmployeeNumberEmployee);

        public Task<IReadOnlyList<Employee>> FindByEmailOrEmployeeNumberAsync(string identifier, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<Employee>> GetDirectReportsAsync(Guid managerId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<Employee>> GetAllActiveAsync(CancellationToken ct)
            => throw new NotSupportedException();

        public Task<IReadOnlySet<Guid>> GetDirectAndIndirectReportIdsAsync(Guid managerId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<bool> AnyActiveSystemAdministratorAsync(CancellationToken ct)
            => throw new NotSupportedException();

        public Task<int> GetActiveSystemAdministratorCountAsync(CancellationToken ct)
            => throw new NotSupportedException();

        public Task<bool> ExistsWithEmailAsync(string email, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<bool> ExistsActiveWithEmailAsync(string email, Guid? excludingEmployeeId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Guid?> GetManagerIdAsync(Guid employeeId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<bool> IsAuthenticationEligibleAsync(Guid employeeId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<bool> ExistsIncludingSoftDeletedAsync(Guid id, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<bool> ExistsByNumberAsync(string employeeNumber, CancellationToken ct)
            => throw new NotSupportedException();

        public Task AddAsync(Employee employee, CancellationToken ct)
            => throw new NotSupportedException();

        public void Remove(Employee employee)
            => throw new NotSupportedException();
    }
}
