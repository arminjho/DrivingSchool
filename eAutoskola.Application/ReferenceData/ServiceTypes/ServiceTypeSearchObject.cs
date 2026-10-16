using eAutoskola.Application.Common;

namespace eAutoskola.Application.ReferenceData.ServiceTypes;

public class ServiceTypeSearchObject : BaseSearchObject
{
    public string? Name { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name", "Price" };
}
