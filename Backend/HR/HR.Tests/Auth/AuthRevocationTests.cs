using System.Security.Claims;
using HR.Application.Auth;
using HR.Infrastructure.Auth;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Auth;

public class AuthRevocationTests
{
    [Fact]
    public async Task ValidateCredentialsAsync_DeniesLoginForTerminatedEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync(
            "EMP-901",
            "terminated901@example.com",
            environment.DefaultDepartment!.Id,
            status: HR.Domain.Enums.EmployeeStatus.Terminated,
            terminatedAt: DateTimeOffset.UtcNow);
        var service = environment.GetRequiredService<IAuthService>();

        var result = await service.ValidateCredentialsAsync(employee.Email!, "ValidPass1!", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_DeniesLoginForSoftDeletedEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync(
            "EMP-902",
            "deleted902@example.com",
            environment.DefaultDepartment!.Id,
            isDeleted: true,
            status: HR.Domain.Enums.EmployeeStatus.Terminated,
            terminatedAt: DateTimeOffset.UtcNow);
        var service = environment.GetRequiredService<IAuthService>();

        var result = await service.ValidateCredentialsAsync(employee.Email!, "ValidPass1!", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task EmployeeSessionValidator_RejectsTerminatedAndDeletedSessions()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var active = await environment.AddEmployeeAsync("EMP-903", "active903@example.com", environment.DefaultDepartment!.Id);
        var terminated = await environment.AddEmployeeAsync(
            "EMP-904",
            "terminated904@example.com",
            environment.DefaultDepartment!.Id,
            status: HR.Domain.Enums.EmployeeStatus.Terminated,
            terminatedAt: DateTimeOffset.UtcNow);
        var deleted = await environment.AddEmployeeAsync(
            "EMP-905",
            "deleted905@example.com",
            environment.DefaultDepartment!.Id,
            isDeleted: true,
            status: HR.Domain.Enums.EmployeeStatus.Terminated,
            terminatedAt: DateTimeOffset.UtcNow);
        var validator = environment.GetRequiredService<IEmployeeSessionValidator>();

        var activeResult = await validator.IsValidAsync(CreatePrincipal(active.Id), CancellationToken.None);
        var terminatedResult = await validator.IsValidAsync(CreatePrincipal(terminated.Id), CancellationToken.None);
        var deletedResult = await validator.IsValidAsync(CreatePrincipal(deleted.Id), CancellationToken.None);

        Assert.True(activeResult);
        Assert.False(terminatedResult);
        Assert.False(deletedResult);
    }

    private static ClaimsPrincipal CreatePrincipal(Guid employeeId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("employee_id", employeeId.ToString())
        ], "test"));
    }
}
