using HR.Application.Authorization;
using HR.Domain.Enums;
using HR.Shared.Results;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Authorization;

public class EmployeeAccessCurrentContextTests
{
    [Fact]
    public async Task GetCurrentAsync_ReturnsContext_ForActiveEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("CTX-ACT-001", "ctx-active@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Employee);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var result = await access.GetCurrentAsync(employee.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(employee.Id, result.Value.EmployeeId);
        Assert.Equal(EmployeeRole.Employee, result.Value.Role);
        Assert.True(result.Value.IsActive);
        Assert.False(result.Value.IsDeleted);
        Assert.False(result.Value.IsTerminated);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsIsActiveFalse_ForSuspendedEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("CTX-SUS-001", "ctx-suspended@example.com", environment.DefaultDepartment!.Id, status: EmployeeStatus.Suspended);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var result = await access.GetCurrentAsync(employee.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        Assert.False(result.Value.IsDeleted);
        Assert.False(result.Value.IsTerminated);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsIsTerminatedTrue_ForTerminatedEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("CTX-TER-001", "ctx-terminated@example.com", environment.DefaultDepartment!.Id, status: EmployeeStatus.Terminated);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var result = await access.GetCurrentAsync(employee.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        Assert.False(result.Value.IsDeleted);
        Assert.True(result.Value.IsTerminated);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsIsDeletedTrue_ForSoftDeletedEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("CTX-DEL-001", "ctx-deleted@example.com", environment.DefaultDepartment!.Id, isDeleted: true);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var result = await access.GetCurrentAsync(employee.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsDeleted);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsUnauthorizedFailure_ForMissingEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var result = await access.GetCurrentAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceError.Unauthorized("Invalid session.").Code, result.Error.Code);
    }
}
