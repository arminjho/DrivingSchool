using eAutoskola.Application.Common;

namespace eAutoskola.Application.ReferenceData.Countries;

public class CountrySearchObject : BaseSearchObject
{
    public string? Name { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Code", "Name" };
}
