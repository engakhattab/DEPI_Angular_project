namespace HR.Models;

public class Trip
{
    public Guid Id { get; set; }

    public string ReferenceName { get; set; } = string.Empty;

    public string Project { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public string TripType { get; set; } = string.Empty;

    public DateOnly TripDate { get; set; }

    public string TripCode { get; set; } = string.Empty;

    public string RequestCode { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
