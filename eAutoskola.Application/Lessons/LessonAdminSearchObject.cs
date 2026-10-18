using eAutoskola.Application.Common;

namespace eAutoskola.Application.Lessons;

/// <summary>Admin "all lessons" overview filters (docs: desktop admin lesson overview). Status
/// matches the LessonStatus enum names case-insensitively; omitted/unrecognised means all.</summary>
public class LessonAdminSearchObject : BaseSearchObject
{
    public string? Status { get; set; }
    public int? InstructorId { get; set; }
    public int? StudentId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "StartsAtUtc" };

    public LessonAdminSearchObject()
    {
        SortBy = "StartsAtUtc";
        SortDirection = "desc";
    }
}
