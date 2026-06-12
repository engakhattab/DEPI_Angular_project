using HR.Domain.Enums;

namespace HR.Application.DTOs.Audit;

public class AuditLogEntryResponse
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public AuditActionType ActionType { get; set; }
    public Guid? ActorEmployeeId { get; set; }
    public string? ActorMarker { get; set; }
    public DateTimeOffset PerformedAt { get; set; }
    public IReadOnlyList<string> ChangedFields { get; set; } = [];
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
    public string? SensitiveSummary { get; set; }
}
