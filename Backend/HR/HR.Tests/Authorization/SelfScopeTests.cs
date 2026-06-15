using HR.Application.Authorization;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Authorization;

public class SelfScopeTests
{
    [Fact]
    public async Task IsSelf_ReturnsTrue_ForSameEmployeeId()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("SELF-001", "self001@example.com", environment.DefaultDepartment!.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(access.IsSelf(employee.Id, employee.Id));
    }

    [Fact]
    public async Task IsSelf_ReturnsFalse_ForDifferentEmployeeIds()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("SELF-002", "self002@example.com", environment.DefaultDepartment!.Id);
        var other = await environment.AddEmployeeAsync("SELF-003", "self003@example.com", environment.DefaultDepartment!.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(access.IsSelf(employee.Id, other.Id));
    }

    [Fact]
    public async Task CanAccessEmployeeAsync_AllowsSelf_ForEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("SELF-004", "self004@example.com", environment.DefaultDepartment!.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.CanAccessEmployeeAsync(employee.Id, employee.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CanAccessEmployeeAsync_DeniesSelf_ForMissingRequester()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employeeId = Guid.NewGuid();
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.CanAccessEmployeeAsync(employeeId, employeeId, CancellationToken.None));
    }

    [Fact]
    public async Task CanAccessEmployeeAsync_DeniesSelf_ForSoftDeletedRequester()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("SELF-DEL-001", "self-deleted@example.com", environment.DefaultDepartment!.Id, isDeleted: true);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.CanAccessEmployeeAsync(employee.Id, employee.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CanAccessEmployeeAsync_DeniesSelf_ForTerminatedRequester()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("SELF-TER-001", "self-terminated@example.com", environment.DefaultDepartment!.Id, status: EmployeeStatus.Terminated);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.CanAccessEmployeeAsync(employee.Id, employee.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CanAccessEmployeeAsync_AllowsSelf_ForSuspendedRequester()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("SELF-SUS-001", "self-suspended@example.com", environment.DefaultDepartment!.Id, status: EmployeeStatus.Suspended);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.CanAccessEmployeeAsync(employee.Id, employee.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CanAccessEmployeeAsync_AllowsSelf_ForManager()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var manager = await environment.AddEmployeeAsync("SELF-005", "self005@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.CanAccessEmployeeAsync(manager.Id, manager.Id, CancellationToken.None));
    }
}
