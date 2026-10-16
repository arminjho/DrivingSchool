namespace eAutoskola.Application.StudyLessons;

public class StudyLessonRequest
{
    public required string Title { get; set; }
    public int QuestionAreaId { get; set; }
    public required string Tags { get; set; }
    public int ValidForYear { get; set; }
    public int ReadingMinutes { get; set; }
    public required string ChapterLabel { get; set; }
}
