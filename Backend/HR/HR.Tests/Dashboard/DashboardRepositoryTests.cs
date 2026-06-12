using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Repositories;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Dashboard;

public class DashboardRepositoryTests
{
    [Fact]
    public async Task DashboardRepository_ReturnsOrganizationWideMetricCounts()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var sales = await environment.AddDepartmentAsync("Sales");
        var activeEngineering = await environment.AddEmployeeAsync("DASH-ENG-001", "dash-eng-1@example.com", environment.DefaultDepartment!.Id);
        var activeSales = await environment.AddEmployeeAsync("DASH-SALES-001", "dash-sales-1@example.com", sales.Id);
        var suspended = await environment.AddEmployeeAsync("DASH-SUSP-001", "dash-susp@example.com", sales.Id, status: EmployeeStatus.Suspended);
        var deleted = await environment.AddEmployeeAsync("DASH-DEL-001", "dash-deleted@example.com", sales.Id, isDeleted: true);
        activeEngineering.JoinDate = new DateOnly(2026, 6, 2);
        activeSales.JoinDate = new DateOnly(2026, 5, 20);
        suspended.JoinDate = new DateOnly(2026, 6, 4);
        await environment.Context.SaveChangesAsync();
        await environment.AddVacationRequestAsync(activeEngineering.Id, VacationRequestStatus.Pending, DateTimeOffset.UtcNow);
        await environment.AddVacationRequestAsync(activeEngineering.Id, VacationRequestStatus.Approved, DateTimeOffset.UtcNow, new DateOnly(2026, 6, 7), new DateOnly(2026, 6, 8));
        await environment.AddVacationRequestAsync(activeSales.Id, VacationRequestStatus.Approved, DateTimeOffset.UtcNow, new DateOnly(2026, 6, 20), new DateOnly(2026, 6, 21));
        await environment.AddVacationRequestAsync(activeSales.Id, VacationRequestStatus.Rejected, DateTimeOffset.UtcNow, new DateOnly(2026, 6, 22), new DateOnly(2026, 6, 23));
        await environment.AddTripAsync("Upcoming", DateTimeOffset.UtcNow, activeEngineering.Id);
        await environment.AddTripAsync("No Requester", DateTimeOffset.UtcNow);
        environment.Context.Trips.Add(new Trip
        {
            ReferenceName = "Later",
            Project = "Project",
            Route = "Route",
            TripType = "Bus",
            TripDate = new DateOnly(2026, 7, 1),
            TripCode = "TRIP-LATER",
            RequestCode = "REQ-LATER",
            CreatedAt = DateTimeOffset.UtcNow,
            RequestedByEmployeeId = activeSales.Id
        });
        await environment.Context.SaveChangesAsync();
        var repository = environment.GetRequiredService<IDashboardRepository>();

        Assert.Equal(2, await repository.CountActiveEmployeesAsync(null, CancellationToken.None));
        Assert.Equal(2, await repository.CountDepartmentsAsync(CancellationToken.None));
        Assert.Equal(1, await repository.CountPendingVacationRequestsAsync(null, CancellationToken.None));
        Assert.Equal(2, await repository.CountApprovedVacationsThisMonthAsync(null, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), CancellationToken.None));
        Assert.Equal(1, await repository.CountEmployeesOnVacationTodayAsync(null, new DateOnly(2026, 6, 7), CancellationToken.None));
        Assert.Equal(2, await repository.CountNewHiresThisMonthAsync(null, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), CancellationToken.None));
        Assert.Equal(2, await repository.CountUpcomingTripsThisWeekAsync(null, new DateOnly(2026, 6, 7), new DateOnly(2026, 6, 14), CancellationToken.None));

        var perDepartment = await repository.CountEmployeesPerDepartmentAsync(null, CancellationToken.None);
        Assert.Equal(1, perDepartment["Engineering"]);
        Assert.Equal(1, perDepartment["Sales"]);

        var byStatus = await repository.CountVacationRequestsByStatusAsync(null, CancellationToken.None);
        Assert.Equal(1, byStatus[VacationRequestStatus.Pending.ToString()]);
        Assert.Equal(2, byStatus[VacationRequestStatus.Approved.ToString()]);
        Assert.Equal(1, byStatus[VacationRequestStatus.Rejected.ToString()]);
    }

    [Fact]
    public async Task DashboardRepository_WhenEmployeeScopeIsProvided_ReturnsOnlyScopedMetrics()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var sales = await environment.AddDepartmentAsync("Sales");
        var scoped = await environment.AddEmployeeAsync("DASH-SCOPE-001", "dash-scope@example.com", environment.DefaultDepartment!.Id);
        var unscoped = await environment.AddEmployeeAsync("DASH-UNSCOPE-001", "dash-unscope@example.com", sales.Id);
        scoped.JoinDate = new DateOnly(2026, 6, 2);
        unscoped.JoinDate = new DateOnly(2026, 6, 3);
        await environment.Context.SaveChangesAsync();
        await environment.AddVacationRequestAsync(scoped.Id, VacationRequestStatus.Pending, DateTimeOffset.UtcNow);
        await environment.AddVacationRequestAsync(unscoped.Id, VacationRequestStatus.Pending, DateTimeOffset.UtcNow);
        await environment.AddTripAsync("Scoped Trip", DateTimeOffset.UtcNow, scoped.Id);
        await environment.AddTripAsync("Unscoped Trip", DateTimeOffset.UtcNow, unscoped.Id);
        var repository = environment.GetRequiredService<IDashboardRepository>();
        var scope = new HashSet<Guid> { scoped.Id };

        Assert.Equal(1, await repository.CountActiveEmployeesAsync(scope, CancellationToken.None));
        Assert.Equal(1, await repository.CountPendingVacationRequestsAsync(scope, CancellationToken.None));
        Assert.Equal(1, await repository.CountNewHiresThisMonthAsync(scope, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), CancellationToken.None));
        Assert.Equal(1, await repository.CountUpcomingTripsThisWeekAsync(scope, new DateOnly(2026, 6, 7), new DateOnly(2026, 6, 14), CancellationToken.None));

        var perDepartment = await repository.CountEmployeesPerDepartmentAsync(scope, CancellationToken.None);
        Assert.Equal(["Engineering"], perDepartment.Keys.ToArray());
        var byStatus = await repository.CountVacationRequestsByStatusAsync(scope, CancellationToken.None);
        Assert.Equal(1, byStatus[VacationRequestStatus.Pending.ToString()]);
    }
}
