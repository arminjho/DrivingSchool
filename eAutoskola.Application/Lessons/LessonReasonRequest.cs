namespace eAutoskola.Application.Lessons;

/// <summary>Used for reject (Pending -> Cancelled by the instructor) and cancel. The reason is
/// mandatory for a rejection and for cancelling a Confirmed lesson - the service enforces that -
/// but a student may cancel their own still-pending request without one.</summary>
public class LessonReasonRequest
{
    public string? Reason { get; set; }
}
