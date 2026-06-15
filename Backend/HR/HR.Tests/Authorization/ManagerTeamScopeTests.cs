using HR.Application.Authorization;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Authorization;

public class ManagerTeamScopeTests
{
    [Fact]
    public async Task IsManagerOfAsync_ReturnsTrue_ForDirectReport()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var manager = await environment.AddEmployeeAsync("MTS-DIR-001", "mts-direct@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var report = await environment.AddEmployeeAsync("MTS-DIR-REP-001", "mts-direct-rep@example.com", environment.DefaultDepartment.Id, managerId: manager.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.IsManagerOfAsync(manager.Id, report.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IsManagerOfAsync_ReturnsTrue_ForIndirectReport()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var manager = await environment.AddEmployeeAsync("MTS-IND-001", "mts-indirect@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var directReport = await environment.AddEmployeeAsync("MTS-IND-DIR-001", "mts-indirect-dir@example.com", environment.DefaultDepartment.Id, managerId: manager.Id);
        var indirectReport = await environment.AddEmployeeAsync("MTS-IND-REP-001", "mts-indirect-rep@example.com", environment.DefaultDepartment.Id, managerId: directReport.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.IsManagerOfAsync(manager.Id, indirectReport.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IsManagerOfAsync_ReturnsFalse_ForPeer()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var manager = await environment.AddEmployeeAsync("MTS-PEER-001", "mts-peer-mgr@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var peer = await environment.AddEmployeeAsync("MTS-PEER-002", "mts-peer@example.com", environment.DefaultDepartment.Id, role: EmployeeRole.Manager);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.IsManagerOfAsync(manager.Id, peer.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IsManagerOfAsync_ReturnsFalse_ForUnrelatedEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var manager = await environment.AddEmployeeAsync("MTS-UNR-001", "mts-unrelated-mgr@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var unrelated = await environment.AddEmployeeAsync("MTS-UNR-002", "mts-unrelated@example.com", environment.DefaultDepartment.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.IsManagerOfAsync(manager.Id, unrelated.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IsManagerOfAsync_ReturnsFalse_ForManagersOwnManager()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var topManager = await environment.AddEmployeeAsync("MTS-TOP-001", "mts-top@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var middleManager = await environment.AddEmployeeAsync("MTS-MID-001", "mts-middle@example.com", environment.DefaultDepartment.Id, role: EmployeeRole.Manager, managerId: topManager.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.False(await access.IsManagerOfAsync(middleManager.Id, topManager.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CanAccessEmployeeAsync_UsesManagerScope()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var manager = await environment.AddEmployeeAsync("MTS-ACS-001", "mts-access-mgr@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var report = await environment.AddEmployeeAsync("MTS-ACS-REP-001", "mts-access-rep@example.com", environment.DefaultDepartment.Id, managerId: manager.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.CanAccessEmployeeAsync(manager.Id, report.Id, CancellationToken.None));
    }
}
