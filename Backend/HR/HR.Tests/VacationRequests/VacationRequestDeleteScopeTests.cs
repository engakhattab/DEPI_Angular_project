using HR.Domain.Enums;

namespace HR.Tests.VacationRequests;

public class VacationRequestDeleteScopeTests
{
    [Fact]
    public async Task DeleteVacationRequestAsync_MissingTarget_ReturnsNotFound()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var employee = fixture.Employee!;

        var result = await fixture.Service.DeleteVacationRequestAsync(
            employee.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task DeleteVacationRequestAsync_EmployeeOwnNonPending_ReturnsBusinessRuleViolation()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var employee = fixture.Employee!;
        var approved = await fixture.Environment.AddVacationRequestAsync(
            employee.Id, VacationRequestStatus.Approved, fixture.UtcNow, workingDayCount: 2);

        var result = await fixture.Service.DeleteVacationRequestAsync(
            employee.Id, approved.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task DeleteVacationRequestAsync_HRAdmin_DeleteAnyPending_Allowed()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var hrAdmin = fixture.HrAdmin!;

        var result = await fixture.Service.DeleteVacationRequestAsync(
            hrAdmin.Id, fixture.DirectReportRequest!.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteVacationRequestAsync_SysAdmin_DeleteAnyPending_Allowed()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var sysAdmin = fixture.SysAdmin!;

        var result = await fixture.Service.DeleteVacationRequestAsync(
            sysAdmin.Id, fixture.DirectReportRequest!.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteVacationRequestAsync_Employee_OutOfScope_ReturnsForbiddenBeforePendingCheck()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var employee = fixture.Employee!;
        var otherPending = await fixture.Environment.AddVacationRequestAsync(
            fixture.OtherEmployee!.Id, VacationRequestStatus.Pending, fixture.UtcNow, workingDayCount: 2);

        var result = await fixture.Service.DeleteVacationRequestAsync(
            employee.Id, otherPending.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task DeleteVacationRequestAsync_Manager_TeamPending_ReturnsForbidden()
    {
        await using var fixture = await VacationRequestScopeFixture.CreateAsync();
        var manager = fixture.Manager!;

        var result = await fixture.Service.DeleteVacationRequestAsync(
            manager.Id, fixture.DirectReportRequest!.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }
}
