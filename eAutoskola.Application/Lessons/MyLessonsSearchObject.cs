using eAutoskola.Application.Common;

namespace eAutoskola.Application.Lessons;

/// <summary>"active" = Pending + Confirmed, per docs/prijava.md §4.2.3.</summary>
public class MyLessonsSearchObject : BaseSearchObject
{
    public string Tab { get; set; } = "active";

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "StartsAtUtc" };

    public MyLessonsSearchObject()
    {
        SortBy = "StartsAtUtc";
        SortDirection = "desc";
    }
}
