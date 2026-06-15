using HR.Application.Authorization;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Authorization;

public class EmployeeAccessContractTests
{
    [Fact]
    public async Task IsSelf_ReturnsTrue_ForSameEmployeeId()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("CTRCT-001", "contract001@example.com", environment.DefaultDepartment!.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(access.IsSelf(employee.Id, employee.Id));
    }

    [Fact]
    public async Task IsSelf_ReturnsFalse_ForDifferentEmployeeIds()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("CTRCT-002", "contract002@example.com", environment.DefaultDepartment!.Id);
        var other = await environment.AddEmployeeAsync("CTRCT-003", "contract003@example.com", environment.DefaultDepartment!.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(access.IsSelf(employee.Id, other.Id));
    }

    [Fact]
    public async Task IsManagerOfAsync_ReturnsTrue_ForDirectReport()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var manager = await environment.AddEmployeeAsync("CTRCT-MGR-001", "contract-mgr@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var report = await environment.AddEmployeeAsync("CTRCT-REP-001", "contract-rep@example.com", environment.DefaultDepartment.Id, managerId: manager.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.IsManagerOfAsync(manager.Id, report.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IsManagerOfAsync_ReturnsFalse_ForNonManager()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("CTRCT-EMP-001", "contract-emp@example.com", environment.DefaultDepartment!.Id);
        var other = await environment.AddEmployeeAsync("CTRCT-EMP-002", "contract-emp2@example.com", environment.DefaultDepartment.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.IsManagerOfAsync(employee.Id, other.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CanAccessTeamDataAsync_ReturnsTrue_ForOrganizationScope()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var admin = await environment.AddEmployeeAsync("CTRCT-HR-001", "contract-hr@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var target = await environment.AddEmployeeAsync("CTRCT-TGT-001", "contract-tgt@example.com", environment.DefaultDepartment.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.CanAccessTeamDataAsync(admin.Id, target.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IsHRAdministratorAsync_ReturnsTrue_ForHRAdminRole()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var hrAdmin = await environment.AddEmployeeAsync("CTRCT-HR-002", "contract-hr2@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.IsHRAdministratorAsync(hrAdmin.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IsSystemAdministratorAsync_ReturnsTrue_ForSystemAdminRole()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var sysAdmin = await environment.AddEmployeeAsync("CTRCT-SYS-001", "contract-sys@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.SystemAdministrator);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.IsSystemAdministratorAsync(sysAdmin.Id, CancellationToken.None));
    }

    [Fact]
    public async Task HasOrganizationScopeAsync_ReturnsTrue_ForHRAdministrator()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var hrAdmin = await environment.AddEmployeeAsync("CTRCT-HR-003", "contract-hr3@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.HasOrganizationScopeAsync(hrAdmin.Id, CancellationToken.None));
    }

    [Fact]
    public async Task HasOrganizationScopeAsync_ReturnsFalse_ForEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("CTRCT-EMP-003", "contract-emp3@example.com", environment.DefaultDepartment!.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.HasOrganizationScopeAsync(employee.Id, CancellationToken.None));
    }
}
