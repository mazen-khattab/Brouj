namespace Brouj.Application.Common.Models;

public sealed class PaginationResult<T>
{
    public PaginationResult(
        IReadOnlyCollection<T> items,
        int pageNumber,
        int pageSize,
        int totalCount,
        int maximumPageSize)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(totalCount, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumPageSize, 1);

        if (pageSize > maximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                pageSize,
                "Page size cannot exceed the configured maximum page size.");
        }

        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    public IReadOnlyCollection<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
}
