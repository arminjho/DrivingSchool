namespace eAutoskola.Contracts.Messaging;

public class LessonCancelledMessage
{
    public int LessonId { get; set; }
    public required string ToEmail { get; set; }
    public required string ToName { get; set; }
    public required string Reason { get; set; }
}
