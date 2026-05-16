using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Modules.Users.Auth;
using ZeroX2C.Blog.API.Modules.Users.Contracts.Admin;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Users.Admin;

public sealed class AdminUserService(BlogDbContext dbContext, IPasswordHasher passwordHasher)
    : IAdminUserService
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

        if (KnownUsers.IsKnownUserId(user.Id))
        {
            return AdminUserOperationResult.Failure(
                AdminUserOperationStatus.KnownUserCannotBeModified
            );
        }

        if (role == UserRole.SuperAdmin)
        {
            return AdminUserOperationResult.Failure(
                AdminUserOperationStatus.SuperAdminAlreadyExists
            );
        }

        user.Role = role;
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

        if (KnownUsers.IsKnownUserId(user.Id))
        {
            return AdminUserOperationResult.Failure(
                AdminUserOperationStatus.KnownUserCannotBeModified
            );
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

        if (KnownUsers.IsKnownUserId(user.Id))
        {
            return AdminUserOperationResult.Failure(
                AdminUserOperationStatus.KnownUserCannotBeModified
            );
        }

        user.IsBlocked = false;
        user.BlockedAt = null;
        user.BlockedReason = null;

        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminUserOperationResult.Success(user);
    }

    public async Task<AdminUserOperationResult> UpdatePasswordAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken
    )
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return AdminUserOperationResult.Failure(AdminUserOperationStatus.UserNotFound);
        }

        if (!KnownUsers.CanPasswordBeChanged(user.Id))
        {
            return AdminUserOperationResult.Failure(
                AdminUserOperationStatus.PasswordCannotBeChanged
            );
        }

        user.PasswordHash = passwordHasher.HashPassword(password);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminUserOperationResult.Success(user);
    }

    private Task<User?> FindUserAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.SingleOrDefaultAsync(
            existingUser => existingUser.Id == userId,
            cancellationToken
        );
}
