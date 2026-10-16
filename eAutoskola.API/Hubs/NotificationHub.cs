using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace eAutoskola.API.Hubs;

/// <summary>JWT-authenticated; every connection is added to a per-user group so a push only
/// reaches that user's own connections.</summary>
[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));
        }

        await base.OnConnectedAsync();
    }

    public static string GroupName(string userId) => $"user:{userId}";
}
