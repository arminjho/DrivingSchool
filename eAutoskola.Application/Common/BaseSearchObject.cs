namespace eAutoskola.Application.Common;

/// <summary>
/// Every list endpoint's query parameters extend this. PageSize is clamped to
/// [1, MaxPageSize] in the setter itself - no derived search object can accidentally create an
/// unbounded "get all" endpoint (upute.md §8.2: "PageSize mora imati definisan maksimalni
/// limit... Endpointi tipa RetrieveAll bez limita smatraju se greškom").
/// </summary>
public abstract class BaseSearchObject
{
    public const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = 10;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 1,
            > MaxPageSize => MaxPageSize,
            _ => value,
        };
    }

    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "asc";

    /// <summary>Allowlist of columns this search object permits sorting by. A SortBy value not
    /// in this set is ignored (never throws - just falls back to the default order).</summary>
    protected virtual ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool IsSortable(string? column) => column is not null && SortableColumns.Contains(column);
}
