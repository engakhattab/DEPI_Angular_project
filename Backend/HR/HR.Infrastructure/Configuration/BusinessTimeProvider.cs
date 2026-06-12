using Microsoft.Extensions.Options;

namespace HR.Infrastructure.Configuration;

public class BusinessTimeProvider(IOptions<BusinessSettings> settings, TimeProvider timeProvider) : IBusinessTimeProvider
{
    private readonly TimeZoneInfo _timeZone = Resolve(settings.Value.TimeZoneId);
    private readonly TimeProvider _timeProvider = timeProvider;

    public DateTimeOffset GetUtcNow()
    {
        return _timeProvider.GetUtcNow();
    }

    public DateOnly GetBusinessDate(DateTimeOffset utcTimestamp)
    {
        var utc = utcTimestamp.ToUniversalTime();
        var local = TimeZoneInfo.ConvertTime(utc, _timeZone);
        return DateOnly.FromDateTime(local.DateTime);
    }

    private static TimeZoneInfo Resolve(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new InvalidOperationException("BusinessSettings:TimeZoneId is required.");
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new InvalidOperationException($"BusinessSettings:TimeZoneId '{timeZoneId}' is invalid.", ex);
        }
        catch (InvalidTimeZoneException ex)
        {
            throw new InvalidOperationException($"BusinessSettings:TimeZoneId '{timeZoneId}' is invalid.", ex);
        }
    }
}
