using eAutoskola.Application.Common;

namespace eAutoskola.Application.Lessons;

/// <summary>"active" = Pending + Confirmed, per docs/prijava.md §4.2.3. Newest request first by
/// default, so a just-booked lesson is the first card of the list it redirects to (upute.md §6).
/// "upcoming" is the same statuses but additionally requires a future start time - the student
/// home's "Sljedeći čas" card uses it (with StartsAtUtc ascending) so a Pending request the
/// instructor never acted on, or a Confirmed lesson whose time already passed, cannot be
/// mistaken for the next lesson.</summary>
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
