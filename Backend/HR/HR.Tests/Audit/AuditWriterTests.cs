using System.Text.Json;
using HR.Application.Audit;
using HR.Application.DTOs.Audit;
using HR.Domain.Enums;
using HR.Infrastructure.Audit;
using HR.Infrastructure.Authorization;
using HR.Infrastructure.Repositories;
using HR.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace HR.Tests.Audit;

public class AuditWriterTests
{
    [Fact]
    public async Task WriteAsync_WhenValuesAreNonSensitive_StoresBeforeAfterValuesAndChangedFields()
    {
        var performedAt = new DateTimeOffset(2026, 6, 7, 10, 15, 0, TimeSpan.Zero);
        await using var environment = await SqliteTestEnvironment.CreateAsync(
            seedDefaultDepartment: true,
            timeProvider: new TestTimeProvider(performedAt));
        var actor = await environment.AddEmployeeAsync("AUD-001", "auditor@example.com", environment.DefaultDepartment!.Id);
        var entityId = Guid.NewGuid();
        var writer = environment.GetRequiredService<IAuditWriter>();

        await writer.WriteAsync(
            "Employee",
            entityId,
            AuditActionType.Updated,
            actor.Id,
            actorMarker: null,
            ["FullName", "JobTitle"],
            new { fullName = "Old Name", jobTitle = "Analyst" },
            new { fullName = "New Name", jobTitle = "Senior Analyst" },
            sensitiveSummary: null,
            CancellationToken.None);
        await environment.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

        var entry = await environment.Context.AuditLogEntries.SingleAsync();
        Assert.Equal("Employee", entry.EntityType);
        Assert.Equal(entityId, entry.EntityId);
        Assert.Equal(AuditActionType.Updated, entry.ActionType);
        Assert.Equal(actor.Id, entry.ActorEmployeeId);
        Assert.Null(entry.ActorMarker);
        Assert.Equal(performedAt, entry.PerformedAt);
        Assert.Null(entry.SensitiveSummary);

        Assert.Equal(["FullName", "JobTitle"], JsonSerializer.Deserialize<string[]>(entry.ChangedFields));
        using var oldValues = JsonDocument.Parse(entry.OldValues!);
        using var newValues = JsonDocument.Parse(entry.NewValues!);
        Assert.Equal("Old Name", oldValues.RootElement.GetProperty("fullName").GetString());
        Assert.Equal("New Name", newValues.RootElement.GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task WriteAsync_WhenValuesAreSensitive_StoresSummaryWithoutBeforeAfterValues()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var actor = await environment.AddEmployeeAsync("AUD-002", "comp-auditor@example.com", environment.DefaultDepartment!.Id);
        var writer = environment.GetRequiredService<IAuditWriter>();

        await writer.WriteAsync(
            "EmployeeCompensation",
            Guid.NewGuid(),
            AuditActionType.CompensationChanged,
            actor.Id,
            actorMarker: null,
            ["BaseSalary"],
            oldValues: null,
            newValues: null,
            sensitiveSummary: "Compensation values changed.",
            CancellationToken.None);
        await environment.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

        var entry = await environment.Context.AuditLogEntries.SingleAsync();
        Assert.Null(entry.OldValues);
        Assert.Null(entry.NewValues);
        Assert.Equal("Compensation values changed.", entry.SensitiveSummary);
        Assert.DoesNotContain("100000", entry.ChangedFields + entry.SensitiveSummary);
    }

    [Fact]
    public async Task WriteAsync_WhenBootstrapCreatesInitialAdmin_StoresSystemActorAuditWithoutSecrets()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var writer = environment.GetRequiredService<IAuditWriter>();

        await writer.WriteAsync(
            "Employee",
            Guid.NewGuid(),
            AuditActionType.InitialAdminCreated,
            actorEmployeeId: null,
            InitialSystemAdminBootstrapper.SystemActorMarker,
            ["Role"],
            oldValues: null,
            new { employeeNumber = "BOOT-001", email = "bootstrap@example.com", role = EmployeeRole.SystemAdministrator.ToString() },
            sensitiveSummary: null,
            CancellationToken.None);
        await environment.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

        var entry = await environment.Context.AuditLogEntries.SingleAsync();
        Assert.Equal(AuditActionType.InitialAdminCreated, entry.ActionType);
        Assert.Null(entry.ActorEmployeeId);
        Assert.Equal(InitialSystemAdminBootstrapper.SystemActorMarker, entry.ActorMarker);
        Assert.Contains("BOOT-001", entry.NewValues);
        Assert.Contains(EmployeeRole.SystemAdministrator.ToString(), entry.NewValues);
        Assert.DoesNotContain("password", entry.NewValues ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", entry.NewValues ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stamp", entry.NewValues ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FailedValidationOrUnauthorizedAttempts_DoNotCreateAuditEntriesWhenWriterIsNotCalled()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var admin = await environment.AddEmployeeAsync(
            "AUD-HR-004",
            "audit-validation@example.com",
            environment.DefaultDepartment!.Id,
            role: EmployeeRole.HRAdministrator);
        var regular = await environment.AddEmployeeAsync("AUD-003", "regular@example.com", environment.DefaultDepartment.Id);
        var service = environment.GetRequiredService<IAuditLogService>();

        var validationFailure = await service.SearchAsync(
            admin.Id,
            new AuditLogQueryRequest { Action = "InvalidAction" },
            CancellationToken.None);
        var unauthorizedFailure = await service.SearchAsync(
            regular.Id,
            new AuditLogQueryRequest(),
            CancellationToken.None);

        Assert.True(validationFailure.IsFailure);
        Assert.Equal("VALIDATION_ERROR", validationFailure.Error!.Code);
        Assert.True(unauthorizedFailure.IsFailure);
        Assert.Equal("FORBIDDEN", unauthorizedFailure.Error!.Code);
        Assert.Empty(await environment.Context.AuditLogEntries.ToListAsync());
    }
}
