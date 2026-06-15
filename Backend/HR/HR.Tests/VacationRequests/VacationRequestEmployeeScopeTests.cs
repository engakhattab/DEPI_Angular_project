using HR.Application.DTOs.VacationRequests;
using HR.Domain.Enums;
using HR.Shared.Results;

namespace HR.Tests.VacationRequests;

public class VacationRequestEmployeeScopeTests
{
    [Theory]
    [InlineData(EmployeeStatus.Active, true)]
    [InlineData(EmployeeStatus.Terminated, false)]
    public async Task GetVacationRequestsAsync_DeletedOrTerminatedRequester_ReturnsForbidden(
        EmployeeStatus status,
        bool isDeleted)
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var requester = await fixture.Environment.AddEmployeeAsync(
            $"REQ-L-{Guid.NewGuid():N}"[..10],
            $"requester-list-{Guid.NewGuid():N}@test.com",
            fixture.Environment.DefaultDepartment!.Id,
            status: status,
            isDeleted: isDeleted,
            terminatedAt: status == EmployeeStatus.Terminated || isDeleted ? fixture.UtcNow : null);
        await fixture.Environment.AddVacationRequestAsync(
            requester.Id, VacationRequestStatus.Pending, fixture.UtcNow, workingDayCount: 2);

        var result = await fixture.Service.GetVacationRequestsAsync(
            requester.Id, null, null, 1, 25, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Theory]
    [InlineData(EmployeeStatus.Active, true)]
    [InlineData(EmployeeStatus.Terminated, false)]
    public async Task GetVacationRequestByIdAsync_DeletedOrTerminatedRequesterSelfRequest_ReturnsForbidden(
        EmployeeStatus status,
        bool isDeleted)
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var requester = await fixture.Environment.AddEmployeeAsync(
            $"REQ-D-{Guid.NewGuid():N}"[..10],
            $"requester-detail-{Guid.NewGuid():N}@test.com",
            fixture.Environment.DefaultDepartment!.Id,
            status: status,
            isDeleted: isDeleted,
            terminatedAt: status == EmployeeStatus.Terminated || isDeleted ? fixture.UtcNow : null);
        var request = await fixture.Environment.AddVacationRequestAsync(
            requester.Id, VacationRequestStatus.Pending, fixture.UtcNow, workingDayCount: 2);

        var result = await fixture.Service.GetVacationRequestByIdAsync(
            requester.Id, request.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Theory]
    [InlineData(EmployeeStatus.Active, true)]
    [InlineData(EmployeeStatus.Terminated, false)]
    public async Task CreateVacationRequestAsync_DeletedOrTerminatedRequesterSelfCreate_ReturnsForbidden(
        EmployeeStatus status,
        bool isDeleted)
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var requester = await fixture.Environment.AddEmployeeAsync(
            $"REQ-C-{Guid.NewGuid():N}"[..10],
            $"requester-create-{Guid.NewGuid():N}@test.com",
            fixture.Environment.DefaultDepartment!.Id,
            status: status,
            isDeleted: isDeleted,
            terminatedAt: status == EmployeeStatus.Terminated || isDeleted ? fixture.UtcNow : null);

        var result = await fixture.Service.CreateVacationRequestAsync(
            requester.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = requester.Id,
                StartDate = new DateOnly(2026, 6, 22),
                EndDate = new DateOnly(2026, 6, 24),
                Reason = "Blocked requester"
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Theory]
    [InlineData(EmployeeStatus.Active, true)]
    [InlineData(EmployeeStatus.Terminated, false)]
    public async Task DeleteVacationRequestAsync_DeletedOrTerminatedRequesterSelfRequest_ReturnsForbidden(
        EmployeeStatus status,
        bool isDeleted)
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var requester = await fixture.Environment.AddEmployeeAsync(
            $"REQ-X-{Guid.NewGuid():N}"[..10],
            $"requester-delete-{Guid.NewGuid():N}@test.com",
            fixture.Environment.DefaultDepartment!.Id,
            status: status,
            isDeleted: isDeleted,
            terminatedAt: status == EmployeeStatus.Terminated || isDeleted ? fixture.UtcNow : null);
        var request = await fixture.Environment.AddVacationRequestAsync(
            requester.Id, VacationRequestStatus.Pending, fixture.UtcNow, workingDayCount: 2);

