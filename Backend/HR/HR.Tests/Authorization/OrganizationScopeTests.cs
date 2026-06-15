using HR.Application.Authorization;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Authorization;

public class OrganizationScopeTests
{
    [Fact]
    public async Task HasOrganizationScopeAsync_ReturnsFalse_ForEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("OSC-EMP-001", "orgscope-emp@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Employee);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.HasOrganizationScopeAsync(employee.Id, CancellationToken.None));
    }

    [Fact]
    public async Task HasOrganizationScopeAsync_ReturnsFalse_ForManager()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var manager = await environment.AddEmployeeAsync("OSC-MGR-001", "orgscope-mgr@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.HasOrganizationScopeAsync(manager.Id, CancellationToken.None));
    }

    [Fact]
    public async Task HasOrganizationScopeAsync_ReturnsTrue_ForHRAdministrator()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var hrAdmin = await environment.AddEmployeeAsync("OSC-HR-001", "orgscope-hr@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.HasOrganizationScopeAsync(hrAdmin.Id, CancellationToken.None));
    }

    [Fact]
    public async Task HasOrganizationScopeAsync_ReturnsTrue_ForSystemAdministrator()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var sysAdmin = await environment.AddEmployeeAsync("OSC-SYS-001", "orgscope-sys@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.SystemAdministrator);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.HasOrganizationScopeAsync(sysAdmin.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IsHRAdministratorAsync_ReturnsTrue_ForHRAdministrator()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var hrAdmin = await environment.AddEmployeeAsync("OSC-HR-002", "orgscope-hr2@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.IsHRAdministratorAsync(hrAdmin.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IsHRAdministratorAsync_ReturnsFalse_ForNonHRRole()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("OSC-NHR-001", "orgscope-nhr@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Employee);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.IsHRAdministratorAsync(employee.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IsSystemAdministratorAsync_ReturnsTrue_ForSystemAdministrator()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var sysAdmin = await environment.AddEmployeeAsync("OSC-SYS-002", "orgscope-sys2@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.SystemAdministrator);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.IsSystemAdministratorAsync(sysAdmin.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IsSystemAdministratorAsync_ReturnsFalse_ForNonSystemRole()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("OSC-NSYS-001", "orgscope-nsys@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Employee);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.IsSystemAdministratorAsync(employee.Id, CancellationToken.None));
    }
}
