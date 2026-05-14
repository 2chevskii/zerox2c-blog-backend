namespace ZeroX2C.Blog.API.Modules.Users.Auth.Contracts;

public sealed record CurrentUserResponse(
    Guid UserId,
    string Username,
    string Email,
    bool IsBlocked,
    UserRole Role
);
