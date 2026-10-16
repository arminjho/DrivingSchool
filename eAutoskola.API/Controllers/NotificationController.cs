using eAutoskola.Application.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[Route("api/notifications")]
public class NotificationController : BaseController<NotificationDto, NotificationSearchObject>
{
    private readonly NotificationService _notificationService;

    public NotificationController(NotificationService notificationService) : base(notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken)
        => Ok(await _notificationService.GetUnreadCountAsync(cancellationToken));

    [HttpPost("{id:int}/mark-read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        await _notificationService.MarkReadAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllReadAsync(cancellationToken);
        return NoContent();
    }
}
