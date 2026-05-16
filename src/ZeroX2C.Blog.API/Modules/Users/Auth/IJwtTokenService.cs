namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public interface IJwtTokenService
{
    Task<GeneratedToken> CreateAccessTokenAsync(User user, CancellationToken cancellationToken);
}
