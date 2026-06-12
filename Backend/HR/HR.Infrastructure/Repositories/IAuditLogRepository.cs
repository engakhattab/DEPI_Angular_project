using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Shared.Pagination;

namespace HR.Infrastructure.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken ct);
    Task<PagedList<AuditLogEntry>> SearchAsync(string? entityType, Guid? entityId, Guid? actorEmployeeId, AuditActionType? action, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct);
}
