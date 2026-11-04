using Lending.Application.Common;

namespace Lending.UnitTests.Common;

public sealed class PaginationTests
{
    [Fact]
    public void PageRequest_uses_defaults_when_values_are_missing()
    {
        var page = new PageRequest(null, null);

        Assert.Equal(1, page.PageNumber);
        Assert.Equal(PageRequest.DefaultPageSize, page.PageSize);
        Assert.Equal(0, page.Skip);
    }

    [Fact]
    public void PageRequest_clamps_invalid_values()
    {
        var page = new PageRequest(-5, 0);

        Assert.Equal(1, page.PageNumber);
        Assert.Equal(1, page.PageSize);
        Assert.Equal(0, page.Skip);
    }

    [Fact]
    public void PageRequest_clamps_page_size_to_maximum()
    {
        var page = new PageRequest(3, 10_000);

        Assert.Equal(3, page.PageNumber);
        Assert.Equal(PageRequest.MaxPageSize, page.PageSize);
        Assert.Equal(2 * PageRequest.MaxPageSize, page.Skip);
    }

    [Fact]
    public void PagedResult_computes_total_pages_and_navigation_flags()
    {
        var result = new PagedResult<int>(Array.Empty<int>(), 2, 10, 23);

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void PagedResult_has_no_next_page_on_last_page()
    {
        var result = new PagedResult<int>(Array.Empty<int>(), 3, 10, 23);

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }
}
