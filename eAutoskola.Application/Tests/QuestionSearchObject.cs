using eAutoskola.Application.Common;

namespace eAutoskola.Application.Tests;

public class QuestionSearchObject : BaseSearchObject
{
    public int? TestId { get; set; }
    public int? QuestionAreaId { get; set; }

    /// <summary>Text typed into the search box of the admin "Pitanja" table.</summary>
    public string? Text { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Text" };
}
