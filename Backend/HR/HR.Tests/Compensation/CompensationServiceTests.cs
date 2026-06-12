using HR.Application.Compensation;
using HR.Application.DTOs.Compensation;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace HR.Tests.Compensation;

public class CompensationServiceTests
{
    [Fact]
    public async Task UpdateAsync_WhenCompensationDoesNotExist_CreatesCompensationHistoryAndAudit()
    {
        var now = new DateTimeOffset(2026, 6, 7, 10, 0, 0, TimeSpan.Zero);
        await using var environment = await SqliteTestEnvironment.CreateAsync(
            seedDefaultDepartment: true,
            timeProvider: new TestTimeProvider(now));
        var requester = await environment.AddEmployeeAsync("COMP-HR-001", "comp-hr-1@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var employee = await environment.AddEmployeeAsync("COMP-EMP-001", "comp-employee-1@example.com", environment.DefaultDepartment.Id);
        var service = environment.GetRequiredService<ICompensationService>();

        var result = await service.UpdateAsync(
            requester.Id,
            employee.Id,
            new CompensationUpdateRequest
            {
                BaseSalary = 12000m,
                SalaryCurrency = "egp",
                LastSalaryReviewDate = new DateOnly(2026, 6, 1)
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(employee.Id, result.Value!.EmployeeId);
        Assert.Equal(12000m, result.Value.BaseSalary);
        Assert.Equal("EGP", result.Value.SalaryCurrency);
        var history = Assert.Single(result.Value.History);
        Assert.Null(history.PreviousBaseSalary);
        Assert.Equal(12000m, history.NewBaseSalary);
        Assert.Null(history.PreviousCurrency);
        Assert.Equal("EGP", history.NewCurrency);
        Assert.Equal(requester.Id, history.ChangedByEmployeeId);
        Assert.Equal(now, history.ChangedAt);

        var compensation = await environment.Context.EmployeeCompensations.SingleAsync();
        Assert.Equal(employee.Id, compensation.EmployeeId);
        Assert.Equal(now, compensation.CreatedAt);
        Assert.Single(await environment.Context.SalaryHistoryEntries.ToListAsync());
        var audit = await environment.Context.AuditLogEntries.SingleAsync();
        Assert.Equal(AuditActionType.CompensationChanged, audit.ActionType);
        Assert.Equal("Compensation values changed.", audit.SensitiveSummary);
        Assert.Null(audit.OldValues);
        Assert.Null(audit.NewValues);
    }

    [Fact]
    public async Task UpdateAsync_WhenValuesChange_UpdatesCompensationAndAddsHistory()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 6, 7, 10, 0, 0, TimeSpan.Zero));
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true, timeProvider: clock);
        var requester = await environment.AddEmployeeAsync("COMP-HR-002", "comp-hr-2@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.SystemAdministrator);
        var employee = await environment.AddEmployeeAsync("COMP-EMP-002", "comp-employee-2@example.com", environment.DefaultDepartment.Id);
        var service = environment.GetRequiredService<ICompensationService>();
        await service.UpdateAsync(requester.Id, employee.Id, new CompensationUpdateRequest { BaseSalary = 10000m, SalaryCurrency = "EGP" }, CancellationToken.None);
        clock.SetUtcNow(new DateTimeOffset(2026, 7, 7, 10, 0, 0, TimeSpan.Zero));

        var result = await service.UpdateAsync(
            requester.Id,
            employee.Id,
            new CompensationUpdateRequest
            {
                BaseSalary = 13000m,
                SalaryCurrency = "USD",
                LastSalaryReviewDate = new DateOnly(2026, 7, 1)
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(13000m, result.Value!.BaseSalary);
        Assert.Equal("USD", result.Value.SalaryCurrency);
        Assert.Equal(new DateOnly(2026, 7, 1), result.Value.LastSalaryReviewDate);
        Assert.Equal(2, result.Value.History.Count);
        var latest = result.Value.History.First();
        Assert.Equal(10000m, latest.PreviousBaseSalary);
        Assert.Equal(13000m, latest.NewBaseSalary);
        Assert.Equal("EGP", latest.PreviousCurrency);
        Assert.Equal("USD", latest.NewCurrency);
        Assert.Equal(clock.GetUtcNow(), latest.ChangedAt);
        Assert.Equal(2, await environment.Context.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_WhenValuesDoNotChange_DoesNotAddHistoryOrAudit()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync("COMP-HR-003", "comp-hr-3@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var employee = await environment.AddEmployeeAsync("COMP-EMP-003", "comp-employee-3@example.com", environment.DefaultDepartment.Id);
        var service = environment.GetRequiredService<ICompensationService>();
        await service.UpdateAsync(requester.Id, employee.Id, new CompensationUpdateRequest { BaseSalary = 10000m, SalaryCurrency = "EGP" }, CancellationToken.None);

        var result = await service.UpdateAsync(requester.Id, employee.Id, new CompensationUpdateRequest { BaseSalary = 10000m, SalaryCurrency = "egp" }, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Single(result.Value!.History);
        Assert.Equal(1, await environment.Context.SalaryHistoryEntries.CountAsync());
        Assert.Equal(1, await environment.Context.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_WhenSalaryIsNegative_ReturnsBusinessRuleAndDoesNotPersist()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync("COMP-HR-004", "comp-hr-4@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var employee = await environment.AddEmployeeAsync("COMP-EMP-004", "comp-employee-4@example.com", environment.DefaultDepartment.Id);
        var service = environment.GetRequiredService<ICompensationService>();

        var result = await service.UpdateAsync(requester.Id, employee.Id, new CompensationUpdateRequest { BaseSalary = -1m, SalaryCurrency = "EGP" }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
        Assert.Empty(await environment.Context.EmployeeCompensations.ToListAsync());
        Assert.Empty(await environment.Context.SalaryHistoryEntries.ToListAsync());
        Assert.Empty(await environment.Context.AuditLogEntries.ToListAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("EG")]
    [InlineData("EGP1")]
    [InlineData("TOOLONGGG")]
    public async Task UpdateAsync_WhenCurrencyIsInvalid_ReturnsBusinessRule(string currency)
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync($"COMP-HR-{Guid.NewGuid():N}"[..16], $"comp-hr-{Guid.NewGuid():N}@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var employee = await environment.AddEmployeeAsync($"COMP-EMP-{Guid.NewGuid():N}"[..17], $"comp-employee-{Guid.NewGuid():N}@example.com", environment.DefaultDepartment.Id);
        var service = environment.GetRequiredService<ICompensationService>();

        var result = await service.UpdateAsync(requester.Id, employee.Id, new CompensationUpdateRequest { BaseSalary = 10000m, SalaryCurrency = currency }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task GetAndUpdateAsync_WhenEmployeeIsMissing_ReturnNotFound()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync("COMP-HR-005", "comp-hr-5@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var service = environment.GetRequiredService<ICompensationService>();
        var missingId = Guid.NewGuid();

        var get = await service.GetAsync(requester.Id, missingId, CancellationToken.None);
        var update = await service.UpdateAsync(requester.Id, missingId, new CompensationUpdateRequest { BaseSalary = 10000m, SalaryCurrency = "EGP" }, CancellationToken.None);

        Assert.True(get.IsFailure);
        Assert.Equal("NOT_FOUND", get.Error!.Code);
        Assert.True(update.IsFailure);
        Assert.Equal("NOT_FOUND", update.Error!.Code);
    }
}
