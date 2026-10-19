using eAutoskola.Application.Common;

namespace eAutoskola.Application.Lessons;

/// <summary>"active" = Pending + Confirmed, per docs/prijava.md §4.2.3. Newest request first by
/// default, so a just-booked lesson is the first card of the list it redirects to (upute.md §6);
/// the student home asks for StartsAtUtc ascending to find the next lesson.</summary>
public class MyLessonsSearchObject : BaseSearchObject
{
    public string Tab { get; set; } = "active";

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "StartsAtUtc", "CreatedAt" };

    public MyLessonsSearchObject()
    {
        SortBy = "CreatedAt";
        SortDirection = "desc";
    }
}
