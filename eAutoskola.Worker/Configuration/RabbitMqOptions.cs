namespace eAutoskola.Worker.Configuration;

/// <summary>
/// Worker has no project reference to eAutoskola.Infrastructure (it must stay a fully
/// independent deployable, per upute.md §3.2), so it reads its own copy of the RabbitMQ/SMTP
/// config rather than sharing Infrastructure's Options classes.
/// </summary>
public class RabbitMqOptions
{
    public string Host { get; }
    public int Port { get; }
    public string User { get; }
    public string Password { get; }

    public RabbitMqOptions()
    {
        Host = Require("RABBITMQ_HOST");
        Port = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var p) ? p : 5672;
        User = Require("RABBITMQ_USER");
        Password = Require("RABBITMQ_PASSWORD");
    }

    private static string Require(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required environment variable '{key}' is missing.");
        }

        return value;
    }
}
