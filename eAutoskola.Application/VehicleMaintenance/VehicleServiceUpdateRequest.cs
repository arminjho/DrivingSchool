using eAutoskola.Domain.Enums;

namespace eAutoskola.Application.VehicleMaintenance;

public class VehicleServiceUpdateRequest
{
    public DateTime ServiceDate { get; set; }
    public int VehicleServiceTypeId { get; set; }
    public required string Description { get; set; }
    public decimal Cost { get; set; }
    public VehicleServiceStatus Status { get; set; }
}
