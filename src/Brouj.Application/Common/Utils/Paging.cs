using Brouj.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
namespace Brouj.Application.Common.Utils;

public static class Paging
{
    public static async Task<PaginationResult<T>> ToPageAsync<T>(IQueryable<T> query, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var count = await query.CountAsync(cancellationToken);
        var offset = ((long)pageNumber - 1) * pageSize;

        // An oversized page is empty; never overflow Skip into a negative database offset.
        var items = offset > int.MaxValue ? [] : await query.Skip((int)offset).Take(pageSize).ToArrayAsync(cancellationToken);

        return new PaginationResult<T>(items, pageNumber, pageSize, count, 100);
    }
}
