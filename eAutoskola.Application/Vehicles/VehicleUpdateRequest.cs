namespace eAutoskola.Application.Vehicles;

public class VehicleUpdateRequest
{
    public required string RegistrationNumber { get; set; }
    public required string Make { get; set; }
    public required string Model { get; set; }
    public int VehicleCategoryId { get; set; }
    public int ManufactureYear { get; set; }
}
