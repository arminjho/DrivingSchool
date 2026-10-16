namespace eAutoskola.Application.Lessons;

/// <summary>Slot comes from a prior GET /api/lessons/available-slots call.</summary>
public class LessonBookRequest
{
    public int InstructorId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public int DrivingZoneId { get; set; }
    public string? Note { get; set; }
}
