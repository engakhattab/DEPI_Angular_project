using HR.Application.Authorization;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Authorization;

public class AccessMatrixTests
{
    [Fact]
    public async Task CanAccessEmployeeAsync_EnforcesEmployeeManagerAndAdministratorVisibility()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("RBAC-EMP-001", "rbac-employee@example.com", environment.DefaultDepartment!.Id);
        var peer = await environment.AddEmployeeAsync("RBAC-PEER-001", "rbac-peer@example.com", environment.DefaultDepartment.Id);
        var manager = await environment.AddEmployeeAsync("RBAC-MGR-001", "rbac-manager@example.com", environment.DefaultDepartment.Id, role: EmployeeRole.Manager);
        var report = await environment.AddEmployeeAsync("RBAC-REP-001", "rbac-report@example.com", environment.DefaultDepartment.Id, managerId: manager.Id);
        var hrAdmin = await environment.AddEmployeeAsync("RBAC-HR-001", "rbac-hr@example.com", environment.DefaultDepartment.Id, role: EmployeeRole.HRAdministrator);
        var systemAdmin = await environment.AddEmployeeAsync("RBAC-SYS-001", "rbac-system@example.com", environment.DefaultDepartment.Id, role: EmployeeRole.SystemAdministrator);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.CanAccessEmployeeAsync(employee.Id, employee.Id, CancellationToken.None));
        Assert.False(await access.CanAccessEmployeeAsync(employee.Id, peer.Id, CancellationToken.None));
        Assert.True(await access.CanAccessEmployeeAsync(manager.Id, report.Id, CancellationToken.None));
        Assert.False(await access.CanAccessEmployeeAsync(manager.Id, peer.Id, CancellationToken.None));
        Assert.True(await access.CanAccessEmployeeAsync(hrAdmin.Id, employee.Id, CancellationToken.None));
        Assert.True(await access.CanAccessEmployeeAsync(systemAdmin.Id, employee.Id, CancellationToken.None));
    }

    [Theory]
    [InlineData(EmployeeRole.Employee, false, false, false)]
    [InlineData(EmployeeRole.Manager, true, false, false)]
    [InlineData(EmployeeRole.HRAdministrator, true, true, false)]
    [InlineData(EmployeeRole.SystemAdministrator, true, true, true)]
    public async Task HasAnyRoleAsync_UsesPhase7RoleMatrix(EmployeeRole role, bool isManagerPolicyAllowed, bool isHrPolicyAllowed, bool isSystemPolicyAllowed)
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync(
            $"RBAC-{role}-002",
            $"rbac-{role.ToString().ToLowerInvariant()}-2@example.com",
            environment.DefaultDepartment!.Id,
            role: role);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.True(await access.HasAnyRoleAsync(requester.Id, CancellationToken.None, EmployeeRole.Employee, EmployeeRole.Manager, EmployeeRole.HRAdministrator, EmployeeRole.SystemAdministrator));
        Assert.Equal(isManagerPolicyAllowed, await access.HasAnyRoleAsync(requester.Id, CancellationToken.None, EmployeeRole.Manager, EmployeeRole.HRAdministrator, EmployeeRole.SystemAdministrator));
        Assert.Equal(isHrPolicyAllowed, await access.HasAnyRoleAsync(requester.Id, CancellationToken.None, EmployeeRole.HRAdministrator, EmployeeRole.SystemAdministrator));
        Assert.Equal(isSystemPolicyAllowed, await access.HasAnyRoleAsync(requester.Id, CancellationToken.None, EmployeeRole.SystemAdministrator));
    }

    [Theory]
    [InlineData(EmployeeStatus.Active, false, true)]
    [InlineData(EmployeeStatus.Suspended, false, true)]
    [InlineData(EmployeeStatus.Terminated, false, false)]
    [InlineData(EmployeeStatus.Active, true, false)]
    public async Task HasAnyRoleAsync_RejectsDeletedAndTerminatedEmployees(EmployeeStatus status, bool isDeleted, bool expected)
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync(
            $"RBAC-ELIG-{Guid.NewGuid():N}"[..18],
            $"rbac-elig-{Guid.NewGuid():N}@example.com",
            environment.DefaultDepartment!.Id,
            status: status,
            isDeleted: isDeleted,
            role: EmployeeRole.HRAdministrator);
        var access = environment.GetRequiredService<IEmployeeAccessService>();

        Assert.Equal(expected, await access.HasAnyRoleAsync(requester.Id, CancellationToken.None, EmployeeRole.HRAdministrator));
    }
}
