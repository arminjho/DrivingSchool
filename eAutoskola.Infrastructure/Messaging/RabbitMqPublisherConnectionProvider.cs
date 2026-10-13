using eAutoskola.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace eAutoskola.Infrastructure.Messaging;

/// <summary>
/// Lazily-created singleton RabbitMQ connection for publishers, guarded by a semaphore
/// (double-checked locking) so concurrent requests don't open multiple connections. Mirrors
/// the reference project's provider pattern (REFERENCE_ANALYSIS.md §2.7).
/// </summary>
public class RabbitMqPublisherConnectionProvider : IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisherConnectionProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqPublisherConnectionProvider(RabbitMqOptions options, ILogger<RabbitMqPublisherConnectionProvider> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.User,
                Password = _options.Password,
            };

            // RabbitMQ's healthcheck can report healthy slightly before the AMQP listener is
            // actually accepting connections - retry the initial connect with backoff
            // (1s -> 2s -> 4s -> 8s) rather than fail the first request after startup
            // (upute.md Appendix A.1).
            var delay = TimeSpan.FromSeconds(1);
            const int maxAttempts = 4;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _connection = await factory.CreateConnectionAsync(cancellationToken);
                    break;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    _logger.LogWarning(ex, "RabbitMQ connection attempt {Attempt}/{MaxAttempts} failed, retrying in {Delay}", attempt, maxAttempts, delay);
                    await Task.Delay(delay, cancellationToken);
                    delay *= 2;
                }
            }

            return _connection!;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
