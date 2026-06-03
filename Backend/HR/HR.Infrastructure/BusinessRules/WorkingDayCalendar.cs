namespace HR.Infrastructure.BusinessRules;

public class WorkingDayCalendar
{
    public bool IsWorkingDay(DateOnly date)
    {
        return date.DayOfWeek is not DayOfWeek.Friday and not DayOfWeek.Saturday;
    }

    public int CountInclusiveWorkingDays(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            return 0;
        }

        var count = 0;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (IsWorkingDay(date))
            {
                count++;
            }
        }

        return count;
    }

    public int CountFullWorkingDaysBetween(DateOnly startBoundary, DateOnly endBoundary)
    {
        var firstCandidate = startBoundary.AddDays(1);
        var lastCandidate = endBoundary.AddDays(-1);
        return CountInclusiveWorkingDays(firstCandidate, lastCandidate);
    }
}
