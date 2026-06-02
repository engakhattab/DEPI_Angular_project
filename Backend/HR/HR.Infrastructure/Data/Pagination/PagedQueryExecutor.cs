using System.Linq.Expressions;
using HR.Shared.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HR.Infrastructure.Data.Pagination;

public static class PagedQueryExecutor
{
    public static async Task<PagedList<T>> ExecuteAsync<T>(
        IQueryable<T> source,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var normalized = PagedList<T>.Normalize(page, pageSize);
        var totalCount = await source.CountAsync(ct);
        var items = await source
            .Skip((normalized.Page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToListAsync(ct);

        return new PagedList<T>(items, totalCount, normalized.Page, normalized.PageSize);
    }

    public static async Task<PagedList<T>> ExecuteDescendingAsync<T, TKey>(
        IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        DatabaseFacade database,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        if (!string.Equals(database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            return await ExecuteAsync(source.OrderByDescending(keySelector), page, pageSize, ct);
        }

        var normalized = PagedList<T>.Normalize(page, pageSize);
        var items = await source.ToListAsync(ct);
        var ordered = items
            .OrderByDescending(keySelector.Compile())
            .Skip((normalized.Page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToList();

        return new PagedList<T>(ordered, items.Count, normalized.Page, normalized.PageSize);
    }
}
