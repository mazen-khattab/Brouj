using Brouj.Application.Common.Models;

namespace Brouj.Application.UnitTests.Common;

public sealed class PaginationResultTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(95, 20, 5)]
    public void Constructor_CalculatesTotalPages(
        int totalCount,
        int pageSize,
        int expectedTotalPages)
    {
        var result = new PaginationResult<int>(
            [],
            pageNumber: 1,
            pageSize,
            totalCount,
            maximumPageSize: 100);

        Assert.Equal(expectedTotalPages, result.TotalPages);
    }

    [Fact]
    public void Constructor_PreservesItemsAndPaginationMetadata()
    {
        int[] items = [3, 4];

        var result = new PaginationResult<int>(
            items,
            pageNumber: 2,
            pageSize: 2,
            totalCount: 5,
            maximumPageSize: 50);

        Assert.Same(items, result.Items);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void Constructor_WhenPageSizeExceedsPolicyMaximum_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PaginationResult<int>(
                [],
                pageNumber: 1,
                pageSize: 101,
                totalCount: 0,
                maximumPageSize: 100));
    }
}
