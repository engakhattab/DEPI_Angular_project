using HR.Application.Compensation;
using HR.Application.DTOs.Compensation;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace HR.Tests.Compensation;

public class CompensationAuthorizationTests
{
    [Theory]
    [InlineData(EmployeeRole.Employee, false)]
    [InlineData(EmployeeRole.Manager, false)]
    [InlineData(EmployeeRole.HRAdministrator, true)]
    [InlineData(EmployeeRole.SystemAdministrator, true)]
    public async Task GetAsync_AllowsOnlyHrAndSystemAdministrators(EmployeeRole requesterRole, bool shouldSucceed)
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync(
            $"COMP-AUTH-{requesterRole}-{Guid.NewGuid():N}"[..20],
            $"comp-auth-{requesterRole.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@example.com",
            environment.DefaultDepartment!.Id,
            role: requesterRole);
        var employee = await environment.AddEmployeeAsync($"COMP-TGT-{Guid.NewGuid():N}"[..18], $"comp-target-{Guid.NewGuid():N}@example.com", environment.DefaultDepartment.Id);
        var service = environment.GetRequiredService<ICompensationService>();

        var result = await service.GetAsync(requester.Id, employee.Id, CancellationToken.None);

        Assert.Equal(shouldSucceed, result.IsSuccess);
        if (!shouldSucceed)
        {
            Assert.Equal("FORBIDDEN", result.Error!.Code);
        }
    }

    [Fact]
    public async Task UpdateAsync_WhenRequesterIsNotAuthorized_DoesNotPersistCompensation()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync("COMP-AUTH-EMP", "comp-auth-employee@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Employee);
        var employee = await environment.AddEmployeeAsync("COMP-AUTH-TGT", "comp-auth-target@example.com", environment.DefaultDepartment.Id);
        var service = environment.GetRequiredService<ICompensationService>();

        var result = await service.UpdateAsync(requester.Id, employee.Id, new CompensationUpdateRequest { BaseSalary = 10000m, SalaryCurrency = "EGP" }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Empty(await environment.Context.EmployeeCompensations.ToListAsync());
        Assert.Empty(await environment.Context.SalaryHistoryEntries.ToListAsync());
        Assert.Empty(await environment.Context.AuditLogEntries.ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_WhenRequesterIsTerminatedAdministrator_ReturnsForbidden()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync(
            "COMP-AUTH-TERM",
            "comp-auth-terminated@example.com",
            environment.DefaultDepartment!.Id,
            status: EmployeeStatus.Terminated,
            role: EmployeeRole.HRAdministrator);
        var employee = await environment.AddEmployeeAsync("COMP-AUTH-TGT2", "comp-auth-target-2@example.com", environment.DefaultDepartment.Id);
        var service = environment.GetRequiredService<ICompensationService>();

        var result = await service.UpdateAsync(requester.Id, employee.Id, new CompensationUpdateRequest { BaseSalary = 10000m, SalaryCurrency = "EGP" }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }
}
