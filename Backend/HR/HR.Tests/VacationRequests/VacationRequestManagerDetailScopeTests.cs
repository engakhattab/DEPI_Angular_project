namespace HR.Tests.VacationRequests;

public class VacationRequestManagerDetailScopeTests
{
    [Fact]
    public async Task GetVacationRequestByIdAsync_ManagerOwnRequest_ReturnsDetail()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.GetVacationRequestByIdAsync(
            manager.Id, fixture.ManagerOwnRequest!.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(fixture.ManagerOwnRequest.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetVacationRequestByIdAsync_ManagerDirectReportRequest_ReturnsDetail()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.GetVacationRequestByIdAsync(
            manager.Id, fixture.DirectReportRequest!.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.DirectReportRequest.Id, result.Value!.Id);
    }

    [Fact]
    public async Task GetVacationRequestByIdAsync_ManagerIndirectReportRequest_ReturnsDetail()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.GetVacationRequestByIdAsync(
            manager.Id, fixture.IndirectReportRequest!.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.IndirectReportRequest.Id, result.Value!.Id);
    }

    [Fact]
    public async Task GetVacationRequestByIdAsync_ManagerOtherEmployeeRequest_ReturnsForbidden()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.GetVacationRequestByIdAsync(
            manager.Id, fixture.OtherEmployeeRequest!.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }
}
