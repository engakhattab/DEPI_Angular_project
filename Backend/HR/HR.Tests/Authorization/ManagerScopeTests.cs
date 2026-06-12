using HR.Application.Authorization;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Authorization;

public class ManagerScopeTests
{
    [Fact]
    public async Task ManagerCanAccessDirectAndIndirectReportsButNotPeersOrUnrelatedEmployees()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var manager = await environment.AddEmployeeAsync("MGR-SCOPE-001", "manager-scope@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var directReport = await environment.AddEmployeeAsync("MGR-DIRECT-001", "manager-direct@example.com", environment.DefaultDepartment.Id, managerId: manager.Id);
        var indirectReport = await environment.AddEmployeeAsync("MGR-INDIRECT-001", "manager-indirect@example.com", environment.DefaultDepartment.Id, managerId: directReport.Id);
        var peer = await environment.AddEmployeeAsync("MGR-PEER-001", "manager-peer@example.com", environment.DefaultDepartment.Id, role: EmployeeRole.Manager);
        var unrelated = await environment.AddEmployeeAsync("MGR-UNREL-001", "manager-unrelated@example.com", environment.DefaultDepartment.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.CanAccessEmployeeAsync(manager.Id, manager.Id, CancellationToken.None));
        Assert.True(await access.CanAccessEmployeeAsync(manager.Id, directReport.Id, CancellationToken.None));
        Assert.True(await access.CanAccessEmployeeAsync(manager.Id, indirectReport.Id, CancellationToken.None));
        Assert.False(await access.CanAccessEmployeeAsync(manager.Id, peer.Id, CancellationToken.None));
        Assert.False(await access.CanAccessEmployeeAsync(manager.Id, unrelated.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ManagerVisibleEmployeeIds_ContainsSelfAndActiveReportsOnly()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var manager = await environment.AddEmployeeAsync("MGR-SCOPE-002", "manager-scope-2@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var directReport = await environment.AddEmployeeAsync("MGR-DIRECT-002", "manager-direct-2@example.com", environment.DefaultDepartment.Id, managerId: manager.Id);
        var indirectReport = await environment.AddEmployeeAsync("MGR-INDIRECT-002", "manager-indirect-2@example.com", environment.DefaultDepartment.Id, managerId: directReport.Id);
        var softDeletedReport = await environment.AddEmployeeAsync("MGR-DELETED-002", "manager-deleted-2@example.com", environment.DefaultDepartment.Id, managerId: manager.Id, isDeleted: true);
        var suspendedReport = await environment.AddEmployeeAsync("MGR-SUSP-002", "manager-suspended-2@example.com", environment.DefaultDepartment.Id, managerId: manager.Id, status: EmployeeStatus.Suspended);
        var unrelated = await environment.AddEmployeeAsync("MGR-UNREL-002", "manager-unrelated-2@example.com", environment.DefaultDepartment.Id);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var visible = await access.GetVisibleEmployeeIdsAsync(manager.Id, CancellationToken.None);

        Assert.Contains(manager.Id, visible);
        Assert.Contains(directReport.Id, visible);
        Assert.Contains(indirectReport.Id, visible);
        Assert.DoesNotContain(softDeletedReport.Id, visible);
        Assert.DoesNotContain(suspendedReport.Id, visible);
        Assert.DoesNotContain(unrelated.Id, visible);
    }
}
