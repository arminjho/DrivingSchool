namespace eAutoskola.Domain.Entities;

public class InstructorProfile
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Category this instructor is qualified to teach. One category, not M2M - see
    /// plan's "Design decisions" §5.</summary>
    public int VehicleCategoryId { get; set; }
    public VehicleCategory? VehicleCategory { get; set; }
}
