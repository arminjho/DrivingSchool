namespace eAutoskola.Contracts.Messaging;

/// <summary>
/// Published by the API to the "mail_sending" RabbitMQ queue; consumed and actually sent by
/// eAutoskola.Worker. Shared assembly so both ends deserialize the same shape.
/// </summary>
public class EmailMessage
{
    public required string ToEmail { get; set; }
    public required string ToName { get; set; }
    public required string Subject { get; set; }
    public required string BodyHtml { get; set; }
}
