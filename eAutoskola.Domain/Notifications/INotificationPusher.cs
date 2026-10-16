namespace eAutoskola.Domain.Notifications;

/// <summary>
/// Real-time push for the in-app Notification feed (clients also poll as fallback - upute.md
/// §7.2). Implemented in the API project via SignalR's IHubContext, since that requires the
/// Hub type which only makes sense in the web layer - Application (which creates
/// notifications) depends on this Domain-level interface instead of referencing API directly.
/// </summary>
public interface INotificationPusher
{
    Task PushAsync(int userId, object notification, CancellationToken cancellationToken);
}
