using HR.Domain.Enums;

namespace HR.Infrastructure.Audit;

public interface IAuditWriter
{
    Task WriteAsync(
        string entityType,
        Guid entityId,
        AuditActionType actionType,
        Guid? actorEmployeeId,
        string? actorMarker,
        IReadOnlyList<string> changedFields,
        object? oldValues,
        object? newValues,
        string? sensitiveSummary,
        CancellationToken ct);
}
