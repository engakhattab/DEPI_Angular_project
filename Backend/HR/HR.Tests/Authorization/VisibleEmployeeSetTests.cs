using HR.Application.Authorization;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Authorization;

public class VisibleEmployeeSetTests
{
    [Fact]
    public async Task GetVisibleEmployeeIdsAsync_ReturnsSelfOnly_ForEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("VST-EMP-001", "vst-emp@example.com", environment.DefaultDepartment!.Id);
        var other = await environment.AddEmployeeAsync("VST-OTH-001", "vst-oth@example.com", environment.DefaultDepartment.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var visible = await access.GetVisibleEmployeeIdsAsync(employee.Id, CancellationToken.None);

        Assert.Contains(employee.Id, visible);
        Assert.DoesNotContain(other.Id, visible);
        Assert.Single(visible);
    }

    [Fact]
    public async Task GetVisibleEmployeeIdsAsync_ReturnsSelfAndReports_ForManager()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var manager = await environment.AddEmployeeAsync("VST-MGR-001", "vst-mgr@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var report = await environment.AddEmployeeAsync("VST-REP-001", "vst-rep@example.com", environment.DefaultDepartment.Id, managerId: manager.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var visible = await access.GetVisibleEmployeeIdsAsync(manager.Id, CancellationToken.None);

        Assert.Contains(manager.Id, visible);
        Assert.Contains(report.Id, visible);
    }

    [Fact]
    public async Task GetVisibleEmployeeIdsAsync_ReturnsAllActive_ForHRAdministrator()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var admin = await environment.AddEmployeeAsync("VST-HR-001", "vst-hr@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var employee = await environment.AddEmployeeAsync("VST-HR-EMP-001", "vst-hr-emp@example.com", environment.DefaultDepartment.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var visible = await access.GetVisibleEmployeeIdsAsync(admin.Id, CancellationToken.None);

        Assert.Contains(admin.Id, visible);
        Assert.Contains(employee.Id, visible);
    }

    [Fact]
    public async Task GetVisibleEmployeeIdsAsync_ReturnsAllActive_ForSystemAdministrator()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var admin = await environment.AddEmployeeAsync("VST-SYS-001", "vst-sys@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.SystemAdministrator);
        var employee = await environment.AddEmployeeAsync("VST-SYS-EMP-001", "vst-sys-emp@example.com", environment.DefaultDepartment.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var visible = await access.GetVisibleEmployeeIdsAsync(admin.Id, CancellationToken.None);

        Assert.Contains(admin.Id, visible);
        Assert.Contains(employee.Id, visible);
    }

    [Fact]
    public async Task GetVisibleEmployeeIdsAsync_ReturnsEmptySet_ForMissingRequester()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var visible = await access.GetVisibleEmployeeIdsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(visible);
    }

    [Fact]
    public async Task GetVisibleEmployeeIdsAsync_ReturnsEmptySet_ForDeletedRequester()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("VST-DEL-001", "vst-del@example.com", environment.DefaultDepartment!.Id, isDeleted: true);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var visible = await access.GetVisibleEmployeeIdsAsync(employee.Id, CancellationToken.None);

        Assert.Empty(visible);
    }

    [Fact]
    public async Task GetVisibleEmployeeIdsAsync_ReturnsEmptySet_ForTerminatedRequester()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("VST-TER-001", "vst-ter@example.com", environment.DefaultDepartment!.Id, status: EmployeeStatus.Terminated);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var visible = await access.GetVisibleEmployeeIdsAsync(employee.Id, CancellationToken.None);

        Assert.Empty(visible);
    }
}
