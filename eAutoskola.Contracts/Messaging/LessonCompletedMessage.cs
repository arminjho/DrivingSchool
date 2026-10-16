namespace eAutoskola.Contracts.Messaging;

public class LessonCompletedMessage
{
    public int LessonId { get; set; }
    public required string ToEmail { get; set; }
    public required string ToName { get; set; }
    public int Rating { get; set; }
}
