namespace eAutoskola.Domain.Entities;

/// <summary>
/// Display/reference lookup table only - does NOT drive Lesson.Status (that's the LessonStatus
/// enum). Exists to satisfy upute.md §3.1's generic "statuses" reference-table CRUD
/// requirement and to back the reference-data screen (docs/prijava.md §4.1.5). Code values
/// match the LessonStatus enum names.
/// </summary>
public class RequestStatus
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
