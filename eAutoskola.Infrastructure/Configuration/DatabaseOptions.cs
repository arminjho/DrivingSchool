namespace eAutoskola.Infrastructure.Configuration;

public class DatabaseOptions : EnvOptionsBase
{
    public string ConnectionString { get; }

    public DatabaseOptions()
    {
        ConnectionString = Require("DB_CONNECTION_STRING");
    }
}
