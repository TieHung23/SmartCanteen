namespace SC.Contract.Shared;

public class PaginatedList<T>(List<T> items, int pageNumber, int pageSize, int totalCount)
    where T : notnull
{
    public List<T> Items { get; set; } = items ?? [];
    public int PageNumber { get; set; } = pageNumber;
    public int PageSize { get; set; } = pageSize;
    public int TotalCount { get; set; } = totalCount;
    public int TotalPages { get; set; } = (int)Math.Ceiling(totalCount / (decimal)pageSize);

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

