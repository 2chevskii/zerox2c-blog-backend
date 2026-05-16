namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public sealed record SteamOpenIdCallback(IReadOnlyDictionary<string, string> Parameters);
