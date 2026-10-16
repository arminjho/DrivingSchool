namespace eAutoskola.Contracts.Messaging;

public class LessonConfirmedMessage
{
    public int LessonId { get; set; }
    public required string ToEmail { get; set; }
    public required string ToName { get; set; }
    public DateTime StartsAtUtc { get; set; }
}
