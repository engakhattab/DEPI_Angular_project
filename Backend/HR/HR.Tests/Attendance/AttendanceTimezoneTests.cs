using HR.Application.Attendance;
using HR.Application.DTOs.Attendance;
using HR.Infrastructure.Configuration;
using HR.Tests.TestInfrastructure;
using Microsoft.Extensions.Options;

namespace HR.Tests.Attendance;

public class AttendanceTimezoneTests
{
    [Theory]
    [InlineData("2026-06-06T20:30:00+00:00", 2026, 6, 6)]
    [InlineData("2026-06-06T22:30:00+00:00", 2026, 6, 7)]
    public void BusinessTimeProvider_WithAfricaCairo_DerivesBusinessDateAtUtcBoundary(string utcValue, int year, int month, int day)
    {
        var provider = new BusinessTimeProvider(
            Options.Create(new BusinessSettings { TimeZoneId = "Africa/Cairo" }),
            new TestTimeProvider(DateTimeOffset.Parse(utcValue)));

        var businessDate = provider.GetBusinessDate(DateTimeOffset.Parse(utcValue));

        Assert.Equal(new DateOnly(year, month, day), businessDate);
    }

    [Fact]
    public async Task ClockInAsync_StoresUtcTimestampAndDerivesAttendanceDateFromConfiguredTimezone()
    {
        var utcNow = new DateTimeOffset(2026, 6, 6, 22, 30, 0, TimeSpan.Zero);
        await using var environment = await SqliteTestEnvironment.CreateAsync(
            seedDefaultDepartment: true,
            timeProvider: new TestTimeProvider(utcNow));
        var employee = await environment.AddEmployeeAsync("ATT-TZ-001", "attendance-timezone@example.com", environment.DefaultDepartment!.Id);
        var service = environment.GetRequiredService<IAttendanceService>();

        var result = await service.ClockInAsync(employee.Id, new AttendanceClockInRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(utcNow, result.Value!.ClockInAtUtc);
        Assert.Equal(new DateOnly(2026, 6, 7), result.Value.AttendanceDate);
    }

    [Fact]
    public void BusinessTimeProvider_ReturnsPinnedUtcNowWithoutUsingUnnamedServerLocalTime()
    {
        var utcNow = new DateTimeOffset(2026, 6, 6, 22, 30, 0, TimeSpan.Zero);
        var provider = new BusinessTimeProvider(
            Options.Create(new BusinessSettings { TimeZoneId = "Africa/Cairo" }),
            new TestTimeProvider(utcNow));

        Assert.Equal(utcNow, provider.GetUtcNow());
    }
}
