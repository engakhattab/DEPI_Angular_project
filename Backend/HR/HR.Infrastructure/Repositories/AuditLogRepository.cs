using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Data;
using HR.Infrastructure.Data.Pagination;
using HR.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Repositories;

public class AuditLogRepository(ApplicationDbContext context) : IAuditLogRepository
{
    private readonly ApplicationDbContext _context = context;

    public Task AddAsync(AuditLogEntry entry, CancellationToken ct)
    {
        return _context.AuditLogEntries.AddAsync(entry, ct).AsTask();
    }

    public Task<PagedList<AuditLogEntry>> SearchAsync(
        string? entityType,
        Guid? entityId,
        Guid? actorEmployeeId,
        AuditActionType? action,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = _context.AuditLogEntries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(e => e.EntityType == entityType);
        }

        if (entityId.HasValue)
        {
            query = query.Where(e => e.EntityId == entityId.Value);
        }

        if (actorEmployeeId.HasValue)
        {
            query = query.Where(e => e.ActorEmployeeId == actorEmployeeId.Value);
        }

        if (action.HasValue)
        {
            query = query.Where(e => e.ActionType == action.Value);
        }

        if (string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            return SearchSqliteAsync(query, from, to, page, pageSize, ct);
        }

        if (from.HasValue)
        {
            query = query.Where(e => e.PerformedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.PerformedAt <= to.Value);
        }

        return PagedQueryExecutor.ExecuteDescendingAsync(query, e => e.PerformedAt, _context.Database, page, pageSize, ct);
    }

    private static async Task<PagedList<AuditLogEntry>> SearchSqliteAsync(
        IQueryable<AuditLogEntry> query,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var entries = await query.ToListAsync(ct);

        if (from.HasValue)
        {
            entries = entries.Where(e => e.PerformedAt >= from.Value).ToList();
        }

        if (to.HasValue)
        {
            entries = entries.Where(e => e.PerformedAt <= to.Value).ToList();
        }

        var normalized = PagedList<AuditLogEntry>.Normalize(page, pageSize);
        var items = entries
            .OrderByDescending(e => e.PerformedAt)
            .Skip((normalized.Page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToList();

        return new PagedList<AuditLogEntry>(items, entries.Count, normalized.Page, normalized.PageSize);
    }
}
