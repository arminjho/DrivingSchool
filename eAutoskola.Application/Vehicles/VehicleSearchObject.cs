using eAutoskola.Application.Common;

namespace eAutoskola.Application.Vehicles;

public class VehicleSearchObject : BaseSearchObject
{
    public string? RegistrationNumber { get; set; }
    public int? VehicleCategoryId { get; set; }
    public string? Status { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "RegistrationNumber", "Make", "Model", "ManufactureYear" };
}
