namespace ZeroX2C.Blog.API.Modules.Users.Contracts.Auth;

public sealed record AuthResponse(
    Guid UserId,
    string Username,
    string Email,
    string AccessToken,
    DateTimeOffset ExpiresAt,
    UserRole Role
);
