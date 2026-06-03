using HR.Infrastructure.BusinessRules;

namespace HR.Tests.BusinessRules;

public class WorkingDayCalendarTests
{
    private readonly WorkingDayCalendar _calendar = new();

    [Theory]
    [InlineData("2026-06-04", true)]
    [InlineData("2026-06-05", false)]
    [InlineData("2026-06-06", false)]
    [InlineData("2026-06-07", true)]
    public void IsWorkingDay_UsesSundayThroughThursdayCalendar(string dateText, bool expected)
    {
        var date = DateOnly.Parse(dateText);

        var result = _calendar.IsWorkingDay(date);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CountInclusiveWorkingDays_CountsOnlySundayThroughThursday()
    {
        var result = _calendar.CountInclusiveWorkingDays(
            new DateOnly(2026, 6, 4),
            new DateOnly(2026, 6, 10));

        Assert.Equal(5, result);
    }

    [Fact]
    public void CountFullWorkingDaysBetween_ExcludesBoundaryDays()
    {
        var result = _calendar.CountFullWorkingDaysBetween(
            new DateOnly(2026, 6, 3),
            new DateOnly(2026, 6, 8));

        Assert.Equal(2, result);
    }

    [Fact]
    public void CountFullWorkingDaysBetween_IgnoresWeekendBoundaryGap()
    {
        var result = _calendar.CountFullWorkingDaysBetween(
            new DateOnly(2026, 6, 4),
            new DateOnly(2026, 6, 7));

        Assert.Equal(0, result);
    }
}
