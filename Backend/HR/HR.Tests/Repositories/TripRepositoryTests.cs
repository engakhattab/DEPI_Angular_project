using HR.Infrastructure.Repositories;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Repositories;

public class TripRepositoryTests
{
    [Fact]
    public async Task GetPageAsync_ReturnsNewestFirstAndSupportsLookup()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync("EMP-330", "requester330@example.com", environment.DefaultDepartment!.Id);
        var older = await environment.AddTripAsync("Older Trip", new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero));
        var newer = await environment.AddTripAsync("Newer Trip", new DateTimeOffset(2026, 6, 2, 8, 0, 0, TimeSpan.Zero), requester.Id);
        var repository = new TripRepository(environment.Context);

        var page = await repository.GetPageAsync(1, 25, CancellationToken.None);
        var lookup = await repository.GetByIdAsync(older.Id, CancellationToken.None);
        var requesterLookup = await repository.GetByIdAsync(newer.Id, CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], page.Items.Select(t => t.Id).ToArray());
        Assert.NotNull(lookup);
        Assert.Equal("Older Trip", lookup!.ReferenceName);
        Assert.Null(lookup.RequestedByEmployeeId);
        Assert.Null(lookup.RequestedBy);
        Assert.NotNull(requesterLookup);
        Assert.Equal(requester.Id, requesterLookup!.RequestedByEmployeeId);
        Assert.Equal(requester.FullName, requesterLookup.RequestedBy!.FullName);
    }
}
