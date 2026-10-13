using System.Text;
using System.Text.Json;
using eAutoskola.Contracts.Messaging;
using eAutoskola.Worker.Mail;
using eAutoskola.Worker.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace eAutoskola.Worker;

/// <summary>
/// Consumes the "mail_sending" queue and actually sends each email via SMTP (MailKit). Uses
/// AsyncEventingBasicConsumer (never the deprecated sync consumer), manual ack, and
/// retry-with-backoff per message - upute.md Appendix A.1.
/// </summary>
public class MailQueueConsumerWorker : BackgroundService
{
    private const string QueueName = "mail_sending";

    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MailQueueConsumerWorker> _logger;

    public MailQueueConsumerWorker(
        RabbitMqConnectionProvider connectionProvider,
        IServiceScopeFactory scopeFactory,
        ILogger<MailQueueConsumerWorker> logger)
    {
        _connectionProvider = connectionProvider;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await _connectionProvider.GetConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: QueueName, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            var raw = Encoding.UTF8.GetString(args.Body.ToArray());
            EmailMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<EmailMessage>(raw);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Could not deserialize message from {Queue}, discarding poison message", QueueName);
                await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
                return;
            }

            if (message is null)
            {
                await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var mailSender = scope.ServiceProvider.GetRequiredService<IMailSender>();

            try
            {
                await RetryHelper.RunWithRetryAsync(
                    () => mailSender.SendAsync(message, stoppingToken),
                    _logger,
                    cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Giving up sending email to {ToEmail} after all retries", message.ToEmail);
            }

            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        };

        await channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
