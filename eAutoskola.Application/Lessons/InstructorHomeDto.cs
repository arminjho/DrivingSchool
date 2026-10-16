namespace eAutoskola.Application.Lessons;

public class InstructorHomeDto
{
    public int TodayLessonCount { get; set; }
    public int NewRequestCount { get; set; }
    public List<LessonDto> TodayLessons { get; set; } = [];
}
