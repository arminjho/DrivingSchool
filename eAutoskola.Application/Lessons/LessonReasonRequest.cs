namespace eAutoskola.Application.Lessons;

/// <summary>Used for reject (Pending -> Cancelled by Instructor) and cancel (Confirmed ->
/// Cancelled) - Reason is required in both cases.</summary>
public class LessonReasonRequest
{
    public required string Reason { get; set; }
}
