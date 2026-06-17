using HR.Infrastructure.Repositories;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Repositories;

public class TripRepositoryTests
{
    [Fact]
    public async Task GetPageByTravelersAsync_ReturnsNewestFirstAndSupportsLookup()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync("EMP-330", "requester330@example.com", environment.DefaultDepartment!.Id);
        var older = await environment.AddTripAsync("Older Trip", new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero));
        var newer = await environment.AddTripAsync("Newer Trip", new DateTimeOffset(2026, 6, 2, 8, 0, 0, TimeSpan.Zero), requester.Id);
        var repository = new TripRepository(environment.Context);

        var allowedIds = new HashSet<Guid> { requester.Id };
        var page = await repository.GetPageByTravelersAsync(allowedIds, null, 1, 25, CancellationToken.None);
        var lookup = await repository.GetByIdAsync(older.Id, CancellationToken.None);
        var requesterLookup = await repository.GetByIdAsync(newer.Id, CancellationToken.None);

        Assert.Equal([newer.Id], page.Items.Select(t => t.Id).ToArray());
        Assert.NotNull(lookup);
        Assert.Equal("Older Trip", lookup!.ReferenceName);
        Assert.Null(lookup.RequestedByEmployeeId);
        Assert.Null(lookup.RequestedBy);
        Assert.NotNull(requesterLookup);
        Assert.Equal(requester.Id, requesterLookup!.RequestedByEmployeeId);
        Assert.Equal(requester.FullName, requesterLookup.RequestedBy!.FullName);
    }

    [Fact]
    public async Task GetPageByTravelersAsync_WithOptionalTravelerFilter_ReturnsOnlyMatching()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var emp1 = await environment.AddEmployeeAsync("EMP-331", "emp331@example.com", environment.DefaultDepartment!.Id);
        var emp2 = await environment.AddEmployeeAsync("EMP-332", "emp332@example.com", environment.DefaultDepartment!.Id);
        var trip1 = await environment.AddTripAsync("Trip 1", new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero), emp1.Id);
        var trip2 = await environment.AddTripAsync("Trip 2", new DateTimeOffset(2026, 6, 2, 8, 0, 0, TimeSpan.Zero), emp2.Id);
        var repository = new TripRepository(environment.Context);

        var allowedIds = new HashSet<Guid> { emp1.Id, emp2.Id };
        var page = await repository.GetPageByTravelersAsync(allowedIds, emp1.Id, 1, 25, CancellationToken.None);

        Assert.Single(page.Items);
        Assert.Equal(trip1.Id, page.Items[0].Id);
    }

    [Fact]
    public async Task GetPageByTravelersAsync_WithOutOfScopeFilter_ReturnsEmptyPage()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var emp1 = await environment.AddEmployeeAsync("EMP-333", "emp333@example.com", environment.DefaultDepartment!.Id);
        var emp2 = await environment.AddEmployeeAsync("EMP-334", "emp334@example.com", environment.DefaultDepartment!.Id);
        await environment.AddTripAsync("Some Trip", new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero), emp1.Id);
        var repository = new TripRepository(environment.Context);

        var allowedIds = new HashSet<Guid> { emp1.Id };
        var page = await repository.GetPageByTravelersAsync(allowedIds, emp2.Id, 1, 25, CancellationToken.None);

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task Lookups_IncludeTravelerAndRequesterWhenPresent()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var traveler = await environment.AddEmployeeAsync("EMP-335", "emp335@example.com", environment.DefaultDepartment!.Id);
        var requester = await environment.AddEmployeeAsync("EMP-336", "emp336@example.com", environment.DefaultDepartment!.Id);
        var trip = await environment.AddTripAsync(
            "Requester Metadata Trip",
            new DateTimeOffset(2026, 6, 3, 8, 0, 0, TimeSpan.Zero),
            traveler.Id,
            requester.Id);
        var repository = new TripRepository(environment.Context);

        var lookup = await repository.GetByIdAsync(trip.Id, CancellationToken.None);
        var trackedLookup = await repository.GetTrackedByIdAsync(trip.Id, CancellationToken.None);

        Assert.NotNull(lookup);
        Assert.Equal(traveler.FullName, lookup!.RequestedBy!.FullName);
        Assert.Equal(requester.Id, lookup.RequesterEmployeeId);
        Assert.Equal(requester.FullName, lookup.Requester!.FullName);
        Assert.NotNull(trackedLookup);
        Assert.Equal(requester.FullName, trackedLookup!.Requester!.FullName);
    }
}
