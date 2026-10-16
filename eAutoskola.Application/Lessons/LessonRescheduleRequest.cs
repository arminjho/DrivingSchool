namespace eAutoskola.Application.Lessons;

public class LessonRescheduleRequest
{
    public DateTime StartsAtUtc { get; set; }
    public int DrivingZoneId { get; set; }
}
