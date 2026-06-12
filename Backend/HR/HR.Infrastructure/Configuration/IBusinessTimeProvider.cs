namespace HR.Infrastructure.Configuration;

public interface IBusinessTimeProvider
{
    DateTimeOffset GetUtcNow();
    DateOnly GetBusinessDate(DateTimeOffset utcTimestamp);
}
