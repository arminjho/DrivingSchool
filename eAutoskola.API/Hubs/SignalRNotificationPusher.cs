using eAutoskola.Domain.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace eAutoskola.API.Hubs;

public class SignalRNotificationPusher : INotificationPusher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationPusher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PushAsync(int userId, object notification, CancellationToken cancellationToken)
        => _hubContext.Clients.Group(NotificationHub.GroupName(userId.ToString()))
            .SendAsync("ReceiveNotification", notification, cancellationToken);
}
