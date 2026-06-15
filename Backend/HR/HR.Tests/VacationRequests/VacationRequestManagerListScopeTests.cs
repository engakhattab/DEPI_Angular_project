using HR.Domain.Enums;

namespace HR.Tests.VacationRequests;

public class VacationRequestManagerListScopeTests
{
    [Fact]
    public async Task GetVacationRequestsAsync_ManagerUnfiltered_ReturnsTeamRequestsOnly()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.GetVacationRequestsAsync(
            manager.Id, null, null, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.DoesNotContain(result.Value.Items, item => item.EmployeeId == manager.Id);
        Assert.Contains(result.Value.Items, item => item.EmployeeId == fixture.DirectReport!.Id);
        Assert.Contains(result.Value.Items, item => item.EmployeeId == fixture.IndirectReport!.Id);
    }

    [Fact]
    public async Task GetVacationRequestsAsync_ManagerSelfFilter_ReturnsOwnRequestsOnly()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.GetVacationRequestsAsync(
            manager.Id, null, manager.Id, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.All(result.Value.Items, item => Assert.Equal(manager.Id, item.EmployeeId));
    }

    [Fact]
    public async Task GetVacationRequestsAsync_ManagerTeamMemberFilter_ReturnsThatMembersRequests()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.GetVacationRequestsAsync(
            manager.Id, null, fixture.DirectReport!.Id, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.All(result.Value.Items, item => Assert.Equal(fixture.DirectReport.Id, item.EmployeeId));
    }

    [Fact]
    public async Task GetVacationRequestsAsync_ManagerOutOfScopeFilter_ReturnsEmptyPage()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.GetVacationRequestsAsync(
            manager.Id, null, fixture.OtherEmployee!.Id, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task GetVacationRequestsAsync_ManagerWithNoTeam_ReturnsEmptyPage()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = await fixture.Environment.AddEmployeeAsync(
            $"MGR-N-{Guid.NewGuid():N}"[..12],
            $"manager-no-team-{Guid.NewGuid():N}@test.com",
            fixture.Environment.DefaultDepartment!.Id,
            role: EmployeeRole.Manager);
        await fixture.Environment.AddVacationRequestAsync(
            manager.Id, VacationRequestStatus.Pending, fixture.UtcNow, workingDayCount: 2);
        await fixture.Environment.AddVacationRequestAsync(
            fixture.OtherEmployee!.Id, VacationRequestStatus.Pending, fixture.UtcNow, workingDayCount: 2);

        var result = await fixture.Service.GetVacationRequestsAsync(
            manager.Id, null, null, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetVacationRequestsAsync_ManagerWithStatusFilter_AppliesWithinScope()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.GetVacationRequestsAsync(
            manager.Id, VacationRequestStatus.Pending, null, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.All(result.Value.Items, item => Assert.Equal(VacationRequestStatus.Pending, item.Status));
        Assert.All(result.Value.Items, item => Assert.NotEqual(manager.Id, item.EmployeeId));
    }
}
