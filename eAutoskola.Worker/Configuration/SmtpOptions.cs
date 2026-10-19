namespace eAutoskola.Worker.Configuration;

public class SmtpOptions
{
    public string Host { get; }
    public int Port { get; }
    public string Username { get; }
    public string Password { get; }
    public bool UseSsl { get; }
    public string FromAddress { get; }

    public SmtpOptions()
    {
        Host = Require("SMTP_HOST");
        Port = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var p) ? p : 587;
        // In Docker, docker-compose.yml points SMTP_HOST/SMTP_PORT at the MailHog container,
        // which accepts mail without authentication; SMTP_USERNAME/SMTP_PASSWORD are only used
        // when a real provider is configured in .env.
        Username = Require("SMTP_USERNAME");
        Password = Require("SMTP_PASSWORD");
        UseSsl = bool.TryParse(Environment.GetEnvironmentVariable("SMTP_USE_SSL"), out var s) && s;
        FromAddress = Require("SMTP_FROM");
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
