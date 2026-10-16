using eAutoskola.Domain.Enums;

namespace eAutoskola.Domain.Entities;

public class Vehicle : ISoftDelete
{
    public int Id { get; set; }
    public required string RegistrationNumber { get; set; }
    public required string Make { get; set; }
    public required string Model { get; set; }

    public int VehicleCategoryId { get; set; }
    public VehicleCategory? VehicleCategory { get; set; }

    public int ManufactureYear { get; set; }
    public VehicleStatus Status { get; set; } = VehicleStatus.Active;
    public string? ImagePath { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<VehicleService> Services { get; set; } = [];
}
