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

            // RabbitMQ's healthcheck can report healthy slightly before the AMQP listener is
            // actually accepting connections - retry the initial connect with backoff rather
            // than crash the host on the first attempt (upute.md Appendix A.1).
            await RetryHelper.RunWithRetryAsync(
                async () => _connection = await factory.CreateConnectionAsync(cancellationToken),
                _logger,
                cancellationToken: cancellationToken);

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
