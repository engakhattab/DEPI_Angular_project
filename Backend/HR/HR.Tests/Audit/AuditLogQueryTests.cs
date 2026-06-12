using System.Text.Json;
using HR.Application.Audit;
using HR.Application.DTOs.Audit;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Audit;

public class AuditLogQueryTests
{
    [Fact]
    public async Task SearchAsync_FiltersByEntityTypeEntityIdActorActionAndDateRange()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var admin = await environment.AddEmployeeAsync(
            "AUD-HR-001",
            "audit-hr@example.com",
            environment.DefaultDepartment!.Id,
            role: EmployeeRole.HRAdministrator);
        var actor = await environment.AddEmployeeAsync("AUD-ACTOR-001", "audit-actor@example.com", environment.DefaultDepartment.Id);
        var targetEntityId = Guid.NewGuid();
        var matching = NewEntry(
            "Employee",
            targetEntityId,
            AuditActionType.Updated,
            actor.Id,
            new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero));
        environment.Context.AuditLogEntries.AddRange(
            NewEntry("Department", targetEntityId, AuditActionType.Updated, actor.Id, new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero)),
            NewEntry("Employee", Guid.NewGuid(), AuditActionType.Updated, actor.Id, new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero)),
            NewEntry("Employee", targetEntityId, AuditActionType.Created, actor.Id, new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero)),
            NewEntry("Employee", targetEntityId, AuditActionType.Updated, admin.Id, new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero)),
            NewEntry("Employee", targetEntityId, AuditActionType.Updated, actor.Id, new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)),
            NewEntry("Employee", targetEntityId, AuditActionType.Updated, actor.Id, new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero)),
            matching);
        await environment.Context.SaveChangesAsync();
        var service = environment.GetRequiredService<IAuditLogService>();

        var result = await service.SearchAsync(
            admin.Id,
            new AuditLogQueryRequest
            {
                EntityType = "Employee",
                EntityId = targetEntityId,
                ActorEmployeeId = actor.Id,
                Action = AuditActionType.Updated.ToString(),
                From = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero),
                To = new DateTimeOffset(2026, 6, 8, 0, 0, 0, TimeSpan.Zero),
                Page = 1,
                PageSize = 25
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(matching.Id, item.Id);
        Assert.Equal("Employee", item.EntityType);
        Assert.Equal(targetEntityId, item.EntityId);
        Assert.Equal(actor.Id, item.ActorEmployeeId);
        Assert.Equal(AuditActionType.Updated, item.ActionType);
        Assert.Equal(["Field"], item.ChangedFields);
        Assert.NotNull(item.OldValues);
        Assert.NotNull(item.NewValues);
    }

    [Fact]
    public async Task SearchAsync_ReturnsPagedResultsInNewestFirstOrder()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var admin = await environment.AddEmployeeAsync(
            "AUD-HR-002",
            "audit-hr-2@example.com",
            environment.DefaultDepartment!.Id,
            role: EmployeeRole.SystemAdministrator);
        var oldest = NewEntry("Employee", Guid.NewGuid(), AuditActionType.Created, admin.Id, new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero));
        var middle = NewEntry("Employee", Guid.NewGuid(), AuditActionType.Updated, admin.Id, new DateTimeOffset(2026, 6, 2, 8, 0, 0, TimeSpan.Zero));
        var newest = NewEntry("Employee", Guid.NewGuid(), AuditActionType.Deleted, admin.Id, new DateTimeOffset(2026, 6, 3, 8, 0, 0, TimeSpan.Zero));
        environment.Context.AuditLogEntries.AddRange(oldest, middle, newest);
        await environment.Context.SaveChangesAsync();
        var service = environment.GetRequiredService<IAuditLogService>();

        var firstPage = await service.SearchAsync(admin.Id, new AuditLogQueryRequest { Page = 1, PageSize = 2 }, CancellationToken.None);
        var secondPage = await service.SearchAsync(admin.Id, new AuditLogQueryRequest { Page = 2, PageSize = 2 }, CancellationToken.None);

        Assert.True(firstPage.IsSuccess, firstPage.Error?.Message);
        Assert.True(secondPage.IsSuccess, secondPage.Error?.Message);
        Assert.Equal(3, firstPage.Value!.TotalCount);
        Assert.Equal(2, firstPage.Value.PageSize);
        Assert.Equal([newest.Id, middle.Id], firstPage.Value.Items.Select(i => i.Id).ToArray());
        Assert.Equal([oldest.Id], secondPage.Value!.Items.Select(i => i.Id).ToArray());
    }

    [Fact]
    public async Task SearchAsync_WhenActionFilterIsInvalid_ReturnsValidationFailure()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var admin = await environment.AddEmployeeAsync(
            "AUD-HR-003",
            "audit-hr-3@example.com",
            environment.DefaultDepartment!.Id,
            role: EmployeeRole.HRAdministrator);
        var service = environment.GetRequiredService<IAuditLogService>();

        var result = await service.SearchAsync(admin.Id, new AuditLogQueryRequest { Action = "NotReal" }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    private static AuditLogEntry NewEntry(
        string entityType,
        Guid entityId,
        AuditActionType actionType,
        Guid? actorEmployeeId,
        DateTimeOffset performedAt)
    {
        return new AuditLogEntry
        {
            EntityType = entityType,
            EntityId = entityId,
            ActionType = actionType,
            ActorEmployeeId = actorEmployeeId,
            PerformedAt = performedAt,
            ChangedFields = JsonSerializer.Serialize(new[] { "Field" }),
            OldValues = JsonSerializer.Serialize(new { field = "old" }),
            NewValues = JsonSerializer.Serialize(new { field = "new" })
        };
    }
}
