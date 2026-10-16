namespace eAutoskola.Application.VehicleMaintenance;

public class VehicleServiceDto
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string VehicleRegistrationNumber { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public int VehicleServiceTypeId { get; set; }
    public string VehicleServiceTypeName { get; set; } = string.Empty;
    public required string Description { get; set; }
    public decimal Cost { get; set; }
    public required string Status { get; set; }
}
