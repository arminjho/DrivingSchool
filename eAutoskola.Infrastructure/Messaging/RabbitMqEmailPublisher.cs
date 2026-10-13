using System.Text;
using System.Text.Json;
using eAutoskola.Contracts.Messaging;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace eAutoskola.Infrastructure.Messaging;

public class RabbitMqEmailPublisher : IEmailPublisher
{
    public const string QueueName = "mail_sending";

    private readonly RabbitMqPublisherConnectionProvider _connectionProvider;
    private readonly ILogger<RabbitMqEmailPublisher> _logger;

    public RabbitMqEmailPublisher(
        RabbitMqPublisherConnectionProvider connectionProvider,
        ILogger<RabbitMqEmailPublisher> logger)
    {
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    public async Task<bool> PublishAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(
                new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
                cancellationToken);

            await channel.QueueDeclareAsync(
                queue: QueueName, durable: true, exclusive: false, autoDelete: false,
                cancellationToken: cancellationToken);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var properties = new BasicProperties { Persistent = true };

            await channel.BasicPublishAsync(
                exchange: string.Empty, routingKey: QueueName, mandatory: false,
                basicProperties: properties, body: body, cancellationToken: cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish email message to RabbitMQ queue {Queue}", QueueName);
            return false;
        }
    }
}
