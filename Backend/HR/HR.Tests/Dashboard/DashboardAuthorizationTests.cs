using HR.Application.Dashboard;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Dashboard;

public class DashboardAuthorizationTests
{
    [Fact]
    public async Task GetSummaryAsync_WhenRequesterIsEmployee_ReturnsForbidden()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("DASH-AUTH-EMP", "dash-auth-employee@example.com", environment.DefaultDepartment!.Id);
        var service = environment.GetRequiredService<IDashboardService>();

        var result = await service.GetSummaryAsync(employee.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenRequesterIsManager_ReturnsTeamScopedMetricsAndHidesDepartmentTotal()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 6, 7, 10, 0, 0, TimeSpan.Zero));
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true, timeProvider: clock);
        var manager = await environment.AddEmployeeAsync("DASH-MGR-001", "dash-manager@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var directReport = await environment.AddEmployeeAsync("DASH-DIR-001", "dash-direct@example.com", environment.DefaultDepartment.Id, managerId: manager.Id);
        var indirectReport = await environment.AddEmployeeAsync("DASH-IND-001", "dash-indirect@example.com", environment.DefaultDepartment.Id, managerId: directReport.Id);
        var unrelated = await environment.AddEmployeeAsync("DASH-UNREL-001", "dash-unrelated@example.com", environment.DefaultDepartment.Id);
        manager.JoinDate = new DateOnly(2026, 6, 1);
        directReport.JoinDate = new DateOnly(2026, 6, 2);
        indirectReport.JoinDate = new DateOnly(2026, 6, 3);
        unrelated.JoinDate = new DateOnly(2026, 6, 4);
        await environment.Context.SaveChangesAsync();
        await environment.AddVacationRequestAsync(directReport.Id, VacationRequestStatus.Pending, DateTimeOffset.UtcNow);
        await environment.AddVacationRequestAsync(indirectReport.Id, VacationRequestStatus.Approved, DateTimeOffset.UtcNow, new DateOnly(2026, 6, 7), new DateOnly(2026, 6, 8));
        await environment.AddVacationRequestAsync(unrelated.Id, VacationRequestStatus.Pending, DateTimeOffset.UtcNow);
        await environment.AddTripAsync("Team Trip", DateTimeOffset.UtcNow, directReport.Id);
        await environment.AddTripAsync("Unrelated Trip", DateTimeOffset.UtcNow, unrelated.Id);
        var service = environment.GetRequiredService<IDashboardService>();

        var result = await service.GetSummaryAsync(manager.Id, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(2, result.Value!.TotalActiveEmployees);
        Assert.Null(result.Value.TotalDepartments);
        Assert.Equal(1, result.Value.PendingVacationRequests);
        Assert.Equal(1, result.Value.ApprovedVacationsThisMonth);
        Assert.Equal(1, result.Value.EmployeesOnVacationToday);
        Assert.Equal(2, result.Value.NewHiresThisMonth);
        Assert.Equal(1, result.Value.UpcomingTripsThisWeek);
        Assert.Equal(2, result.Value.EmployeesPerDepartment["Engineering"]);
        Assert.Equal(1, result.Value.VacationRequestsByStatus[VacationRequestStatus.Pending.ToString()]);
        Assert.Equal(1, result.Value.VacationRequestsByStatus[VacationRequestStatus.Approved.ToString()]);
    }

    [Theory]
    [InlineData(EmployeeRole.HRAdministrator)]
    [InlineData(EmployeeRole.SystemAdministrator)]
    public async Task GetSummaryAsync_WhenRequesterIsAdministrator_ReturnsOrganizationWideMetrics(EmployeeRole role)
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var admin = await environment.AddEmployeeAsync($"DASH-{role}-001", $"dash-{role.ToString().ToLowerInvariant()}@example.com", environment.DefaultDepartment!.Id, role: role);
        await environment.AddEmployeeAsync("DASH-ORG-001", "dash-org-1@example.com", environment.DefaultDepartment.Id);
        await environment.AddEmployeeAsync("DASH-ORG-002", "dash-org-2@example.com", environment.DefaultDepartment.Id);
        var service = environment.GetRequiredService<IDashboardService>();

        var result = await service.GetSummaryAsync(admin.Id, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(3, result.Value!.TotalActiveEmployees);
        Assert.Equal(1, result.Value.TotalDepartments);
    }
}
