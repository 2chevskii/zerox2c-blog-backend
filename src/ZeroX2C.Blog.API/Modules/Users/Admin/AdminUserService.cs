using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Modules.Users.Admin.Contracts;
using ZeroX2C.Blog.API.Persistence;

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
}

public enum AdminUserOperationStatus
{
    Success,
    UserNotFound,
    CannotRemoveOnlySuperAdmin,
    SuperAdminAlreadyExists,
    CannotBlockSuperAdmin,
}

public sealed record AdminUserOperationResult(
    AdminUserOperationStatus Status,
    AdminUserResponse? User = null
)
{
    public static AdminUserOperationResult Success(User user) =>
        new(AdminUserOperationStatus.Success, ToResponse(user));

    public static AdminUserOperationResult Failure(AdminUserOperationStatus status) =>
        new(status);

    public static AdminUserResponse ToResponse(User user) =>
        new(
            user.Id,
            user.Username,
            user.Email,
            user.IsBlocked,
            user.CreatedAt,
            user.Role
        );
}

public sealed class AdminUserService(BlogDbContext dbContext) : IAdminUserService
{
    public async Task<IReadOnlyCollection<AdminUserResponse>> GetUsersAsync(
        CancellationToken cancellationToken
    )
    {
        var users = await dbContext.Users
            .OrderByDescending(user => user.CreatedAt)
            .ToListAsync(cancellationToken);

        return users.Select(AdminUserOperationResult.ToResponse).ToArray();
    }

    public async Task<AdminUserOperationResult> UpdateRoleAsync(
        Guid userId,
        UserRole role,
        CancellationToken cancellationToken
    )
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return AdminUserOperationResult.Failure(AdminUserOperationStatus.UserNotFound);
        }

        if (user.Role == UserRole.SuperAdmin && role != UserRole.SuperAdmin)
        {
            return AdminUserOperationResult.Failure(
                AdminUserOperationStatus.CannotRemoveOnlySuperAdmin
            );
        }

        if (user.Role != UserRole.SuperAdmin && role == UserRole.SuperAdmin)
        {
            var superAdminExists = await dbContext.Users.AnyAsync(
                existingUser =>
                    existingUser.Id != user.Id && existingUser.Role == UserRole.SuperAdmin,
                cancellationToken
            );

            if (superAdminExists)
            {
                return AdminUserOperationResult.Failure(
                    AdminUserOperationStatus.SuperAdminAlreadyExists
                );
            }
        }

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminUserOperationResult.Success(user);
    }

    public async Task<AdminUserOperationResult> BlockUserAsync(
        Guid userId,
        string? reason,
        CancellationToken cancellationToken
    )
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return AdminUserOperationResult.Failure(AdminUserOperationStatus.UserNotFound);
        }

        if (user.Role == UserRole.SuperAdmin)
        {
            return AdminUserOperationResult.Failure(
                AdminUserOperationStatus.CannotBlockSuperAdmin
            );
        }

        user.IsBlocked = true;
        user.BlockedAt = DateTimeOffset.UtcNow;
        user.BlockedReason = reason;
        user.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminUserOperationResult.Success(user);
    }

    public async Task<AdminUserOperationResult> UnblockUserAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return AdminUserOperationResult.Failure(AdminUserOperationStatus.UserNotFound);
        }

        user.IsBlocked = false;
        user.BlockedAt = null;
        user.BlockedReason = null;
        user.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminUserOperationResult.Success(user);
    }

    private Task<User?> FindUserAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.SingleOrDefaultAsync(
            existingUser => existingUser.Id == userId,
            cancellationToken
        );
}
