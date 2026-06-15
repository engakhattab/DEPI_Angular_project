using HR.Application.Authorization;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Authorization;

public class VisibleEmployeeScopeTests
{
    [Fact]
    public async Task ManagerVisibleEmployeeIds_ExcludesSuspendedReport()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var manager = await environment.AddEmployeeAsync("VES-SUS-001", "ves-sus-mgr@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var suspendedReport = await environment.AddEmployeeAsync("VES-SUS-REP-001", "ves-sus-rep@example.com", environment.DefaultDepartment.Id, managerId: manager.Id, status: EmployeeStatus.Suspended);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var visible = await access.GetVisibleEmployeeIdsAsync(manager.Id, CancellationToken.None);

        Assert.DoesNotContain(suspendedReport.Id, visible);
    }

    [Fact]
    public async Task ManagerVisibleEmployeeIds_ExcludesSoftDeletedReport()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var manager = await environment.AddEmployeeAsync("VES-DEL-001", "ves-del-mgr@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var deletedReport = await environment.AddEmployeeAsync("VES-DEL-REP-001", "ves-del-rep@example.com", environment.DefaultDepartment.Id, managerId: manager.Id, isDeleted: true);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var visible = await access.GetVisibleEmployeeIdsAsync(manager.Id, CancellationToken.None);

        Assert.DoesNotContain(deletedReport.Id, visible);
    }

    [Fact]
    public async Task ManagerVisibleEmployeeIds_ExcludesTerminatedReport()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var manager = await environment.AddEmployeeAsync("VES-TER-001", "ves-ter-mgr@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var terminatedReport = await environment.AddEmployeeAsync("VES-TER-REP-001", "ves-ter-rep@example.com", environment.DefaultDepartment.Id, managerId: manager.Id, status: EmployeeStatus.Terminated);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        var visible = await access.GetVisibleEmployeeIdsAsync(manager.Id, CancellationToken.None);

        Assert.DoesNotContain(terminatedReport.Id, visible);
    }
}
