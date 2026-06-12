using HR.Application.Audit;
using HR.Application.DTOs.Audit;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Audit;

public class AuditAuthorizationTests
{
    [Theory]
    [InlineData(EmployeeRole.Employee, false)]
    [InlineData(EmployeeRole.Manager, false)]
    [InlineData(EmployeeRole.HRAdministrator, true)]
    [InlineData(EmployeeRole.SystemAdministrator, true)]
    public async Task SearchAsync_AllowsOnlyHrAndSystemAdministrators(EmployeeRole role, bool shouldSucceed)
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync(
            $"AUD-{role}-001",
            $"{role.ToString().ToLowerInvariant()}@example.com",
            environment.DefaultDepartment!.Id,
            role: role);
        environment.Context.AuditLogEntries.Add(new AuditLogEntry
        {
            EntityType = "Employee",
            EntityId = Guid.NewGuid(),
            ActionType = AuditActionType.Created,
            ActorEmployeeId = requester.Id,
            PerformedAt = new DateTimeOffset(2026, 6, 7, 10, 0, 0, TimeSpan.Zero),
            ChangedFields = "[]"
        });
        await environment.Context.SaveChangesAsync();
        var service = environment.GetRequiredService<IAuditLogService>();

        var result = await service.SearchAsync(requester.Id, new AuditLogQueryRequest(), CancellationToken.None);

        Assert.Equal(shouldSucceed, result.IsSuccess);
        if (!shouldSucceed)
        {
            Assert.Equal("FORBIDDEN", result.Error!.Code);
        }
    }

    [Fact]
    public async Task SearchAsync_WhenAdministratorIsTerminated_ReturnsForbidden()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync(
            "AUD-HR-TERM",
            "audit-terminated@example.com",
            environment.DefaultDepartment!.Id,
            status: EmployeeStatus.Terminated,
            terminatedAt: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            role: EmployeeRole.HRAdministrator);
        var service = environment.GetRequiredService<IAuditLogService>();

        var result = await service.SearchAsync(requester.Id, new AuditLogQueryRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }
}
