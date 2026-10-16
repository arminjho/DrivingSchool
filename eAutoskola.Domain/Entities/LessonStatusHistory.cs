using eAutoskola.Domain.Enums;

namespace eAutoskola.Domain.Entities;

/// <summary>Audit trail: who changed a lesson's status, when, and why (upute.md §7).</summary>
public class LessonStatusHistory
{
    public int Id { get; set; }

    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    public LessonStatus FromStatus { get; set; }
    public LessonStatus ToStatus { get; set; }

    public int ChangedByUserId { get; set; }
    public User? ChangedByUser { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}
