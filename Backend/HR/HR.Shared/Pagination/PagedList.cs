using Microsoft.EntityFrameworkCore;

namespace HR.Shared.Pagination;

public class PagedList<T>
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;

    public PagedList(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        var normalized = Normalize(page, pageSize);
        Items = items;
        TotalCount = totalCount;
        Page = normalized.Page;
        PageSize = normalized.PageSize;
    }

    public static async Task<PagedList<T>> CreateAsync(
        IQueryable<T> source, int page, int pageSize, CancellationToken ct = default)
    {
        var normalized = Normalize(page, pageSize);
        var count = await source.CountAsync(ct);
        var items = await source
            .Skip((normalized.Page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToListAsync(ct);
        return new PagedList<T>(items, count, normalized.Page, normalized.PageSize);
    }

    private static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var normalizedPage = page <= 0 ? DefaultPage : page;
        var normalizedPageSize = pageSize <= 0 ? DefaultPageSize : pageSize;

        if (normalizedPageSize > MaxPageSize)
        {
            normalizedPageSize = MaxPageSize;
        }

        return (normalizedPage, normalizedPageSize);
    }
}
