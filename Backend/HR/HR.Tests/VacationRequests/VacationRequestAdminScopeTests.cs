using HR.Application.DTOs.VacationRequests;
using HR.Domain.Enums;

namespace HR.Tests.VacationRequests;

public class VacationRequestAdminScopeTests
{
    [Fact]
    public async Task GetVacationRequestsAsync_HRAdmin_ReturnsOrganizationWide()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var hrAdmin = fixture.HrAdmin!;

        var result = await fixture.Service.GetVacationRequestsAsync(
            hrAdmin.Id, null, null, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Items.Count >= 4);
    }

    [Fact]
    public async Task GetVacationRequestsAsync_SysAdmin_ReturnsOrganizationWide()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var sysAdmin = fixture.SysAdmin!;

        var result = await fixture.Service.GetVacationRequestsAsync(
            sysAdmin.Id, null, null, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Items.Count >= 4);
    }

    [Fact]
    public async Task GetVacationRequestByIdAsync_HRAdmin_AnyRequest_ReturnsDetail()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var hrAdmin = fixture.HrAdmin!;

        var result = await fixture.Service.GetVacationRequestByIdAsync(
            hrAdmin.Id, fixture.EmployeeOwnRequest!.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.EmployeeOwnRequest.Id, result.Value!.Id);
    }

    [Fact]
    public async Task GetVacationRequestByIdAsync_SysAdmin_AnyRequest_ReturnsDetail()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var sysAdmin = fixture.SysAdmin!;

        var result = await fixture.Service.GetVacationRequestByIdAsync(
            sysAdmin.Id, fixture.OtherEmployeeRequest!.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.OtherEmployeeRequest.Id, result.Value!.Id);
    }

    [Fact]
    public async Task CreateVacationRequestAsync_HRAdmin_OnBehalfOfEmployee_Allowed()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var hrAdmin = fixture.HrAdmin!;

        var result = await fixture.Service.CreateVacationRequestAsync(
            hrAdmin.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = fixture.Employee!.Id,
                StartDate = new DateOnly(2026, 6, 22),
                EndDate = new DateOnly(2026, 6, 24),
                Reason = "HR created"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(hrAdmin.Id, result.Value!.CreatedByEmployeeId);
        Assert.Equal(hrAdmin.FullName, result.Value.CreatedByEmployeeName);
    }

    [Fact]
    public async Task CreateVacationRequestAsync_SysAdmin_OnBehalfOfEmployee_Allowed()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var sysAdmin = fixture.SysAdmin!;

        var result = await fixture.Service.CreateVacationRequestAsync(
            sysAdmin.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = fixture.Employee!.Id,
                StartDate = new DateOnly(2026, 6, 22),
                EndDate = new DateOnly(2026, 6, 24),
                Reason = "SYS created"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(sysAdmin.Id, result.Value!.CreatedByEmployeeId);
        Assert.Equal(sysAdmin.FullName, result.Value.CreatedByEmployeeName);
    }

    [Theory]
    [InlineData(EmployeeStatus.Suspended, false)]
    [InlineData(EmployeeStatus.Terminated, false)]
    [InlineData(EmployeeStatus.Active, true)]
    public async Task CreateVacationRequestAsync_HRAdmin_OnBehalfOfIneligibleTarget_ReturnsBusinessRuleViolation(
        EmployeeStatus targetStatus,
        bool isDeleted)
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var hrAdmin = fixture.HrAdmin!;
        var target = await fixture.Environment.AddEmployeeAsync(
            $"TGT-H-{Guid.NewGuid():N}"[..12],
            $"target-hr-{Guid.NewGuid():N}@test.com",
            fixture.Environment.DefaultDepartment!.Id,
            status: targetStatus,
            isDeleted: isDeleted,
            terminatedAt: targetStatus == EmployeeStatus.Terminated || isDeleted ? fixture.UtcNow : null);

        var result = await fixture.Service.CreateVacationRequestAsync(
            hrAdmin.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = target.Id,
                StartDate = new DateOnly(2026, 6, 22),
                EndDate = new DateOnly(2026, 6, 24),
                Reason = "Ineligible target"
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Theory]
    [InlineData(EmployeeStatus.Suspended, false)]
    [InlineData(EmployeeStatus.Terminated, false)]
    [InlineData(EmployeeStatus.Active, true)]
    public async Task CreateVacationRequestAsync_SysAdmin_OnBehalfOfIneligibleTarget_ReturnsBusinessRuleViolation(
        EmployeeStatus targetStatus,
        bool isDeleted)
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var sysAdmin = fixture.SysAdmin!;
        var target = await fixture.Environment.AddEmployeeAsync(
            $"TGT-S-{Guid.NewGuid():N}"[..12],
            $"target-sys-{Guid.NewGuid():N}@test.com",
            fixture.Environment.DefaultDepartment!.Id,
            status: targetStatus,
            isDeleted: isDeleted,
            terminatedAt: targetStatus == EmployeeStatus.Terminated || isDeleted ? fixture.UtcNow : null);

        var result = await fixture.Service.CreateVacationRequestAsync(
            sysAdmin.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = target.Id,
                StartDate = new DateOnly(2026, 6, 22),
                EndDate = new DateOnly(2026, 6, 24),
                Reason = "Ineligible target"
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateVacationStatusAsync_HRAdmin_NonSelfReview_Allowed()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var hrAdmin = fixture.HrAdmin!;

        var result = await fixture.Service.UpdateVacationStatusAsync(
            fixture.EmployeeOwnRequest!.Id,
            hrAdmin.Id,
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Approved },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateVacationStatusAsync_HRAdmin_SelfReview_ReturnsBusinessRuleViolation()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var hrAdmin = fixture.HrAdmin!;
        var hrAdminRequest = await fixture.Environment.AddVacationRequestAsync(
            hrAdmin.Id, VacationRequestStatus.Pending, fixture.UtcNow, workingDayCount: 2);

        var result = await fixture.Service.UpdateVacationStatusAsync(
            hrAdminRequest.Id,
            hrAdmin.Id,
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Approved },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateVacationStatusAsync_SysAdmin_NonSelfReview_Allowed()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var sysAdmin = fixture.SysAdmin!;

        var result = await fixture.Service.UpdateVacationStatusAsync(
            fixture.EmployeeOwnRequest!.Id,
            sysAdmin.Id,
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Approved },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
