namespace ZeroX2C.Blog.API.Modules.Users.Admin.Contracts;

public sealed record AdminUserResponse(
    Guid Id,
    string Username,
    string Email,
    bool IsBlocked,
    DateTime CreatedAt,
    UserRole Role
);
