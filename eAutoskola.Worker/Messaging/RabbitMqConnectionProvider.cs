using eAutoskola.Worker.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace eAutoskola.Worker.Messaging;

/// <summary>
/// Consumer-side connection provider. Separate class from the API-side publisher's provider
/// because Worker has no project reference to Infrastructure (REFERENCE_ANALYSIS.md §2.7/§3).
/// </summary>
public class RabbitMqConnectionProvider : IAsyncDisposable
{
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqConnectionProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnectionProvider(RabbitMqOptions options, ILogger<RabbitMqConnectionProvider> logger)
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

            // RabbitMQ's healthcheck can report healthy before the AMQP listener accepts
            // connections, and a broker can take well over a minute to boot. The bounded
            // 1s->2s->4s->8s retry (upute.md Appendix A.1) is for processing a message; the
            // initial connect instead waits for the broker for as long as it takes, so a
            // BackgroundService never takes the whole host down just because the broker is late.
            _connection = await ConnectWithBackoffAsync(factory, cancellationToken);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IConnection> ConnectWithBackoffAsync(ConnectionFactory factory, CancellationToken cancellationToken)
    {
        var delay = InitialRetryDelay;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var connection = await factory.CreateConnectionAsync(cancellationToken);
                _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port} (attempt {Attempt})", _options.Host, _options.Port, attempt);
                return connection;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "RabbitMQ at {Host}:{Port} is not reachable yet (attempt {Attempt}): {Reason}. Retrying in {Delay}",
                    _options.Host, _options.Port, attempt, ex.Message, delay);

                // Throws OperationCanceledException when the host is stopping, which ends the loop.
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, MaxRetryDelay.TotalSeconds));
            }
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
