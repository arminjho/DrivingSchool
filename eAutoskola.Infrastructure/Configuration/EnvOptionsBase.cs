namespace eAutoskola.Infrastructure.Configuration;

/// <summary>
/// Base for every strongly-typed configuration group. Each concrete *Options class reads its
/// own environment variables exactly once, in its constructor, and is registered as a DI
/// singleton. This is the ONLY place <see cref="Environment.GetEnvironmentVariable"/> is
/// called from - never read env vars anywhere else, and never put real values in
/// appsettings.json (upute.md §3.3).
/// </summary>
public abstract class EnvOptionsBase
{
    protected static string Require(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required environment variable '{key}' is missing. Check your .env file against .env.example.");
        }

        return value;
    }

    protected static string GetOrDefault(string key, string defaultValue)
        => Environment.GetEnvironmentVariable(key) is { Length: > 0 } value ? value : defaultValue;

    protected static int GetOrDefault(string key, int defaultValue)
        => int.TryParse(Environment.GetEnvironmentVariable(key), out var value) ? value : defaultValue;

    protected static bool GetOrDefault(string key, bool defaultValue)
        => bool.TryParse(Environment.GetEnvironmentVariable(key), out var value) ? value : defaultValue;

    protected static string[] GetArrayOrDefault(string key, string[] defaultValue)
        => Environment.GetEnvironmentVariable(key) is { Length: > 0 } value
            ? value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : defaultValue;
}
