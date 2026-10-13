namespace eAutoskola.Infrastructure.Configuration;

public class JwtOptions : EnvOptionsBase
{
    public string Key { get; }
    public string Issuer { get; }
    public string Audience { get; }
    public int ExpiryMinutes { get; }

    public JwtOptions()
    {
        Key = Require("JWT_KEY");
        Issuer = Require("JWT_ISSUER");
        Audience = Require("JWT_AUDIENCE");
        ExpiryMinutes = GetOrDefault("JWT_EXPIRY_MINUTES", 60);
    }
}
