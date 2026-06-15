using HR.Application.Authorization;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Authorization;

public class OrganizationScopeEligibilityTests
{
    [Fact]
    public async Task HasOrganizationScopeAsync_Denied_ForDeletedHRAdministrator()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var hrAdmin = await environment.AddEmployeeAsync("OSC-EL-DEL-001", "oselig-del@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator, isDeleted: true);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.HasOrganizationScopeAsync(hrAdmin.Id, CancellationToken.None));
    }

    [Fact]
    public async Task HasOrganizationScopeAsync_Denied_ForTerminatedHRAdministrator()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var hrAdmin = await environment.AddEmployeeAsync("OSC-EL-TER-001", "oselig-ter@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator, status: EmployeeStatus.Terminated);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.HasOrganizationScopeAsync(hrAdmin.Id, CancellationToken.None));
    }

    [Fact]
    public async Task HasOrganizationScopeAsync_Allowed_ForSuspendedAdministrator()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var hrAdmin = await environment.AddEmployeeAsync("OSC-EL-SUS-001", "oselig-sus@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator, status: EmployeeStatus.Suspended);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.HasOrganizationScopeAsync(hrAdmin.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IsHRAdministratorAsync_Denied_ForDeletedHRAdministrator()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var hrAdmin = await environment.AddEmployeeAsync("OSC-HR-DEL-001", "oshrel-del@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator, isDeleted: true);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.IsHRAdministratorAsync(hrAdmin.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IsHRAdministratorAsync_Denied_ForTerminatedHRAdministrator()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var hrAdmin = await environment.AddEmployeeAsync("OSC-HR-TER-001", "oshrel-ter@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator, status: EmployeeStatus.Terminated);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.IsHRAdministratorAsync(hrAdmin.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IsSystemAdministratorAsync_Denied_ForDeletedSystemAdministrator()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var sysAdmin = await environment.AddEmployeeAsync("OSC-SYS-DEL-001", "ossysel-del@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.SystemAdministrator, isDeleted: true);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.IsSystemAdministratorAsync(sysAdmin.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IsSystemAdministratorAsync_Denied_ForTerminatedSystemAdministrator()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var sysAdmin = await environment.AddEmployeeAsync("OSC-SYS-TER-001", "ossysel-ter@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.SystemAdministrator, status: EmployeeStatus.Terminated);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.IsSystemAdministratorAsync(sysAdmin.Id, CancellationToken.None));
    }

    [Fact]
    public async Task HasAnyRoleAsync_Denied_ForMissingEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.HasAnyRoleAsync(Guid.NewGuid(), CancellationToken.None, EmployeeRole.Employee));
    }

    [Fact]
    public async Task HasAnyRoleAsync_Denied_ForDeletedEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("OSC-HAR-DEL-001", "oshar-del@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager, isDeleted: true);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.HasAnyRoleAsync(employee.Id, CancellationToken.None, EmployeeRole.Manager));
    }

    [Fact]
    public async Task HasAnyRoleAsync_Denied_ForTerminatedEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("OSC-HAR-TER-001", "oshar-ter@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator, status: EmployeeStatus.Terminated);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.HasAnyRoleAsync(employee.Id, CancellationToken.None, EmployeeRole.HRAdministrator));
    }

    [Fact]
    public async Task HasAnyRoleAsync_Allowed_ForSuspendedEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("OSC-HAR-SUS-001", "oshar-sus@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager, status: EmployeeStatus.Suspended);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.HasAnyRoleAsync(employee.Id, CancellationToken.None, EmployeeRole.Manager));
    }
}
