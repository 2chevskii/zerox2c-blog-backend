using ZeroX2C.Blog.API.Modules.Users.Contracts.Auth;

namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public interface IAuthService
{
    string CreateSteamAuthenticationUrl(string returnTo, string realm);

    Task<AuthOperationResult<AuthResponse>> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken
    );

    Task<AuthOperationResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken
    );

    Task<AuthOperationResult<AuthResponse>> LoginWithSteamAsync(
        SteamOpenIdCallback callback,
        string expectedReturnTo,
        CancellationToken cancellationToken
    );

    Task<AuthOperationResult<CurrentUserResponse>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken
    );
}
