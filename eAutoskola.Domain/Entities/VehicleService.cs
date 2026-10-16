using eAutoskola.Domain.Enums;

namespace eAutoskola.Domain.Entities;

public class VehicleService
{
    public int Id { get; set; }

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public DateTime ServiceDate { get; set; }

    public int VehicleServiceTypeId { get; set; }
    public VehicleServiceType? VehicleServiceType { get; set; }

    public required string Description { get; set; }
    public decimal Cost { get; set; }
    public VehicleServiceStatus Status { get; set; } = VehicleServiceStatus.Scheduled;

    /// <summary>When the record was entered (ServiceDate is when the service happens, possibly in the future).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
