namespace ZeroX2C.Blog.API.Utility.Configuration;

public static class ConfigurationExtensions
{
    public static string GetRequiredConnectionString(
        this IConfiguration configuration,
        string name
    ) =>
        configuration.GetConnectionString(name)
        ?? throw new MissingRequiredConnectionStringException(name);
}

public class MissingRequiredConnectionStringException(string name)
    : Exception($"Missing required connection string '{name}'");
