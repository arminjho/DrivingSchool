using eAutoskola.Application.Common;

namespace eAutoskola.Application.Lessons;

/// <summary>"all" | "pending" | "confirmed" | "cancelled" (docs/prijava.md §4.3.2).</summary>
public class LessonRequestSearchObject : BaseSearchObject
{
    public string Status { get; set; } = "all";

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "StartsAtUtc" };

    public LessonRequestSearchObject()
    {
        SortBy = "StartsAtUtc";
        SortDirection = "asc";
    }
}
