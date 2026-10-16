namespace eAutoskola.Application.Common;

public class PagedResult<T>
{
    public List<T> ResultList { get; set; } = [];
    public int Count { get; set; }
}
