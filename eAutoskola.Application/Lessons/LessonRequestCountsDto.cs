namespace eAutoskola.Application.Lessons;

public class LessonRequestCountsDto
{
    public int All { get; set; }
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public int Cancelled { get; set; }
}
