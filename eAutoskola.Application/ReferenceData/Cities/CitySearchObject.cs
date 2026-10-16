using eAutoskola.Application.Common;

namespace eAutoskola.Application.ReferenceData.Cities;

public class CitySearchObject : BaseSearchObject
{
    public string? Name { get; set; }
    public int? CountryId { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name", "PostalCode" };
}
