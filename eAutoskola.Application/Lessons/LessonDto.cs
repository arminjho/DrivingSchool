namespace eAutoskola.Application.Lessons;

public class LessonDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public int InstructorId { get; set; }
    public string InstructorFullName { get; set; } = string.Empty;
    public int? VehicleId { get; set; }
    public string? VehicleRegistrationNumber { get; set; }
    public int DrivingZoneId { get; set; }
    public string DrivingZoneName { get; set; } = string.Empty;
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public int LessonNumber { get; set; }
    public string? Note { get; set; }
    public required string Status { get; set; }
    public string? RejectionReason { get; set; }
    public int? Rating { get; set; }
    public string? InstructorNotes { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>True for a lesson beyond the package quota that carries its own charge.</summary>
    public bool RequiresPayment { get; set; }
    public bool IsPaid { get; set; }
}
