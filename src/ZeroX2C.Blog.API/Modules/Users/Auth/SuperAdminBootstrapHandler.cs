using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZeroX2C.Blog.API.CrossCutting.Bootstrap;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public sealed class SuperAdminBootstrapHandler(
    BlogDbContext dbContext,
    IPasswordHasher passwordHasher,
    IOptions<SuperAdminOptions> superAdminOptions,
    IAuthenticationContextManager authenticationContextManager,
    ILogger<SuperAdminBootstrapHandler> logger
) : IBootstrapHandler
{
    public int Order => 0;

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        using var authenticationScope = authenticationContextManager.AsSystem();

        await EnsureSystemUserAsync(cancellationToken);
        await EnsureSuperAdminAsync(cancellationToken);
        await EnsureOnlyKnownSuperAdminsAsync(cancellationToken);
    }

    private async Task EnsureSystemUserAsync(CancellationToken cancellationToken)
    {
        await EnsureKnownUserIdentityIsAvailableAsync(KnownUsers.System, cancellationToken);

        var user = await dbContext
            .Users.Include(existingUser => existingUser.ExternalLogins)
            .SingleOrDefaultAsync(
                existingUser => existingUser.Id == KnownUsers.System.Id,
                cancellationToken
            );

        if (user is null)
        {
            user = new User
            {
                Id = KnownUsers.System.Id,
                Username = KnownUsers.System.Username,
                Email = KnownUsers.System.Email,
                EmailConfirmed = true,
                PasswordHash = [],
                Role = KnownUsers.System.Role,
            };

            dbContext.Users.Add(user);
        }
        else
        {
            user.Username = KnownUsers.System.Username;
            user.Email = KnownUsers.System.Email;
            user.EmailConfirmed = true;
            user.PasswordHash = [];
            user.Role = KnownUsers.System.Role;
            user.IsBlocked = false;
            user.BlockedAt = null;
            user.BlockedReason = null;
            user.ExternalLogins.Clear();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSuperAdminAsync(CancellationToken cancellationToken)
    {
        await EnsureKnownUserIdentityIsAvailableAsync(KnownUsers.SuperAdmin, cancellationToken);

        var user = await dbContext.Users.SingleOrDefaultAsync(
            existingUser => existingUser.Id == KnownUsers.SuperAdmin.Id,
            cancellationToken
        );

        if (user is null)
        {
            user = new User
            {
                Id = KnownUsers.SuperAdmin.Id,
                Username = KnownUsers.SuperAdmin.Username,
                Email = KnownUsers.SuperAdmin.Email,
                EmailConfirmed = true,
                PasswordHash = passwordHasher.HashPassword(GetSuperAdminPassword()),
                Role = KnownUsers.SuperAdmin.Role,
            };

            dbContext.Users.Add(user);
        }
        else
        {
            user.Username = KnownUsers.SuperAdmin.Username;
            user.Email = KnownUsers.SuperAdmin.Email;
            user.EmailConfirmed = true;
            user.Role = KnownUsers.SuperAdmin.Role;
            user.IsBlocked = false;
            user.BlockedAt = null;
            user.BlockedReason = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureOnlyKnownSuperAdminsAsync(CancellationToken cancellationToken)
    {
        var invalidSuperAdminExists = await dbContext.Users.AnyAsync(
            user =>
                user.Role == UserRole.SuperAdmin
                && user.Id != KnownUsers.System.Id
                && user.Id != KnownUsers.SuperAdmin.Id,
            cancellationToken
        );

        if (invalidSuperAdminExists)
        {
            throw new InvalidOperationException(
                "Invalid auth state: only system and superadmin technical users may hold SuperAdmin role."
            );
        }
    }

    private async Task EnsureKnownUserIdentityIsAvailableAsync(
        KnownUsers.KnownUser knownUser,
        CancellationToken cancellationToken
    )
    {
        var usernameOwner = await dbContext.Users.SingleOrDefaultAsync(
            user => user.Username == knownUser.Username && user.Id != knownUser.Id,
            cancellationToken
        );
        if (usernameOwner is not null)
        {
            throw new InvalidOperationException(
                $"Invalid auth state: username '{knownUser.Username}' is used by a non-technical user."
            );
        }

        var emailOwner = await dbContext.Users.SingleOrDefaultAsync(
            user => user.Email == knownUser.Email && user.Id != knownUser.Id,
            cancellationToken
        );
        if (emailOwner is not null)
        {
            throw new InvalidOperationException(
                $"Invalid auth state: email '{knownUser.Email}' is used by a non-technical user."
            );
        }
    }

    private string GetSuperAdminPassword()
    {
        if (superAdminOptions.Value.UseDefaultPassword)
        {
            logger.LogWarning(
                "Using default SuperAdmin password for technical user '{Username}'.",
                KnownUsers.SuperAdmin.Username
            );

            return KnownUsers.SuperAdminDefaultPassword;
        }

        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        logger.LogWarning(
            "Generated SuperAdmin password for technical user '{Username}': {Password}",
            KnownUsers.SuperAdmin.Username,
            password
        );

        return password;
    }
}
