using HR.Domain.Entities;
using HR.Infrastructure.Data;
using HR.Infrastructure.Data.Pagination;
using HR.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Repositories;

public class EmployeeDocumentRepository(ApplicationDbContext context) : IEmployeeDocumentRepository
{
    private readonly ApplicationDbContext _context = context;

    public Task<EmployeeDocument?> GetCurrentByIdAsync(Guid employeeId, Guid documentId, CancellationToken ct)
    {
        return _context.EmployeeDocuments.FirstOrDefaultAsync(
            d => d.Id == documentId && d.EmployeeId == employeeId && d.RemovedAt == null,
            ct);
    }

    public Task<PagedList<EmployeeDocument>> GetCurrentPageAsync(Guid employeeId, int page, int pageSize, CancellationToken ct)
    {
        var query = _context.EmployeeDocuments
            .AsNoTracking()
            .Where(d => d.EmployeeId == employeeId && d.RemovedAt == null);

        if (string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            return GetCurrentPageSqliteAsync(query, page, pageSize, ct);
        }

        return PagedQueryExecutor.ExecuteAsync(query.OrderByDescending(d => d.UploadedAt), page, pageSize, ct);
    }

    public Task AddAsync(EmployeeDocument document, CancellationToken ct)
    {
        return _context.EmployeeDocuments.AddAsync(document, ct).AsTask();
    }

    private static async Task<PagedList<EmployeeDocument>> GetCurrentPageSqliteAsync(
        IQueryable<EmployeeDocument> query,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var normalized = PagedList<EmployeeDocument>.Normalize(page, pageSize);
        var documents = await query.ToListAsync(ct);
        var items = documents
            .OrderByDescending(d => d.UploadedAt)
            .Skip((normalized.Page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToList();

        return new PagedList<EmployeeDocument>(items, documents.Count, normalized.Page, normalized.PageSize);
    }
}
