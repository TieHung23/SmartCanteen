namespace SC.Contract.Shared;

public class PaginationParams
{
    private int _pageNumber = 1;
    private int _pageSize = 10;
    private const int MaxPageSize = 100;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 10 : value > MaxPageSize ? MaxPageSize : value;
    }

    protected PaginationParams()
    {
    }

    protected PaginationParams(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>
    /// Calculates the number of items to skip for pagination
    /// </summary>
    public int GetSkipCount() => (PageNumber - 1) * PageSize;
}

