namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public sealed record SteamOpenIdValidationResult(bool IsValid, string? SteamId = null)
{
    public static SteamOpenIdValidationResult Valid(string steamId) => new(true, steamId);
    public static SteamOpenIdValidationResult Invalid() => new(false);
}
