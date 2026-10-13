namespace eAutoskola.Infrastructure.Configuration;

public class RabbitMqOptions : EnvOptionsBase
{
    public string Host { get; }
    public int Port { get; }
    public string User { get; }
    public string Password { get; }

    public RabbitMqOptions()
    {
        Host = Require("RABBITMQ_HOST");
        Port = GetOrDefault("RABBITMQ_PORT", 5672);
        User = Require("RABBITMQ_USER");
        Password = Require("RABBITMQ_PASSWORD");
    }
}
