using eAutoskola.Domain.Enums;

namespace eAutoskola.Domain.Entities;

/// <summary>
/// Driving lesson (čas vožnje). Status transitions are decided exclusively by
/// eAutoskola.Application/Lessons/LessonStateMachine - never hard-deleted (upute.md §7).
/// </summary>
public class Lesson
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public User? Student { get; set; }

    public int InstructorId { get; set; }
    public User? Instructor { get; set; }

    /// <summary>Auto-assigned at booking; must be non-null before the state machine allows
    /// Pending -> Confirmed.</summary>
    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public int DrivingZoneId { get; set; }
    public DrivingZone? DrivingZone { get; set; }

    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }

    /// <summary>Sequential count of this student's lessons (e.g. "18. čas").</summary>
    public int LessonNumber { get; set; }

    public string? Note { get; set; }

    public LessonStatus Status { get; set; } = LessonStatus.Pending;

    /// <summary>Required when an Instructor rejects a Pending lesson or anyone cancels a
    /// Confirmed lesson.</summary>
    public string? RejectionReason { get; set; }

    public int? Rating { get; set; }
    public string? InstructorNotes { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LessonStatusHistory> StatusHistory { get; set; } = [];

    /// <summary>Only populated for lessons beyond the package quota.</summary>
    public ICollection<Charge> Charges { get; set; } = [];
}
