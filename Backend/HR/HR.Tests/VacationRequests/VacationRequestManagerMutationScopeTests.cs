using HR.Application.DTOs.VacationRequests;
using HR.Domain.Enums;

namespace HR.Tests.VacationRequests;

public class VacationRequestManagerMutationScopeTests
{
    [Fact]
    public async Task CreateVacationRequestAsync_ManagerSelfCreate_Allowed()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.CreateVacationRequestAsync(
            manager.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = manager.Id,
                StartDate = new DateOnly(2026, 6, 22),
                EndDate = new DateOnly(2026, 6, 24),
                Reason = "Manager leave"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateVacationRequestAsync_ManagerTeamMemberCreate_ReturnsForbidden()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.CreateVacationRequestAsync(
            manager.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = fixture.DirectReport!.Id,
                StartDate = new DateOnly(2026, 6, 22),
                EndDate = new DateOnly(2026, 6, 24),
                Reason = "For team"
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task CreateVacationRequestAsync_ManagerOtherEmployeeCreate_ReturnsForbidden()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.CreateVacationRequestAsync(
            manager.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = fixture.OtherEmployee!.Id,
                StartDate = new DateOnly(2026, 6, 22),
                EndDate = new DateOnly(2026, 6, 24),
                Reason = "For other"
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Theory]
    [InlineData(EmployeeStatus.Suspended, false)]
    [InlineData(EmployeeStatus.Terminated, false)]
    [InlineData(EmployeeStatus.Active, true)]
    public async Task CreateVacationRequestAsync_ManagerIneligibleTargetCreate_ReturnsForbidden(
        EmployeeStatus targetStatus,
        bool isDeleted)
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;
        var target = await fixture.Environment.AddEmployeeAsync(
            $"TGT-M-{Guid.NewGuid():N}"[..12],
            $"target-manager-{Guid.NewGuid():N}@test.com",
            fixture.Environment.DefaultDepartment!.Id,
            managerId: manager.Id,
            status: targetStatus,
            isDeleted: isDeleted,
            terminatedAt: targetStatus == EmployeeStatus.Terminated || isDeleted ? fixture.UtcNow : null);

        var result = await fixture.Service.CreateVacationRequestAsync(
            manager.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = target.Id,
                StartDate = new DateOnly(2026, 6, 22),
                EndDate = new DateOnly(2026, 6, 24),
                Reason = "Manager cannot create for target"
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateVacationStatusAsync_ManagerReviewTeamRequest_Allowed()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.UpdateVacationStatusAsync(
            fixture.DirectReportRequest!.Id,
            manager.Id,
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Approved },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(VacationRequestStatus.Approved, result.Value!.Status);
    }

    [Fact]
    public async Task UpdateVacationStatusAsync_ManagerSelfReview_ReturnsBusinessRuleViolation()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.UpdateVacationStatusAsync(
            fixture.ManagerOwnRequest!.Id,
            manager.Id,
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Approved },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateVacationStatusAsync_ManagerReviewOutsideTeam_ReturnsForbidden()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.UpdateVacationStatusAsync(
            fixture.OtherEmployeeRequest!.Id,
            manager.Id,
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Approved },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Theory]
    [InlineData(EmployeeStatus.Active, true)]
    [InlineData(EmployeeStatus.Terminated, false)]
    public async Task UpdateVacationStatusAsync_DeletedOrTerminatedManagerRequester_ReturnsForbidden(
        EmployeeStatus requesterStatus,
        bool isDeleted)
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var requester = await fixture.Environment.AddEmployeeAsync(
            $"MGR-R-{Guid.NewGuid():N}"[..12],
            $"requester-review-{Guid.NewGuid():N}@test.com",
            fixture.Environment.DefaultDepartment!.Id,
            status: requesterStatus,
            isDeleted: isDeleted,
            terminatedAt: requesterStatus == EmployeeStatus.Terminated || isDeleted ? fixture.UtcNow : null,
            role: EmployeeRole.Manager);
        var report = await fixture.Environment.AddEmployeeAsync(
            $"RPT-R-{Guid.NewGuid():N}"[..12],
            $"review-report-{Guid.NewGuid():N}@test.com",
            fixture.Environment.DefaultDepartment!.Id,
            managerId: requester.Id);
        var request = await fixture.Environment.AddVacationRequestAsync(
            report.Id, VacationRequestStatus.Pending, fixture.UtcNow, workingDayCount: 2);

        var result = await fixture.Service.UpdateVacationStatusAsync(
            request.Id,
            requester.Id,
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Approved },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task DeleteVacationRequestAsync_ManagerOwnPending_Allowed()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.DeleteVacationRequestAsync(
            manager.Id, fixture.ManagerOwnRequest!.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteVacationRequestAsync_ManagerTeamPending_ReturnsForbidden()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.DeleteVacationRequestAsync(
            manager.Id, fixture.DirectReportRequest!.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }
}
