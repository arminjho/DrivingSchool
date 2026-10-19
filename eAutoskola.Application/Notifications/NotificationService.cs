using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Notifications;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eAutoskola.Application.Notifications;

public class NotificationService : BaseQueryService<NotificationDto, NotificationSearchObject, Notification>
{
    private readonly EAutoskolaDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationPusher _notificationPusher;
    private readonly ILogger<NotificationService> _logger;
    private readonly List<Notification> _staged = [];

    public NotificationService(
        EAutoskolaDbContext context,
        ICurrentUserService currentUser,
        INotificationPusher notificationPusher,
        ILogger<NotificationService> logger)
        : base(context)
    {
        _context = context;
        _currentUser = currentUser;
        _notificationPusher = notificationPusher;
        _logger = logger;
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
    /// Stages the notification row on the shared DbContext - it never calls SaveChanges itself.
    /// The calling service's single SaveChangesAsync stores it in the same transaction as the
    /// business change it reports (upute.md §3.4: rely on the shared DbContext instead of
    /// service-in-service SaveChanges calls). Call <see cref="PushStagedAsync"/> after that save.
    /// </summary>
    public void Stage(int userId, string title, string message, NotificationType type)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
        };

        _context.Notifications.Add(notification);
        _staged.Add(notification);
    }

    /// <summary>Stages the same notification for every other active administrator - the desktop
    /// app's bell reports what happens in the school (new requests, cancellations, payments,
    /// refunds). Like <see cref="Stage"/>, it only stages: the caller's save stores them.</summary>
    public async Task StageForAdminsAsync(string title, string message, NotificationType type, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUser.UserId;
        var adminIds = await _context.Users.AsNoTracking()
            .Where(u => u.Role == Role.Admin && u.IsActive && u.Id != currentUserId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var adminId in adminIds)
        {
            Stage(adminId, title, message, type);
        }
    }

    /// <summary>Pushes the notifications staged in this request over SignalR, once the caller's
    /// SaveChangesAsync has committed them. A failed push is only logged: the row is stored and
    /// the clients also poll, so the business operation must not fail because of it.</summary>
    public async Task PushStagedAsync(CancellationToken cancellationToken)
    {
        var pending = _staged.ToArray();
        _staged.Clear();

        foreach (var notification in pending)
        {
            try
            {
                await _notificationPusher.PushAsync(notification.UserId, notification.Adapt<NotificationDto>(), cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Real-time push of notification {NotificationId} to user {UserId} failed", notification.Id, notification.UserId);
            }
        }
    }

    /// <summary>Detaches staged notifications that will never be saved (the caller's save was
    /// abandoned), so a later SaveChanges on the same context cannot store them by accident.</summary>
    public void DiscardStaged()
    {
        foreach (var notification in _staged)
        {
            _context.Entry(notification).State = EntityState.Detached;
        }

        _staged.Clear();
    }
}
