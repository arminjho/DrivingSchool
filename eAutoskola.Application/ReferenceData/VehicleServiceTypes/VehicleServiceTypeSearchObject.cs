using eAutoskola.Application.Common;

namespace eAutoskola.Application.ReferenceData.VehicleServiceTypes;

public class VehicleServiceTypeSearchObject : BaseSearchObject
{
    public string? Name { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name" };
}
