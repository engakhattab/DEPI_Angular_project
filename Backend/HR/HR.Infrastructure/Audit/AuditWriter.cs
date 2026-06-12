using System.Text.Json;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Repositories;

namespace HR.Infrastructure.Audit;

public class AuditWriter(IAuditLogRepository auditLogRepository, TimeProvider timeProvider) : IAuditWriter
{
    private readonly IAuditLogRepository _auditLogRepository = auditLogRepository;
    private readonly TimeProvider _timeProvider = timeProvider;

    public Task WriteAsync(
        string entityType,
        Guid entityId,
        AuditActionType actionType,
        Guid? actorEmployeeId,
        string? actorMarker,
        IReadOnlyList<string> changedFields,
        object? oldValues,
        object? newValues,
        string? sensitiveSummary,
        CancellationToken ct)
    {
        var entry = new AuditLogEntry
        {
            EntityType = entityType,
            EntityId = entityId,
            ActionType = actionType,
            ActorEmployeeId = actorEmployeeId,
            ActorMarker = actorMarker,
            PerformedAt = _timeProvider.GetUtcNow(),
            ChangedFields = JsonSerializer.Serialize(changedFields),
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
            SensitiveSummary = sensitiveSummary
        };

        return _auditLogRepository.AddAsync(entry, ct);
    }
}