        var result = await fixture.Service.DeleteVacationRequestAsync(
            requester.Id, request.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task GetVacationRequestsAsync_EmployeeList_ReturnsOwnRequestsOnly()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var employee = fixture.Employee!;

        var result = await fixture.Service.GetVacationRequestsAsync(
            employee.Id, null, null, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.All(result.Value.Items, item => Assert.Equal(employee.Id, item.EmployeeId));
    }

    [Fact]
    public async Task GetVacationRequestsAsync_EmployeeListWithOtherEmployeeId_ReturnsEmptyPage()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var employee = fixture.Employee!;
        var otherId = fixture.OtherEmployee!.Id;

        var result = await fixture.Service.GetVacationRequestsAsync(
            employee.Id, null, otherId, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetVacationRequestByIdAsync_EmployeeOwnRequest_ReturnsDetail()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var employee = fixture.Employee!;

        var result = await fixture.Service.GetVacationRequestByIdAsync(
            employee.Id, fixture.EmployeeOwnRequest!.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(fixture.EmployeeOwnRequest.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetVacationRequestByIdAsync_EmployeeOtherRequest_ReturnsForbidden()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var employee = fixture.Employee!;

        var result = await fixture.Service.GetVacationRequestByIdAsync(
            employee.Id, fixture.OtherEmployeeRequest!.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task CreateVacationRequestAsync_EmployeeSelfCreate_Allowed()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var employee = fixture.Employee!;

        var result = await fixture.Service.CreateVacationRequestAsync(
            employee.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = employee.Id,
                StartDate = new DateOnly(2026, 6, 22),
                EndDate = new DateOnly(2026, 6, 24),
                Reason = "Annual leave"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(employee.Id, result.Value!.CreatedByEmployeeId);
        Assert.Equal(employee.FullName, result.Value.CreatedByEmployeeName);
    }

    [Fact]
    public async Task GetVacationRequestByIdAsync_ExistingNullCreator_ReturnsDetailWithNullCreator()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var employee = fixture.Employee!;

        var result = await fixture.Service.GetVacationRequestByIdAsync(
            employee.Id, fixture.EmployeeOwnRequest!.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.CreatedByEmployeeId);
        Assert.Null(result.Value.CreatedByEmployeeName);
    }

    [Fact]
    public async Task CreateVacationRequestAsync_EmployeeNonSelfCreate_ReturnsForbidden()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var employee = fixture.Employee!;

        var result = await fixture.Service.CreateVacationRequestAsync(
            employee.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = fixture.OtherEmployee!.Id,
                StartDate = new DateOnly(2026, 6, 22),
                EndDate = new DateOnly(2026, 6, 24),
                Reason = "Other"
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateVacationStatusAsync_EmployeeReview_ReturnsForbidden()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var employee = fixture.Employee!;

        var result = await fixture.Service.UpdateVacationStatusAsync(
            fixture.EmployeeOwnRequest!.Id,
            employee.Id,
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Approved },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task DeleteVacationRequestAsync_EmployeeOwnPending_Allowed()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var employee = fixture.Employee!;

        var result = await fixture.Service.DeleteVacationRequestAsync(
            employee.Id, fixture.EmployeeOwnRequest!.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteVacationRequestAsync_EmployeeOtherRequest_ReturnsForbidden()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var employee = fixture.Employee!;

        var result = await fixture.Service.DeleteVacationRequestAsync(
            employee.Id, fixture.OtherEmployeeRequest!.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }
}
