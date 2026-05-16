namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public interface IAuthenticationContext
{
    AuthenticationData Data { get; }

    bool IsAuthenticated { get; }

    Guid UserId { get; }

    Guid? MaybeUserId { get; }

    string Username { get; }

    string? MaybeUsername { get; }

    string? Email { get; }

    string? MaybeEmail { get; }

    UserRole Role { get; }

    UserRole? MaybeRole { get; }

    bool IsBlocked { get; }

    bool? MaybeIsBlocked { get; }
}
