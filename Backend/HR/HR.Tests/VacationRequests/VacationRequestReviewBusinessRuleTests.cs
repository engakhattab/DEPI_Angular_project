using HR.Application.DTOs.VacationRequests;
using HR.Application.VacationRequests;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace HR.Tests.VacationRequests;

public class VacationRequestReviewBusinessRuleTests
{
    [Fact]
    public async Task UpdateVacationStatusAsync_ApprovesPendingRequestAndDeductsBalanceOnce()
    {
        await using var fixture = await ReviewFixture.CreateAsync();
        var employee = await fixture.AddEmployeeAsync("EMP-701", "employee701@example.com", vacationBalanceDays: 10);
        var reviewer = await fixture.AddEmployeeAsync("EMP-702", "reviewer702@example.com");
        var request = await fixture.AddVacationRequestAsync(employee.Id, VacationRequestStatus.Pending, workingDayCount: 3);

        var result = await fixture.Service.UpdateVacationStatusAsync(
            request.Id,
            reviewer.Id,
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Approved },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(VacationRequestStatus.Approved, result.Value!.Status);
        Assert.Equal(reviewer.Id, result.Value.ReviewedByEmployeeId);
        Assert.Equal("EMP-702", result.Value.ReviewedByEmployeeName);
        Assert.Equal(7, (await fixture.ReloadEmployeeAsync(employee.Id)).VacationBalanceDays);
    }

