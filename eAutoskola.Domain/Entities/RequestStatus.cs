namespace eAutoskola.Domain.Entities;

/// <summary>
/// The reference table of lesson statuses (docs/prijava.md §4.1.5). Every Lesson points at its
/// row through Lesson.RequestStatusId, so a lesson can only have a status that exists here; the
/// LessonStatus enum is what the state machine decides with. The four rows' Code values are the
/// enum names - the name and the description can be edited, the rows cannot be re-coded or
/// deleted (RequestStatusService).
/// </summary>
public class RequestStatus
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
