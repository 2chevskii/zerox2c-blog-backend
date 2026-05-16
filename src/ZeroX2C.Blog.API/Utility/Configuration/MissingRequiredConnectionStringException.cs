namespace ZeroX2C.Blog.API.Utility.Configuration;

public class MissingRequiredConnectionStringException(string name)
    : Exception($"Missing required connection string '{name}'");
