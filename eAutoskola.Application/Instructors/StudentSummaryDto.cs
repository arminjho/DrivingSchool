namespace eAutoskola.Application.Instructors;

/// <summary>"Moji učenici" row - a student who has at least one lesson with this instructor.</summary>
public class StudentSummaryDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public string? ProfileImagePath { get; set; }
    public int VehicleCategoryId { get; set; }
    public string VehicleCategoryName { get; set; } = string.Empty;
    public int TotalLessons { get; set; }
}
