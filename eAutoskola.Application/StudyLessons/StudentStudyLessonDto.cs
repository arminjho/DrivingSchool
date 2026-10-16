namespace eAutoskola.Application.StudyLessons;

/// <summary>"Moje lekcije" row.</summary>
public class StudentStudyLessonDto
{
    public int StudyLessonId { get; set; }
    public required string Title { get; set; }
    public string QuestionAreaName { get; set; } = string.Empty;
    public required string ChapterLabel { get; set; }
    public int ReadingMinutes { get; set; }
    public int SectionCount { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime LastOpenedAt { get; set; }
    public int ProgressPercent { get; set; }
}
