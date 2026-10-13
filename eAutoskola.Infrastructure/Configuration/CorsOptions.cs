namespace eAutoskola.Infrastructure.Configuration;

public class CorsOptions : EnvOptionsBase
{
    public string[] AllowedOrigins { get; }

    public CorsOptions()
    {
        AllowedOrigins = GetArrayOrDefault("CORS_ALLOWED_ORIGINS", []);
    }
}
