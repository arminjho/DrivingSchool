using eAutoskola.Application.Common;

namespace eAutoskola.Application.Users;

public class UserSearchObject : BaseSearchObject
{
    /// <summary>Matches against first name, last name, or email.</summary>
    public string? Search { get; set; }
    public string? Role { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "FirstName", "LastName", "Email" };
}
