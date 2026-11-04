namespace Lending.Application.Common;

/// <summary>
/// Normalized pagination parameters. Page numbers are 1-based and page sizes are
/// clamped so a client cannot request an unbounded result set.
/// </summary>
public readonly record struct PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public PageRequest(
        int? pageNumber,
        int? pageSize,
        int defaultPageSize = DefaultPageSize,
        int maxPageSize = MaxPageSize)
    {
        PageNumber = Math.Max(pageNumber ?? 1, 1);
        PageSize = Math.Clamp(pageSize ?? defaultPageSize, 1, maxPageSize);
    }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int Skip => (PageNumber - 1) * PageSize;
}

/// <summary>
/// Envelope returned by every paginated collection endpoint.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}
