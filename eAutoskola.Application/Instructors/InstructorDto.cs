namespace eAutoskola.Application.Instructors;

public class InstructorDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfileImagePath { get; set; }
    public int VehicleCategoryId { get; set; }
    public string VehicleCategoryName { get; set; } = string.Empty;
}
