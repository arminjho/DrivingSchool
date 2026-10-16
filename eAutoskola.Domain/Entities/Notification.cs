using eAutoskola.Domain.Enums;

namespace eAutoskola.Domain.Entities;

public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public required string Title { get; set; }
    public required string Message { get; set; }
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
