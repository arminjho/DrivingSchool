using eAutoskola.Application.Common;

namespace eAutoskola.Application.Tests;

public class TestSearchObject : BaseSearchObject
{
    public string? Title { get; set; }
    public int? QuestionAreaId { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Title" };
}
