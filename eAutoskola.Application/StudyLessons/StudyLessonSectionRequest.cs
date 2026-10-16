namespace eAutoskola.Application.StudyLessons;

public class StudyLessonSectionRequest
{
    public int StudyLessonId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public int SortOrder { get; set; }
}
