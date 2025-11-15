using HR.Models;

namespace HR.DTOs.Transportation;

public class TripResponse
{
    public Guid Id { get; set; }

    public string ReferenceName { get; set; } = string.Empty;

    public string Project { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public string TripType { get; set; } = string.Empty;

    public DateOnly TripDate { get; set; }

    public string TripCode { get; set; } = string.Empty;

    public string RequestCode { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public static TripResponse FromEntity(Trip trip)
    {
        return new TripResponse
        {
            Id = trip.Id,
            ReferenceName = trip.ReferenceName,
            Project = trip.Project,
            Route = trip.Route,
            TripType = trip.TripType,
            TripDate = trip.TripDate,
            TripCode = trip.TripCode,
            RequestCode = trip.RequestCode,
            CreatedAt = trip.CreatedAt
        };
    }
}
