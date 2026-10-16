namespace eAutoskola.Application.Notifications;

public class NotificationDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required string Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
