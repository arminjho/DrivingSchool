namespace eAutoskola.Contracts.Messaging;

public class PasswordResetRequestedMessage
{
    public required string ToEmail { get; set; }
    public required string ToName { get; set; }
    public required string Code { get; set; }
}
