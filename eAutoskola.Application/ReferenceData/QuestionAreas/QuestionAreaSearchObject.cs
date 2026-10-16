using eAutoskola.Application.Common;

namespace eAutoskola.Application.ReferenceData.QuestionAreas;

public class QuestionAreaSearchObject : BaseSearchObject
{
    public string? Name { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name" };
}
