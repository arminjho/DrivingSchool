namespace eAutoskola.Domain.Entities;

public class StudentProfile
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int VehicleCategoryId { get; set; }
    public VehicleCategory? VehicleCategory { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