    [Fact]
    public async Task UpdateVacationStatusAsync_RejectedApprovedRequestRestoresBalanceOnce()
    {
        await using var fixture = await ReviewFixture.CreateAsync();
        var employee = await fixture.AddEmployeeAsync("EMP-703", "employee703@example.com", vacationBalanceDays: 6);
        var reviewer = await fixture.AddEmployeeAsync("EMP-704", "reviewer704@example.com");
        var request = await fixture.AddVacationRequestAsync(employee.Id, VacationRequestStatus.Approved, workingDayCount: 4);

        var result = await fixture.Service.UpdateVacationStatusAsync(
            request.Id,
            reviewer.Id,
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Rejected },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, (await fixture.ReloadEmployeeAsync(employee.Id)).VacationBalanceDays);
    }

    [Fact]
    public async Task UpdateVacationStatusAsync_RejectsRejectedTerminalTransition()
    {
        await using var fixture = await ReviewFixture.CreateAsync();
        var employee = await fixture.AddEmployeeAsync("EMP-705", "employee705@example.com");
        var reviewer = await fixture.AddEmployeeAsync("EMP-706", "reviewer706@example.com");
        var request = await fixture.AddVacationRequestAsync(employee.Id, VacationRequestStatus.Rejected);

        var result = await fixture.Service.UpdateVacationStatusAsync(
            request.Id,
            reviewer.Id,
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Approved },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateVacationStatusAsync_RejectsSelfReview()
    {
        await using var fixture = await ReviewFixture.CreateAsync();
        var employee = await fixture.AddEmployeeAsync("EMP-707", "employee707@example.com");
        var request = await fixture.AddVacationRequestAsync(employee.Id, VacationRequestStatus.Pending);

        var result = await fixture.Service.UpdateVacationStatusAsync(
            request.Id,
            employee.Id,
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Approved },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateVacationStatusAsync_AllowsAnyAuthenticatedNonRequester()
    {
        await using var fixture = await ReviewFixture.CreateAsync();
        var employee = await fixture.AddEmployeeAsync("EMP-708", "employee708@example.com");
        var reviewer = await fixture.AddEmployeeAsync("EMP-709", "reviewer709@example.com", status: EmployeeStatus.Suspended);
        var request = await fixture.AddVacationRequestAsync(employee.Id, VacationRequestStatus.Pending);

        var result = await fixture.Service.UpdateVacationStatusAsync(
            request.Id,
            reviewer.Id,
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Rejected },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateVacationStatusAsync_SameStatusIsNoOp()
    {
        await using var fixture = await ReviewFixture.CreateAsync();
        var employee = await fixture.AddEmployeeAsync("EMP-710", "employee710@example.com");
        var reviewer = await fixture.AddEmployeeAsync("EMP-711", "reviewer711@example.com");
        var request = await fixture.AddVacationRequestAsync(employee.Id, VacationRequestStatus.Approved, workingDayCount: 2);
        request.ReviewedByEmployeeId = reviewer.Id;
        request.ReviewedAt = fixture.UtcNow.AddHours(-2);
        request.UpdatedAt = fixture.UtcNow.AddHours(-2);
        request.ReviewedBy = reviewer;
        await fixture.Environment.Context.SaveChangesAsync();

        var result = await fixture.Service.UpdateVacationStatusAsync(
            request.Id,
            reviewer.Id,
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Approved },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(reviewer.Id, result.Value!.ReviewedByEmployeeId);
        Assert.Equal(fixture.UtcNow.AddHours(-2), result.Value.ReviewedAt);
        Assert.Equal(fixture.UtcNow.AddHours(-2), result.Value.UpdatedAt);
    }

    [Fact]
    public async Task DeleteVacationRequestAsync_AllowsPendingDeletionOnly()
    {
        await using var fixture = await ReviewFixture.CreateAsync();
        var employee = await fixture.AddEmployeeAsync("EMP-712", "employee712@example.com");
        var pending = await fixture.AddVacationRequestAsync(employee.Id, VacationRequestStatus.Pending);
        var approved = await fixture.AddVacationRequestAsync(employee.Id, VacationRequestStatus.Approved);

        var pendingResult = await fixture.Service.DeleteVacationRequestAsync(pending.Id, CancellationToken.None);
        var approvedResult = await fixture.Service.DeleteVacationRequestAsync(approved.Id, CancellationToken.None);

        Assert.True(pendingResult.IsSuccess);
        Assert.False(approvedResult.IsSuccess);
        Assert.False(await fixture.Environment.Context.VacationRequests.AnyAsync(v => v.Id == pending.Id));
        Assert.True(await fixture.Environment.Context.VacationRequests.AnyAsync(v => v.Id == approved.Id));
    }

    private sealed class ReviewFixture : IAsyncDisposable
    {
        private readonly SqliteTestEnvironment _environment;

        private ReviewFixture(SqliteTestEnvironment environment, DateTimeOffset utcNow)
        {
            _environment = environment;
            UtcNow = utcNow;
            Service = environment.GetRequiredService<IVacationRequestService>();
        }

        public SqliteTestEnvironment Environment => _environment;

        public IVacationRequestService Service { get; }

        public DateTimeOffset UtcNow { get; }

        public static async Task<ReviewFixture> CreateAsync()
        {
            var utcNow = new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);
            var environment = await SqliteTestEnvironment.CreateAsync(
                seedDefaultDepartment: true,
                timeProvider: new TestTimeProvider(utcNow));
            return new ReviewFixture(environment, utcNow);
        }

        public Task<Employee> AddEmployeeAsync(
            string employeeNumber,
            string email,
            EmployeeStatus status = EmployeeStatus.Active,
            int vacationBalanceDays = 21)
        {
            return _environment.AddEmployeeAsync(
                employeeNumber,
                email,
                _environment.DefaultDepartment!.Id,
                status: status,
                vacationBalanceDays: vacationBalanceDays);
        }

        public Task<VacationRequest> AddVacationRequestAsync(
            Guid employeeId,
            VacationRequestStatus status,
            int workingDayCount = 2)
        {
            return _environment.AddVacationRequestAsync(
                employeeId,
                status,
                UtcNow,
                workingDayCount: workingDayCount);
        }

        public async Task<Employee> ReloadEmployeeAsync(Guid employeeId)
        {
            var employee = await _environment.Context.Employees.SingleAsync(e => e.Id == employeeId);
            return employee;
        }

        public ValueTask DisposeAsync()
        {
            return _environment.DisposeAsync();
        }
    }
}
