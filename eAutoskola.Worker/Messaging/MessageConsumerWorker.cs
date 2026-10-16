using System.Text;
using System.Text.Json;
using eAutoskola.Contracts.Messaging;
using eAutoskola.Worker.Handlers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace eAutoskola.Worker.Messaging;

/// <summary>
/// Generic RabbitMQ consumer: one BackgroundService instance per queue (matching the
/// reference project's "one consumer per queue" pattern - REFERENCE_ANALYSIS.md §2.7). All the
/// connect/declare/consume/retry/ack/dead-letter plumbing lives here ONCE; the actual work for
/// each message type is in its own IMessageHandler&lt;TMessage&gt; (e.g. LessonConfirmedHandler),
/// resolved from a fresh DI scope per message. AsyncEventingBasicConsumer, manual ack,
/// retry-with-backoff (1s -&gt; 2s -&gt; 4s -&gt; 8s), then dead-letter - never an empty catch
/// (upute.md Appendix A.1).
/// </summary>
public class MessageConsumerWorker<TMessage> : BackgroundService where TMessage : class
{
    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MessageConsumerWorker<TMessage>> _logger;
    private readonly string _queueName;
    private readonly string _deadLetterQueueName;

    public MessageConsumerWorker(
        RabbitMqConnectionProvider connectionProvider,
        IServiceScopeFactory scopeFactory,
        ILogger<MessageConsumerWorker<TMessage>> logger)
    {
        _connectionProvider = connectionProvider;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _queueName = MessageQueueNaming.GetQueueName<TMessage>();
        _deadLetterQueueName = $"{_queueName}.dlq";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await _connectionProvider.GetConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: _queueName, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(
            queue: _deadLetterQueueName, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            var raw = Encoding.UTF8.GetString(args.Body.ToArray());
            TMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<TMessage>(raw);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Could not deserialize message from {Queue}, routing straight to {DeadLetterQueue}. Raw body: {RawBody}", _queueName, _deadLetterQueueName, raw);
                await PublishToDeadLetterAsync(channel, raw, stoppingToken);
                await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
                return;
            }

            if (message is null)
            {
                await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TMessage>>();

            try
            {
                await RetryHelper.RunWithRetryAsync(
                    () => handler.HandleAsync(message, stoppingToken),
                    _logger,
                    cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Giving up processing message from {Queue} after all retries - moving to {DeadLetterQueue}. Raw body: {RawBody}",
                    _queueName, _deadLetterQueueName, raw);
                await PublishToDeadLetterAsync(channel, raw, stoppingToken);
            }

            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        };

        await channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task PublishToDeadLetterAsync(IChannel channel, string rawBody, CancellationToken cancellationToken)
    {
        try
        {
            var body = Encoding.UTF8.GetBytes(rawBody);
            var properties = new BasicProperties { Persistent = true };
            await channel.BasicPublishAsync(
                exchange: string.Empty, routingKey: _deadLetterQueueName, mandatory: false,
                basicProperties: properties, body: body, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // The message is still acked on the live queue either way (it's already been
            // retried to exhaustion) - this only logs that the DLQ copy itself failed to land,
            // so the failure is never silently lost from the logs even if RabbitMQ is
            // unreachable at this exact moment.
            _logger.LogError(ex, "Failed to publish to dead-letter queue {DeadLetterQueue}", _deadLetterQueueName);
        }
    }
}
