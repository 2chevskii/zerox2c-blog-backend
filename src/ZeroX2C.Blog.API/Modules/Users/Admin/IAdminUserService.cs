using ZeroX2C.Blog.API.Modules.Users.Contracts.Admin;

namespace ZeroX2C.Blog.API.Modules.Users.Admin;

public interface IAdminUserService
{
    Task<IReadOnlyCollection<AdminUserResponse>> GetUsersAsync(
        CancellationToken cancellationToken
    );

    Task<AdminUserOperationResult> UpdateRoleAsync(
        Guid userId,
        UserRole role,
        CancellationToken cancellationToken
    );

    Task<AdminUserOperationResult> BlockUserAsync(
        Guid userId,
        string? reason,
        CancellationToken cancellationToken
    );

    Task<AdminUserOperationResult> UnblockUserAsync(
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<AdminUserOperationResult> UpdatePasswordAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken
    );
}
