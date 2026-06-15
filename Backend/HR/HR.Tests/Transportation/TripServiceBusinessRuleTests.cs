using HR.Application.DTOs.Transportation;
using HR.Application.Transportation;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Transportation;

public class TripServiceBusinessRuleTests
{
    [Fact]
    public async Task CreateTripAsync_AcceptsActiveRequesterOnFutureWorkingDay()
    {
        await using var environment = await CreateEnvironmentAsync();
        var employee = await environment.AddEmployeeAsync("EMP-1001", "employee1001@example.com", environment.DefaultDepartment!.Id);
        var service = environment.GetRequiredService<ITripService>();

        var result = await service.CreateTripAsync(
            employee.Id,
            BuildRequest(employee.Id, new DateOnly(2026, 6, 7)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(employee.Id, result.Value!.RequestedByEmployeeId);
        Assert.Equal(employee.FullName, result.Value.RequestedByEmployeeName);
    }

    [Fact]
    public async Task CreateTripAsync_RejectsMissingRequester()
    {
        await using var environment = await CreateEnvironmentAsync();
        var service = environment.GetRequiredService<ITripService>();

        var result = await service.CreateTripAsync(
            Guid.NewGuid(),
            BuildRequest(Guid.NewGuid(), new DateOnly(2026, 6, 7)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task CreateTripAsync_RejectsSuspendedRequesterAsTraveler()
    {
        await using var environment = await CreateEnvironmentAsync();
        var employee = await environment.AddEmployeeAsync(
            $"EMP-{Guid.NewGuid():N}"[..11].ToUpperInvariant(),
            $"{Guid.NewGuid():N}@example.com",
            environment.DefaultDepartment!.Id,
            status: EmployeeStatus.Suspended);
        var service = environment.GetRequiredService<ITripService>();

        var result = await service.CreateTripAsync(
            employee.Id,
            BuildRequest(employee.Id, new DateOnly(2026, 6, 7)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task CreateTripAsync_RejectsTerminatedRequester()
    {
        await using var environment = await CreateEnvironmentAsync();
        var employee = await environment.AddEmployeeAsync(
            $"EMP-{Guid.NewGuid():N}"[..11].ToUpperInvariant(),
            $"{Guid.NewGuid():N}@example.com",
            environment.DefaultDepartment!.Id,
            status: EmployeeStatus.Terminated,
            terminatedAt: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var service = environment.GetRequiredService<ITripService>();

        var result = await service.CreateTripAsync(
            employee.Id,
            BuildRequest(employee.Id, new DateOnly(2026, 6, 7)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task CreateTripAsync_RejectsDeletedRequester()
    {
        await using var environment = await CreateEnvironmentAsync();
        var employee = await environment.AddEmployeeAsync(
            $"EMP-{Guid.NewGuid():N}"[..11].ToUpperInvariant(),
            $"{Guid.NewGuid():N}@example.com",
            environment.DefaultDepartment!.Id,
            status: EmployeeStatus.Active,
            isDeleted: true,
            terminatedAt: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var service = environment.GetRequiredService<ITripService>();

        var result = await service.CreateTripAsync(
            employee.Id,
            BuildRequest(employee.Id, new DateOnly(2026, 6, 7)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Theory]
    [InlineData("2026-06-02")]
    [InlineData("2026-06-05")]
    [InlineData("2026-06-06")]
    public async Task CreateTripAsync_RejectsPastAndNonWorkingDates(string tripDateText)
    {
        await using var environment = await CreateEnvironmentAsync();
        var employee = await environment.AddEmployeeAsync("EMP-1002", "employee1002@example.com", environment.DefaultDepartment!.Id);
        var service = environment.GetRequiredService<ITripService>();

        var result = await service.CreateTripAsync(
            employee.Id,
            BuildRequest(employee.Id, DateOnly.Parse(tripDateText)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task GetTripResponses_HistoricalNullTraveler_ForbiddenForEmployee()
    {
        await using var environment = await CreateEnvironmentAsync();
        var employee = await environment.AddEmployeeAsync("EMP-1003", "employee1003@example.com", environment.DefaultDepartment!.Id);
        var service = environment.GetRequiredService<ITripService>();
        var historicalTrip = await environment.AddTripAsync("Historical Trip", new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));

        var byId = await service.GetTripByIdAsync(employee.Id, historicalTrip.Id, CancellationToken.None);
        var page = await service.GetTripsAsync(employee.Id, null, 1, 25, CancellationToken.None);

        Assert.False(byId.IsSuccess);
        Assert.Equal("FORBIDDEN", byId.Error!.Code);
        Assert.Empty(page.Value!.Items);
    }

    [Fact]
    public async Task GetTripResponses_HistoricalNullTraveler_AccessibleByHRAdmin()
    {
        await using var environment = await CreateEnvironmentAsync();
        var hr = await environment.AddEmployeeAsync("EMP-HR1", "emp-hr1@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var service = environment.GetRequiredService<ITripService>();
        var historicalTrip = await environment.AddTripAsync("Historical Trip", new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));

        var byId = await service.GetTripByIdAsync(hr.Id, historicalTrip.Id, CancellationToken.None);
        var page = await service.GetTripsAsync(hr.Id, null, 1, 25, CancellationToken.None);

        Assert.True(byId.IsSuccess);
        Assert.Equal(historicalTrip.Id, byId.Value!.Id);
        Assert.Null(byId.Value.RequestedByEmployeeId);
        Assert.Contains(page.Value!.Items, trip => trip.Id == historicalTrip.Id);
    }

    private static async Task<SqliteTestEnvironment> CreateEnvironmentAsync()
    {
        return await SqliteTestEnvironment.CreateAsync(
            seedDefaultDepartment: true,
            timeProvider: new TestTimeProvider(new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero)));
    }

    private static TripCreateRequest BuildRequest(Guid requestedByEmployeeId, DateOnly tripDate)
    {
        return new TripCreateRequest
        {
            ReferenceName = "Team Shuttle",
            Project = "HR Revamp",
            Route = "HQ to Client",
            TripType = "Business",
            TripDate = tripDate,
            RequestedByEmployeeId = requestedByEmployeeId
        };
    }
}
