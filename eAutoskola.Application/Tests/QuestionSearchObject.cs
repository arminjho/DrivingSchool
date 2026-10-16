using eAutoskola.Application.Common;

namespace eAutoskola.Application.Tests;

public class QuestionSearchObject : BaseSearchObject
{
    public int? TestId { get; set; }
    public int? QuestionAreaId { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Text" };
}
