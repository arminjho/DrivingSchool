using eAutoskola.Application.Common;

namespace eAutoskola.Application.VehicleMaintenance;

public class VehicleServiceSearchObject : BaseSearchObject
{
    public int? VehicleId { get; set; }
    public int? VehicleCategoryId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ServiceDate", "Cost" };
}
