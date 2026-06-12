namespace HR.Application.DTOs.Audit;

public class AuditLogQueryRequest
{
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? ActorEmployeeId { get; set; }
    public string? Action { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
