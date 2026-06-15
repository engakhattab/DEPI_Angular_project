using HR.Application.DTOs.VacationRequests;
using HR.Application.VacationRequests;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.VacationRequests;

public class VacationRequestServiceBusinessRuleTests
{
    [Fact]
    public async Task CreateVacationRequestAsync_AcceptsValidFutureRequestAndStoresWorkingDayCount()
    {
        await using var fixture = await VacationRequestFixture.CreateAsync(new DateTimeOffset(2026, 6, 3, 8, 0, 0, TimeSpan.Zero));
        var employee = await fixture.AddEmployeeAsync("EMP-601", "valid@example.com");

        var result = await fixture.Service.CreateVacationRequestAsync(
            employee.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = employee.Id,
                StartDate = new DateOnly(2026, 6, 9),
                EndDate = new DateOnly(2026, 6, 11),
                Reason = "Annual leave"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(VacationRequestStatus.Pending, result.Value!.Status);
        Assert.Equal(3, result.Value.WorkingDayCount);
    }

    [Fact]
    public async Task CreateVacationRequestAsync_RejectsOverlapWithPendingOrApprovedRequests()
    {
        await using var fixture = await VacationRequestFixture.CreateAsync(new DateTimeOffset(2026, 6, 3, 8, 0, 0, TimeSpan.Zero));
        var employee = await fixture.AddEmployeeAsync("EMP-602", "overlap@example.com");
        await fixture.Environment.AddVacationRequestAsync(
            employee.Id,
            VacationRequestStatus.Approved,
            fixture.UtcNow,
            startDate: new DateOnly(2026, 6, 9),
            endDate: new DateOnly(2026, 6, 11),
            workingDayCount: 3);

        var result = await fixture.Service.CreateVacationRequestAsync(
            employee.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = employee.Id,
                StartDate = new DateOnly(2026, 6, 11),
                EndDate = new DateOnly(2026, 6, 12),
                Reason = "Overlap"
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task CreateVacationRequestAsync_AllowsOverlapWithRejectedRequestOnly()
    {
        await using var fixture = await VacationRequestFixture.CreateAsync(new DateTimeOffset(2026, 6, 3, 8, 0, 0, TimeSpan.Zero));
        var employee = await fixture.AddEmployeeAsync("EMP-603", "rejected-overlap@example.com");
        await fixture.Environment.AddVacationRequestAsync(
            employee.Id,
            VacationRequestStatus.Rejected,
            fixture.UtcNow,
            startDate: new DateOnly(2026, 6, 9),
            endDate: new DateOnly(2026, 6, 11),
            workingDayCount: 3);

        var result = await fixture.Service.CreateVacationRequestAsync(
            employee.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = employee.Id,
                StartDate = new DateOnly(2026, 6, 10),
                EndDate = new DateOnly(2026, 6, 11),
                Reason = "Rejected overlap"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(EmployeeStatus.Suspended, false, 21, "suspended@example.com", "BUSINESS_RULE_VIOLATION")]
    [InlineData(EmployeeStatus.Terminated, false, 21, "terminated@example.com", "FORBIDDEN")]
    [InlineData(EmployeeStatus.Active, true, 21, "deleted@example.com", "FORBIDDEN")]
    [InlineData(EmployeeStatus.Active, false, 1, "balance@example.com", "BUSINESS_RULE_VIOLATION")]
    public async Task CreateVacationRequestAsync_RejectsEmployeeStateAndBalanceFailures(
        EmployeeStatus status,
        bool isDeleted,
        int vacationBalanceDays,
        string email,
        string expectedCode)
    {
        await using var fixture = await VacationRequestFixture.CreateAsync(new DateTimeOffset(2026, 6, 3, 8, 0, 0, TimeSpan.Zero));
        var employee = await fixture.AddEmployeeAsync(
            $"EMP-{Random.Shared.Next(700, 799)}",
            email,
            status: status,
            vacationBalanceDays: vacationBalanceDays,
            isDeleted: isDeleted,
            terminatedAt: status == EmployeeStatus.Terminated || isDeleted ? fixture.UtcNow : null);

        var result = await fixture.Service.CreateVacationRequestAsync(
            employee.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = employee.Id,
                StartDate = new DateOnly(2026, 6, 8),
                EndDate = new DateOnly(2026, 6, 10),
                Reason = "Blocked"
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error!.Code);
    }

    [Theory]
    [InlineData("2026-06-02", "2026-06-04")]
    [InlineData("2026-06-05", "2026-06-04")]
    public async Task CreateVacationRequestAsync_RejectsPastStartAndInvalidDateRange(string startDateText, string endDateText)
    {
        await using var fixture = await VacationRequestFixture.CreateAsync(new DateTimeOffset(2026, 6, 3, 8, 0, 0, TimeSpan.Zero));
        var employee = await fixture.AddEmployeeAsync("EMP-604", "dates@example.com");

        var result = await fixture.Service.CreateVacationRequestAsync(
            employee.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = employee.Id,
                StartDate = DateOnly.Parse(startDateText),
                EndDate = DateOnly.Parse(endDateText),
                Reason = "Dates"
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task CreateVacationRequestAsync_RejectsRequestsInsideNoticeWindow()
    {
        await using var fixture = await VacationRequestFixture.CreateAsync(new DateTimeOffset(2026, 6, 4, 8, 0, 0, TimeSpan.Zero));
        var employee = await fixture.AddEmployeeAsync("EMP-605", "notice@example.com");

        var result = await fixture.Service.CreateVacationRequestAsync(
            employee.Id,
            new VacationRequestCreateRequest
            {
                EmployeeId = employee.Id,
                StartDate = new DateOnly(2026, 6, 8),
                EndDate = new DateOnly(2026, 6, 9),
                Reason = "Notice"
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    private sealed class VacationRequestFixture : IAsyncDisposable
    {
        private readonly SqliteTestEnvironment _environment;

        private VacationRequestFixture(SqliteTestEnvironment environment, DateTimeOffset utcNow)
        {
            _environment = environment;
            UtcNow = utcNow;
            Service = environment.GetRequiredService<IVacationRequestService>();
        }

        public SqliteTestEnvironment Environment => _environment;

        public IVacationRequestService Service { get; }

        public DateTimeOffset UtcNow { get; }

        public static async Task<VacationRequestFixture> CreateAsync(DateTimeOffset utcNow)
        {
            var environment = await SqliteTestEnvironment.CreateAsync(
                seedDefaultDepartment: true,
                timeProvider: new TestTimeProvider(utcNow));
            return new VacationRequestFixture(environment, utcNow);
        }

        public Task<HR.Domain.Entities.Employee> AddEmployeeAsync(
            string employeeNumber,
            string email,
            EmployeeStatus status = EmployeeStatus.Active,
            int vacationBalanceDays = 21,
            bool isDeleted = false,
            DateTimeOffset? terminatedAt = null)
        {
            return _environment.AddEmployeeAsync(
                employeeNumber,
                email,
                _environment.DefaultDepartment!.Id,
                status: status,
                vacationBalanceDays: vacationBalanceDays,
                isDeleted: isDeleted,
                terminatedAt: terminatedAt);
        }

        public ValueTask DisposeAsync()
        {
            return _environment.DisposeAsync();
        }
    }
}
