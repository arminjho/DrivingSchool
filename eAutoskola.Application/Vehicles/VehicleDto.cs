namespace eAutoskola.Application.Vehicles;

public class VehicleDto
{
    public int Id { get; set; }
    public required string RegistrationNumber { get; set; }
    public required string Make { get; set; }
    public required string Model { get; set; }
    public int VehicleCategoryId { get; set; }
    public string VehicleCategoryName { get; set; } = string.Empty;
    public int ManufactureYear { get; set; }
    public required string Status { get; set; }
    public string? ImagePath { get; set; }
}
