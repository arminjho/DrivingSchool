namespace eAutoskola.Domain.Entities;

public class StudyLessonSection
{
    public int Id { get; set; }

    public int StudyLessonId { get; set; }
    public StudyLesson? StudyLesson { get; set; }

    public required string Title { get; set; }
    public required string Content { get; set; }
    public int SortOrder { get; set; }
}
