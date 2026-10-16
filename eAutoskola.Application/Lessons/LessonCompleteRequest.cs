namespace eAutoskola.Application.Lessons;

public class LessonCompleteRequest
{
    public int Rating { get; set; }
    public required string InstructorNotes { get; set; }
}
