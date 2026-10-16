namespace eAutoskola.Application.StudyLessons;

public class StudyLessonSectionDto
{
    public int Id { get; set; }
    public int StudyLessonId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public int SortOrder { get; set; }
}

public class StudyLessonDetailDto : StudyLessonDto
{
    public List<StudyLessonSectionDto> Sections { get; set; } = [];
}
