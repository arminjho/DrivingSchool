namespace eAutoskola.Infrastructure.Configuration;

public class SmtpOptions : EnvOptionsBase
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
        Port = GetOrDefault("SMTP_PORT", 587);
        Username = Require("SMTP_USERNAME");
        Password = Require("SMTP_PASSWORD");
        UseSsl = GetOrDefault("SMTP_USE_SSL", true);
        FromAddress = Require("SMTP_FROM");
    }
}
