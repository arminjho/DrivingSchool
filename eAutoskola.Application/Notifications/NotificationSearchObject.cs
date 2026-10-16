using eAutoskola.Application.Common;

namespace eAutoskola.Application.Notifications;

public class NotificationSearchObject : BaseSearchObject
{
    public bool? IsRead { get; set; }

    protected override ISet<string> SortableColumns { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CreatedAt" };

    public NotificationSearchObject()
    {
        SortBy = "CreatedAt";
        SortDirection = "desc";
    }
}
