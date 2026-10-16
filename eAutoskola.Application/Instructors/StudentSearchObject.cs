using eAutoskola.Application.Common;

namespace eAutoskola.Application.Instructors;

public class StudentSearchObject : BaseSearchObject
{
    public string? Search { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "FirstName", "LastName" };
}
