namespace eAutoskola.Application.VehicleMaintenance;

public class VehicleServiceInsertRequest
{
    public int VehicleId { get; set; }
    public DateTime ServiceDate { get; set; }
    public int VehicleServiceTypeId { get; set; }
    public required string Description { get; set; }
    public decimal Cost { get; set; }
}
