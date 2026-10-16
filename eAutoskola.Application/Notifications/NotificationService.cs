using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Notifications;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Notifications;

public class NotificationService : BaseQueryService<NotificationDto, NotificationSearchObject, Notification>
{
    private readonly EAutoskolaDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationPusher _notificationPusher;

    public NotificationService(EAutoskolaDbContext context, ICurrentUserService currentUser, INotificationPusher notificationPusher)
        : base(context)
    {
        _context = context;
        _currentUser = currentUser;
        _notificationPusher = notificationPusher;
    }

    protected override IQueryable<Notification> ApplyFilter(IQueryable<Notification> query, NotificationSearchObject search)
    {
        query = query.Where(n => n.UserId == _currentUser.UserId);
        if (search.IsRead.HasValue)
        {
            query = query.Where(n => n.IsRead == search.IsRead.Value);
        }
        return query;
    }

    public Task<int> GetUnreadCountAsync(CancellationToken cancellationToken)
        => _context.Notifications.CountAsync(n => n.UserId == _currentUser.UserId && !n.IsRead, cancellationToken);

    public async Task MarkReadAsync(int id, CancellationToken cancellationToken)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), id);

        if (notification.UserId != _currentUser.UserId)
        {
            throw new ForbiddenException(eAutoskola.Domain.Resources.Messages.NotOwner);
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(CancellationToken cancellationToken)
    {
        await _context.Notifications
            .Where(n => n.UserId == _currentUser.UserId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true), cancellationToken);
    }

    /// <summary>
    /// Best-effort side effect, deliberately NOT part of the caller's larger business
    /// transaction (same treatment as RabbitMQ publishing) - creates its own row, saves
    /// immediately, then pushes over SignalR. Callers (e.g. LessonCommandService) invoke this
    /// after their own atomic Lesson+LessonStatusHistory save has already succeeded.
    /// </summary>
    public async Task NotifyAsync(int userId, string title, string message, NotificationType type, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        await _notificationPusher.PushAsync(userId, notification.Adapt<NotificationDto>(), cancellationToken);
    }
}
