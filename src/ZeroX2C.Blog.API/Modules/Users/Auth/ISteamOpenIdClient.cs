namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public interface ISteamOpenIdClient
{
    string CreateAuthenticationUrl(string returnTo, string realm);

    Task<SteamOpenIdValidationResult> ValidateCallbackAsync(
        SteamOpenIdCallback callback,
        string expectedReturnTo,
        CancellationToken cancellationToken
    );
}
