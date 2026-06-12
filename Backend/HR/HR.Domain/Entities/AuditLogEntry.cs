using HR.Domain.Enums;

namespace HR.Domain.Entities;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public AuditActionType ActionType { get; set; }
    public Guid? ActorEmployeeId { get; set; }
    public Employee? Actor { get; set; }
    public string? ActorMarker { get; set; }
    public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;
    public string ChangedFields { get; set; } = "[]";
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? SensitiveSummary { get; set; }
}
