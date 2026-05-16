using ZeroX2C.Blog.API.Modules.Users.Contracts.Admin;

namespace ZeroX2C.Blog.API.Modules.Users.Admin;

public sealed record AdminUserOperationResult(
    AdminUserOperationStatus Status,
    AdminUserResponse? User = null
)
{
    public static AdminUserOperationResult Success(User user) =>
        new(AdminUserOperationStatus.Success, ToResponse(user));

    public static AdminUserOperationResult Failure(AdminUserOperationStatus status) => new(status);

    public static AdminUserResponse ToResponse(User user) =>
        new(
            user.Id,
            user.Username,
            user.Email,
            user.IsBlocked,
            user.CreatedAt,
            user.Role,
            KnownUsers.IsKnownUserId(user.Id),
            KnownUsers.CanPasswordBeChanged(user.Id)
        );
}
