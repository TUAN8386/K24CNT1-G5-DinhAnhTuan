using Microsoft.EntityFrameworkCore;

namespace QLDienThoai.Models;

public interface IPagedResult
{
    int PageIndex { get; }
    int TotalPages { get; }
    int TotalCount { get; }
}

public class PagedResult<T> : IPagedResult
{
    public List<T> Items { get; set; } = new();
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public static class PagingExtensions
{
    public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> query, int page, int pageSize)
    {
        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<T> { Items = items, PageIndex = page, PageSize = pageSize, TotalCount = total };
    }
}
