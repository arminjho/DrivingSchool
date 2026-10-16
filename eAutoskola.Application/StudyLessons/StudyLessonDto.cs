namespace eAutoskola.Application.StudyLessons;

public class StudyLessonDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public int QuestionAreaId { get; set; }
    public string QuestionAreaName { get; set; } = string.Empty;
    public required string Tags { get; set; }
    public int ValidForYear { get; set; }
    public int ReadingMinutes { get; set; }
    public required string ChapterLabel { get; set; }
    public int SectionCount { get; set; }
}
