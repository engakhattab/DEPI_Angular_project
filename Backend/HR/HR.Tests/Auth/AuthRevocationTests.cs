using System.Security.Claims;
using HR.Application.Auth;
using HR.Domain.Enums;
using HR.Infrastructure.Auth;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Auth;

public class AuthRevocationTests
{
    [Fact]
    public async Task ValidateCredentialsAsync_DeniesLoginForSoftDeletedEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("AUTH-DEL-001", "auth-deleted@example.com", environment.DefaultDepartment!.Id, isDeleted: true);
        var service = environment.GetRequiredService<IAuthService>();

        var result = await service.ValidateCredentialsAsync(employee.Email!, "ValidPass1!", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_DeniesLoginForTerminatedEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("AUTH-TER-001", "auth-terminated@example.com", environment.DefaultDepartment!.Id, status: EmployeeStatus.Terminated);
        var service = environment.GetRequiredService<IAuthService>();

        var result = await service.ValidateCredentialsAsync(employee.Email!, "ValidPass1!", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task EmployeeSessionValidator_RejectsDeletedAndTerminatedSessions()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var active = await environment.AddEmployeeAsync("AUTH-ACT-001", "auth-active@example.com", environment.DefaultDepartment!.Id);
        var deleted = await environment.AddEmployeeAsync("AUTH-DEL-002", "auth-deleted-2@example.com", environment.DefaultDepartment.Id, isDeleted: true);
        var terminated = await environment.AddEmployeeAsync("AUTH-TER-002", "auth-terminated-2@example.com", environment.DefaultDepartment.Id, status: EmployeeStatus.Terminated);
        var suspended = await environment.AddEmployeeAsync("AUTH-SUS-001", "auth-suspended@example.com", environment.DefaultDepartment.Id, status: EmployeeStatus.Suspended);
        var validator = environment.GetRequiredService<IEmployeeSessionValidator>();

        Assert.True(await validator.IsValidAsync(CreatePrincipal(active.Id), CancellationToken.None));
        Assert.False(await validator.IsValidAsync(CreatePrincipal(deleted.Id), CancellationToken.None));
        Assert.False(await validator.IsValidAsync(CreatePrincipal(terminated.Id), CancellationToken.None));
        Assert.True(await validator.IsValidAsync(CreatePrincipal(suspended.Id), CancellationToken.None));
    }

    [Fact]
    public async Task EmployeeSessionValidator_RejectsMissingEmployeeClaim()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var validator = environment.GetRequiredService<IEmployeeSessionValidator>();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test"));

        Assert.False(await validator.IsValidAsync(principal, CancellationToken.None));
    }

    private static ClaimsPrincipal CreatePrincipal(Guid employeeId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("employee_id", employeeId.ToString())
        ], "test"));
    }
}
