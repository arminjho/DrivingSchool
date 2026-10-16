using eAutoskola.Application.Common;

namespace eAutoskola.Application.Instructors;

public class InstructorSearchObject : BaseSearchObject
{
    public int? VehicleCategoryId { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "FirstName", "LastName" };
}
