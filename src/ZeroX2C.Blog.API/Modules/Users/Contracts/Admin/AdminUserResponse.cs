namespace ZeroX2C.Blog.API.Modules.Users.Contracts.Admin;

public sealed record AdminUserResponse(
    Guid Id,
    string Username,
    string Email,
    bool IsBlocked,
    DateTime CreatedAt,
    UserRole Role,
    bool IsKnownUser,
    bool CanChangePassword
);
