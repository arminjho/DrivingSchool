using System.Text;
using System.Text.Json;
using eAutoskola.Contracts.Messaging;
using eAutoskola.Domain.Messaging;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace eAutoskola.Infrastructure.Messaging;

/// <summary>
/// One RabbitMQ publisher for every message contract type (LessonConfirmedMessage,
/// PasswordResetRequestedMessage, ...) - queue name comes from the shared
/// MessageQueueNaming helper in Contracts, so the Worker's consumers can never drift from the
/// name this publisher actually uses.
/// </summary>
public class RabbitMqMessagePublisher : IMessagePublisher
{
    private readonly RabbitMqPublisherConnectionProvider _connectionProvider;
    private readonly ILogger<RabbitMqMessagePublisher> _logger;

    public RabbitMqMessagePublisher(
        RabbitMqPublisherConnectionProvider connectionProvider,
        ILogger<RabbitMqMessagePublisher> logger)
    {
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    public async Task<bool> PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : class
    {
        var queueName = MessageQueueNaming.GetQueueName<TMessage>();

        try
        {
            var connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(
                new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
                cancellationToken);

            await channel.QueueDeclareAsync(
                queue: queueName, durable: true, exclusive: false, autoDelete: false,
                cancellationToken: cancellationToken);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var properties = new BasicProperties { Persistent = true };

            await channel.BasicPublishAsync(
                exchange: string.Empty, routingKey: queueName, mandatory: false,
                basicProperties: properties, body: body, cancellationToken: cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {MessageType} to RabbitMQ queue {Queue}", typeof(TMessage).Name, queueName);
            return false;
        }
    }
}
