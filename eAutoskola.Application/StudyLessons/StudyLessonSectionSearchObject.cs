using eAutoskola.Application.Common;

namespace eAutoskola.Application.StudyLessons;

public class StudyLessonSectionSearchObject : BaseSearchObject
{
    public int? StudyLessonId { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SortOrder", "Title" };

    public StudyLessonSectionSearchObject()
    {
        SortBy = "SortOrder";
        SortDirection = "asc";
    }
}
