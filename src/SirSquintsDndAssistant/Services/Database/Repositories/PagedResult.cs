namespace SirSquintsDndAssistant.Services.Database.Repositories;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public static PagedResult<T> Empty(int page, int pageSize) => new()
    {
        Items = new List<T>(),
        TotalCount = 0,
        Page = page,
        PageSize = pageSize
    };
}
