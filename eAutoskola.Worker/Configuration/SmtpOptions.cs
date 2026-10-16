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
        // TODO(config): for local dev, SMTP_HOST/PORT point at the mailhog container (set by
        // docker-compose.yml) and SMTP_USERNAME/PASSWORD can stay as the .env.example
        // placeholders since mailhog doesn't require auth. For a real send (outside Docker, or
        // for the submission build), set a Mailtrap sandbox inbox (mailtrap.io) or a Gmail "app
        // password" (myaccount.google.com/apppasswords - needs 2FA enabled) here instead.
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
